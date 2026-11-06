"""Contrato HTTP v1 do serviço de ML (consumido pelo backend .NET). Mudança incompatível => /v2."""
from __future__ import annotations

from datetime import date, datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

LocationKind = Literal["pharmacy", "health_unit"]


class RecommendationRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    as_of: date | None = Field(None, description="Data de referência (padrão: dia seguinte à última movimentação).")
    lead_time_days: int | None = Field(None, ge=0, le=30)
    min_shelf_life_days: int | None = Field(None, ge=0, le=365)
    min_transfer_qty: int | None = Field(None, ge=1, le=100000)
    donor_demand_z: float | None = Field(None, ge=0, le=3, description=">0 deixa o doador mais conservador.")
    receiver_demand_z: float | None = Field(None, ge=0, le=3)
    max_distance_km: float | None = Field(None, gt=0, description="Só tem efeito quando há coordenadas.")


class RunInfo(BaseModel):
    model_version: str
    serving_tier: Literal["lgbm", "baseline"]
    trained_at: datetime
    data_fingerprint: str
    as_of: date
    horizon_weeks: int
    data_age_days: int
    status: Literal["ok", "degraded"]
    warnings: list[str]


class Summary(BaseModel):
    units_at_risk: float
    units_rescued: float
    rescue_rate: float | None
    n_transfers: int
    unmet_demand_before: float


class Suggestion(BaseModel):
    medicine_id: str
    batch_id: str
    batch_number: str
    from_location_id: str
    from_location_kind: LocationKind
    to_location_id: str
    to_location_kind: LocationKind
    quantity: int = Field(gt=0)
    expiration_date: datetime
    days_to_expiry: int
    shelf_life_at_arrival: int
    distance_km: float | None
    proximity_cost: float
    projected_waste_at_source: float


class ResidualWaste(BaseModel):
    location_id: str
    location_kind: LocationKind
    medicine_id: str
    batch_id: str
    batch_number: str
    expiration_date: datetime
    days_to_expiry: int
    unavoidable_waste: float


class StockItem(BaseModel):
    location_id: str
    medicine_id: str
    batch_id: str
    batch_number: str
    quantity: int
    expiration_date: datetime


class RecommendationResponse(BaseModel):
    run: RunInfo
    summary: Summary
    recommendations: list[Suggestion]
    residual_waste: list[ResidualWaste]
    already_expired: list[StockItem]
    no_forecast: list[StockItem]


class ModelInfo(BaseModel):
    model_version: str
    serving_tier: Literal["lgbm", "baseline"]
    trained_at: datetime
    last_train_week: date
    n_series: int
    n_weeks: int
    promotion: dict
    backtest_summary: dict | None
    coverage_q90: float | None
    available_versions: list[str]


class Health(BaseModel):
    status: Literal["ok"]
    model_loaded: bool
