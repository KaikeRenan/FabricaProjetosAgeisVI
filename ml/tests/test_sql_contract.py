"""Garante que as consultas do SqlSource casam com o schema EF Core (nomes snake_case de Context.cs).
Usa SQLite em memória com as MESMAS tabelas/colunas do mapeamento — não substitui um teste contra PostgreSQL."""
import pandas as pd
import pytest
from sqlalchemy import create_engine
from sqlalchemy.pool import StaticPool

from redistml.config import DataConfig
from redistml.panel import build_demand_panel
from redistml.sources import SqlSource


@pytest.fixture()
def engine(raw):
    eng = create_engine("sqlite://", poolclass=StaticPool, connect_args={"check_same_thread": False})
    m = raw.movements.rename(columns={"movement_id": "id"}).assign(removed_at=None)
    m.to_sql("stock_movements", eng, index=False)
    raw.stocks.rename(columns={"stock_id": "id"}).to_sql("stocks", eng, index=False)
    raw.batches.rename(columns={"batch_id": "id"}).to_sql("batches", eng, index=False)
    raw.medicines.rename(columns={"medicine_id": "id"}).assign(removed_at=None).to_sql("medicines", eng, index=False)
    for kind, table in (("pharmacy", "pharmacies"), ("health_unit", "health_units")):
        loc = raw.locations[raw.locations["kind"] == kind].rename(columns={"location_id": "id"})
        loc[["id", "name", "zip_code", "neighborhood", "zone"]].assign(removed_at=None).to_sql(table, eng, index=False)
    return eng


def test_sql_roundtrip_preserves_demand_and_snapshot(raw, engine):
    loaded = SqlSource(engine=engine).load()
    a = build_demand_panel(raw, DataConfig())["demand"].sum()
    b = build_demand_panel(loaded, DataConfig())["demand"].sum()
    assert a == b
    assert set(loaded.locations["kind"]) == {"pharmacy", "health_unit"}
    assert loaded.locations["zip_code"].notna().all()
    assert loaded.stocks["removed_at"].notna().any() and loaded.stocks["removed_at"].isna().any()
    assert loaded.batches["expiration_date"].dtype.kind == "M"          # datetime ingênuo, sem tz
