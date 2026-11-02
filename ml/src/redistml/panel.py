"""RawData -> painel semanal de demanda (location x medicine x week), completo (zeros explícitos)."""
from __future__ import annotations

import numpy as np
import pandas as pd

from .config import DataConfig
from .sources import RawData

KEYS = ["location_id", "medicine_id"]


def monday(ts: pd.Series | pd.Timestamp):
    if isinstance(ts, pd.Series):
        return ts.dt.normalize() - pd.to_timedelta(ts.dt.weekday, unit="D")
    ts = pd.Timestamp(ts).normalize()
    return ts - pd.Timedelta(days=ts.weekday())


def default_as_of(raw: RawData) -> pd.Timestamp:
    """Data de referência quando o chamador não informa `as_of`.
    Semanas anteriores a ela são 'completas'. Se o último movimento caiu no sábado/domingo, a semana operacional já
    fechou e `as_of` é a segunda seguinte (sem isso a última semana completa seria descartada). Em produção prefira
    passar `as_of` explicitamente (a data de execução, idealmente uma segunda-feira)."""
    last = raw.movements["created_at"].max()
    if last.weekday() >= 5:
        return monday(last) + pd.Timedelta(weeks=1)
    return last.normalize() + pd.Timedelta(days=1)


def add_location(stocks: pd.DataFrame) -> pd.DataFrame:
    s = stocks.copy()
    s["location_id"] = s["pharmacy_id"].where(s["pharmacy_id"].notna(), s["health_unit_id"])
    s["location_kind"] = np.where(s["pharmacy_id"].notna(), "pharmacy", "health_unit")
    return s


def validate_raw(raw: RawData, cfg: DataConfig) -> list[str]:
    """Erros fatais levantam ValueError; retorna avisos (invariantes que o domínio hoje não garante)."""
    warnings: list[str] = []
    m, s = raw.movements, raw.stocks
    unknown = set(m["type"].unique()) - set(cfg.movement_type_map)
    if unknown:
        raise ValueError(f"Tipos de movimento sem mapeamento em DataConfig.movement_type_map: {unknown}")
    if (m["quantity"] < 0).any():
        raise ValueError("StockMovement.Quantity negativa: esperado módulo + tipo do movimento.")
    if m["movement_id"].duplicated().any():
        raise ValueError("movement_id duplicado.")
    both = s["pharmacy_id"].notna() & s["health_unit_id"].notna()
    none = s["pharmacy_id"].isna() & s["health_unit_id"].isna()
    if both.any():
        warnings.append(f"{int(both.sum())} stocks com PharmacyId E HealthUnitId (usando Pharmacy).")
    if none.any():
        warnings.append(f"{int(none.sum())} stocks sem local (ignorados).")
    missing = set(m["stock_id"]) - set(s["stock_id"])
    if missing:
        warnings.append(f"{len(missing)} stock_id de movimentos sem linha em Stocks (ignorados).")
    return warnings


def build_demand_panel(raw: RawData, cfg: DataConfig, as_of=None) -> pd.DataFrame:
    as_of = pd.Timestamp(as_of) if as_of is not None else default_as_of(raw)
    last_week = monday(as_of) - pd.Timedelta(weeks=1)

    stocks = add_location(raw.stocks)[["stock_id", "location_id"]].dropna()
    m = raw.movements.copy()
    m["kind"] = m["type"].map(cfg.movement_type_map)
    m = m[m["kind"].isin(cfg.demand_kinds)]          # só dispensação é demanda
    m = m.merge(stocks, on="stock_id", how="inner")
    m["week"] = monday(m["created_at"])
    m = m[m["week"] <= last_week]                     # nunca usa semana parcial

    if m.empty:
        return pd.DataFrame({"location_id": pd.Series(dtype="object"), "medicine_id": pd.Series(dtype="object"),
                             "week": pd.Series(dtype="datetime64[ns]"), "demand": pd.Series(dtype="float64")})
    agg = (m.groupby(KEYS + ["week"], as_index=False)["quantity"].sum()
             .rename(columns={"quantity": "demand"}))
    first = agg.groupby(KEYS, as_index=False)["week"].min().rename(columns={"week": "first_week"})
    weeks = pd.DataFrame({"week": pd.date_range(first["first_week"].min(), last_week, freq="7D")})
    full = first.merge(weeks, how="cross")
    full = full[full["week"] >= full["first_week"]].drop(columns="first_week")
    panel = full.merge(agg, on=KEYS + ["week"], how="left")
    panel["demand"] = panel["demand"].fillna(0.0).astype(float)
    return panel.sort_values(KEYS + ["week"]).reset_index(drop=True)


def location_kinds(stocks: pd.DataFrame) -> dict:
    """location_id -> 'pharmacy' | 'health_unit' (necessário porque o backend modela os dois como entidades distintas)."""
    s = add_location(stocks).dropna(subset=["location_id"]).drop_duplicates("location_id")
    return dict(zip(s["location_id"], s["location_kind"]))
