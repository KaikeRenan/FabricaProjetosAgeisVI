"""Orquestração: treino (backtest + gate + registro) e inferência (previsão + otimização) com degradação graciosa.

Tiers de serviço
  - "lgbm"     : modelo global LightGBM, só quando há histórico suficiente E ele vence os baselines no backtest.
  - "baseline" : TSB + desvio das últimas semanas. É o tier de partida (poucos dados) e o fallback seguro.
O tier usado é SEMPRE devolvido ao chamador (rastreabilidade); nunca se serve um modelo não-promovido.
"""
from __future__ import annotations

import hashlib
import json
import platform
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

import joblib
import lightgbm
import pandas as pd

from .baselines import baseline_forecast_next
from .config import BacktestConfig, DataConfig, ModelConfig, OptimizerConfig, ServingConfig
from .evaluation import paired_bootstrap_gain, promotion_decision, rolling_origin_backtest
from .features import build_origin_features, stack_horizons
from .model import GlobalDemandForecaster
from .optimizer import OptimizationResult, optimize
from .panel import KEYS, add_location, build_demand_panel, default_as_of, location_kinds, validate_raw
from .registry import ModelRegistry
from .sources import RawData


def panel_fingerprint(panel: pd.DataFrame) -> str:
    h = pd.util.hash_pandas_object(panel, index=False).to_numpy().tobytes()
    return hashlib.sha256(h).hexdigest()[:16]


def required_weeks(mcfg: ModelConfig, bcfg: BacktestConfig) -> int:
    return mcfg.min_train_weeks + mcfg.horizon_weeks + (bcfg.n_splits - 1) * bcfg.step_weeks


# --------------------------------------------------------------------------------------------- treino
def train(raw: RawData, models_dir: str | Path, data_cfg=DataConfig(), mcfg=ModelConfig(),
          bcfg=BacktestConfig(), as_of=None, set_current: bool = True) -> dict:
    warnings = validate_raw(raw, data_cfg)
    panel = build_demand_panel(raw, data_cfg, as_of)
    if panel.empty:
        raise ValueError("Sem histórico de saídas (Exit) para treinar/servir; registre movimentações primeiro.")

    n_weeks = int(panel["week"].nunique())
    need = required_weeks(mcfg, bcfg)
    fp = panel_fingerprint(panel)
    trained_at = datetime.now(timezone.utc)
    version = f"{trained_at:%Y%m%dT%H%M%SZ}-{fp[:8]}"

    meta: dict = dict(
        model_version=version, trained_at=trained_at.isoformat(), last_train_week=str(panel["week"].max().date()),
        n_series=int(panel.groupby(KEYS).ngroups), n_weeks=n_weeks, n_rows=int(len(panel)),
        data_fingerprint=fp, data_warnings=warnings,
        configs=dict(data=asdict(data_cfg), model=asdict(mcfg), backtest=asdict(bcfg)),
        versions=dict(python=platform.python_version(), lightgbm=lightgbm.__version__, pandas=pd.__version__),
    )
    model = None
    if n_weeks < need:
        meta.update(serving_tier="baseline", backtest_summary=None, coverage_q90=None,
                    promotion=dict(promoted=False, reason=f"histórico insuficiente: {n_weeks} < {need} semanas"))
    else:
        bt = rolling_origin_backtest(panel, mcfg, bcfg)
        decision = promotion_decision(bt, bcfg)
        table = stack_horizons(build_origin_features(panel, mcfg), mcfg, with_target=True)
        model = GlobalDemandForecaster(mcfg).fit(table)
        meta.update(
            serving_tier="lgbm" if decision["promoted"] else "baseline",
            backtest_origins=[str(o.date()) for o in bt.origins],
            backtest_summary=bt.summary.round(4).to_dict("index"),
            backtest_by_horizon=bt.by_horizon.round(4).to_dict("index"),
            coverage_q90=round(bt.coverage_q90, 4), pinball_q90=round(bt.pinball_q90, 4),
            promotion=decision, feature_importance=model.feature_importance(),
            gain_vs_best_baseline=paired_bootstrap_gain(bt.predictions, "lgbm", decision["best_baseline"]))

    reg = ModelRegistry(models_dir)
    out = reg.new_version(version)
    if model is not None:
        joblib.dump(model, out / "model.joblib")
    (out / "metadata.json").write_text(json.dumps(meta, indent=2, default=str))
    if set_current:
        reg.set_current(version)
    return meta


