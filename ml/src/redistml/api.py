"""Serviço HTTP de inferência (FastAPI). Treino NÃO é exposto por HTTP: roda como job agendado (`redistml train`)."""
from __future__ import annotations

import hmac
import logging
import os
import threading
import time
import uuid
from dataclasses import dataclass, replace
from typing import Callable

import pandas as pd
from fastapi import Depends, FastAPI, Header, HTTPException, Request

from .config import DataConfig, OptimizerConfig, ServingConfig
from .pipeline import ModelBundle, load_bundle, recommend
from .registry import ModelRegistry
from .schemas import (Health, ModelInfo, RecommendationRequest, RecommendationResponse)
from .sources import RawData, SqlSource

log = logging.getLogger("redistml.api")


@dataclass(frozen=True)
class Settings:
    models_dir: str = "artifacts/models"
    database_url: str | None = None
    api_key: str | None = None
    timezone: str = "America/Sao_Paulo"

    @classmethod
    def from_env(cls) -> "Settings":
        s = cls(models_dir=os.getenv("ML_MODELS_DIR", "artifacts/models"),
                database_url=os.getenv("ML_DATABASE_URL"), api_key=os.getenv("ML_API_KEY"),
                timezone=os.getenv("ML_TIMEZONE", "America/Sao_Paulo"))
        if not s.api_key or len(s.api_key) < 24:       # falha FECHADA: sem segredo forte o serviço não sobe
            raise RuntimeError("ML_API_KEY ausente ou curta (<24 chars). Gere com: openssl rand -hex 32")
        if not s.database_url:
            raise RuntimeError("ML_DATABASE_URL ausente (use um usuário PostgreSQL SOMENTE-LEITURA).")
        return s


class _BundleCache:
    """Recarrega o modelo quando o ponteiro `current` muda (promoção/rollback sem reiniciar o serviço)."""

    def __init__(self, models_dir: str):
        self.reg, self.models_dir, self._lock = ModelRegistry(models_dir), models_dir, threading.Lock()
        self._bundle: ModelBundle | None = None

    def get(self) -> ModelBundle:
        version = self.reg.current_version()
        if version is None:
            raise HTTPException(503, "Nenhum modelo registrado (rode o job de treino).")
        with self._lock:
            if self._bundle is None or self._bundle.version != version:
                self._bundle = load_bundle(self.models_dir, version)
                log.info("modelo carregado: %s (tier=%s)", version, self._bundle.tier)
            return self._bundle

    def loaded(self) -> bool:
        return self.reg.current_version() is not None


def _records(df: pd.DataFrame, cols: list[str]) -> list[dict]:
    if df is None or len(df) == 0:
        return []
    out = df[cols].astype(object)
    return out.where(out.notna(), None).to_dict("records")


def create_app(settings: Settings | None = None, raw_loader: Callable[[], RawData] | None = None) -> FastAPI:
    settings = settings or Settings.from_env()
    cache = _BundleCache(settings.models_dir)
    if raw_loader is None:
        source = SqlSource(settings.database_url, timezone=settings.timezone)
        raw_loader = source.load

    app = FastAPI(title="redistml", version="1.0.0",
                  description="Previsão de demanda e recomendação de remanejamento de medicamentos.")

    @app.middleware("http")
    async def request_log(request: Request, call_next):
        rid = request.headers.get("x-request-id") or uuid.uuid4().hex[:12]
        t0 = time.perf_counter()
        response = await call_next(request)
        response.headers["x-request-id"] = rid
        log.info("rid=%s %s %s -> %s in %.0fms", rid, request.method, request.url.path, response.status_code,
                 (time.perf_counter() - t0) * 1000)
        return response

    def auth(x_api_key: str | None = Header(default=None)) -> None:
        if not x_api_key or not hmac.compare_digest(x_api_key.encode(), settings.api_key.encode()):
            raise HTTPException(401, "API key inválida.")

    @app.get("/health", response_model=Health, tags=["ops"])
    def health():
        return Health(status="ok", model_loaded=cache.loaded())

    @app.get("/v1/model", response_model=ModelInfo, dependencies=[Depends(auth)], tags=["model"])
    def model_info():
        b = cache.get()
        m = b.meta
        return ModelInfo(model_version=b.version, serving_tier=b.tier, trained_at=m["trained_at"],
                         last_train_week=m["last_train_week"], n_series=m["n_series"], n_weeks=m["n_weeks"],
                         promotion=m["promotion"], backtest_summary=m.get("backtest_summary"),
                         coverage_q90=m.get("coverage_q90"), available_versions=cache.reg.list_versions())

    @app.post("/v1/recommendations", response_model=RecommendationResponse, dependencies=[Depends(auth)],
              tags=["recommendations"])
    def recommendations(req: RecommendationRequest):
        bundle = cache.get()
        overrides = {k: v for k, v in req.model_dump(exclude={"as_of"}).items() if v is not None}
        try:
            raw = raw_loader()
            out = recommend(raw, settings.models_dir, DataConfig(), replace(OptimizerConfig(), **overrides),
                            ServingConfig(), as_of=pd.Timestamp(req.as_of) if req.as_of else None, bundle=bundle)
        except ValueError as e:                                    # dados inconsistentes (validate_raw)
            raise HTTPException(422, f"Dados inválidos: {e}") from e
        except HTTPException:
            raise
        except Exception as e:                                     # banco fora do ar etc.
            log.exception("falha ao gerar recomendações")
            raise HTTPException(503, "Falha ao ler dados ou gerar recomendações.") from e

        r = out.result
        rec_cols = ["medicine_id", "batch_id", "batch_number", "from_location_id", "from_location_kind",
                    "to_location_id", "to_location_kind", "quantity", "expiration_date", "days_to_expiry",
                    "shelf_life_at_arrival", "distance_km", "proximity_cost", "projected_waste_at_source"]
        res_cols = ["location_id", "location_kind", "medicine_id", "batch_id", "batch_number", "expiration_date",
                    "days_to_expiry", "unavoidable_waste"]
        item_cols = ["location_id", "medicine_id", "batch_id", "batch_number", "quantity", "expiration_date"]
        return RecommendationResponse(
            run=out.run, summary={**{"unmet_demand_before": 0.0}, **r.summary},
            recommendations=_records(r.recommendations, rec_cols),
            residual_waste=_records(r.residual_waste, res_cols),
            already_expired=_records(r.already_expired, item_cols),
            no_forecast=_records(r.no_forecast, item_cols))

    return app


def app_factory() -> FastAPI:          # uvicorn --factory redistml.api:app_factory
    logging.basicConfig(level=os.getenv("ML_LOG_LEVEL", "INFO"),
                        format="%(asctime)s %(levelname)s %(name)s %(message)s")
    return create_app()