# ------------------------------------------------------------------------------------------ inferência
@dataclass
class ModelBundle:
    version: str
    meta: dict
    model: GlobalDemandForecaster | None

    @property
    def tier(self) -> str:
        return "lgbm" if (self.model is not None and self.meta.get("serving_tier") == "lgbm") else "baseline"


def load_bundle(models_dir: str | Path, version: str | None = None) -> ModelBundle:
    reg = ModelRegistry(models_dir)
    version = version or reg.current_version()
    if version is None:
        raise FileNotFoundError("Nenhum modelo registrado. Rode `redistml train` primeiro.")
    d = reg.version_dir(version)
    meta = json.loads((d / "metadata.json").read_text())
    model = joblib.load(d / "model.joblib") if (d / "model.joblib").exists() else None
    return ModelBundle(version, meta, model)


def forecast_next(panel: pd.DataFrame, bundle: ModelBundle, horizon_weeks: int | None = None) -> pd.DataFrame:
    if bundle.tier == "lgbm":
        feats = build_origin_features(panel, bundle.model.cfg)
        feats = feats[feats["week"] == panel["week"].max()]
        X = stack_horizons(feats, bundle.model.cfg, with_target=False)
        return pd.concat([X[KEYS + ["h"]], bundle.model.predict(X)], axis=1)
    h = horizon_weeks or bundle.meta["configs"]["model"]["horizon_weeks"]
    return baseline_forecast_next(panel, h)


@dataclass
class RecommendationOutcome:
    result: OptimizationResult
    run: dict


def current_snapshot(raw: RawData) -> pd.DataFrame:
    stocks = add_location(raw.stocks)
    snap = stocks[stocks["removed_at"].isna() & (stocks["quantity"] > 0) & stocks["location_id"].notna()
                  & stocks["batch_id"].notna()]
    b = raw.batches
    if "removed_at" in b.columns:
        b = b[b["removed_at"].isna()]            # lote soft-deleted não entra em remanejamento
    snap = snap.merge(b[["batch_id", "medicine_id", "batch_number", "expiration_date"]], on="batch_id")
    return snap[["location_id", "medicine_id", "batch_id", "batch_number", "quantity", "expiration_date"]]


def recommend(raw: RawData, models_dir: str | Path, data_cfg=DataConfig(), ocfg=OptimizerConfig(),
              scfg=ServingConfig(), as_of=None, bundle: ModelBundle | None = None) -> RecommendationOutcome:
    bundle = bundle or load_bundle(models_dir)
    as_of = pd.Timestamp(as_of) if as_of is not None else default_as_of(raw)
    warnings: list[str] = []

    panel = build_demand_panel(raw, data_cfg, as_of)
    last_move = raw.movements["created_at"].max()
    age = int((as_of.normalize() - last_move.normalize()).days)
    if age > scfg.max_data_age_days:
        warnings.append(f"dados defasados: última movimentação há {age} dias (limite {scfg.max_data_age_days}).")
    if bundle.tier == "baseline":
        why = bundle.meta.get("promotion", {}).get("reason") or "modelo de ML não superou os baselines no backtest"
        warnings.append(f"tier 'baseline' em uso ({why}); sem sazonalidade/tendência.")

    snap = current_snapshot(raw)
    horizon = bundle.meta["configs"]["model"]["horizon_weeks"]
    fc = forecast_next(panel, bundle, horizon) if not panel.empty else pd.DataFrame(columns=KEYS + ["h", "mean", "q90"])
    res = optimize(fc, snap, as_of, ocfg, raw.locations)

    kinds = location_kinds(raw.stocks)
    if len(res.recommendations):
        r = res.recommendations
        r["from_location_kind"] = r["from_location_id"].map(kinds)
        r["to_location_kind"] = r["to_location_id"].map(kinds)
    if len(res.residual_waste):
        res.residual_waste["location_kind"] = res.residual_waste["location_id"].map(kinds)

    run = dict(model_version=bundle.version, serving_tier=bundle.tier, trained_at=bundle.meta["trained_at"],
               data_fingerprint=panel_fingerprint(panel) if not panel.empty else "empty",
               as_of=as_of.normalize(), horizon_weeks=horizon, data_age_days=age,
               status="degraded" if warnings else "ok", warnings=warnings)
    return RecommendationOutcome(res, run)
