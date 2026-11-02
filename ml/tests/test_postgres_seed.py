"""Teste em PostgreSQL REAL (opcional). Pré-requisito: banco com tests/pg/schema.sql e o seed.sql do mock aplicados.
Local:  PG_TEST_URL='postgresql+psycopg://user:pw@localhost:5432/mvp' PG_TEST_CSV_DIR=data/mock pytest tests/test_postgres_seed.py
CI:     veja .github/workflows/ci.yml (job `postgres`)."""
import os

import pandas as pd
import pytest

from redistml.config import DataConfig
from redistml.panel import build_demand_panel
from redistml.pipeline import current_snapshot
from redistml.sources import CsvSource, SqlSource

URL = os.getenv("PG_TEST_URL")
CSV_DIR = os.getenv("PG_TEST_CSV_DIR", "data/mock")
pytestmark = pytest.mark.skipif(not URL, reason="defina PG_TEST_URL (PostgreSQL com schema + seed aplicados)")


@pytest.fixture(scope="module")
def both():
    return SqlSource(URL).load(), CsvSource(CSV_DIR).load()


def test_row_counts_and_id_types(both):
    pg, cs = both
    for name in ("movements", "stocks", "batches", "medicines", "locations"):
        assert len(getattr(pg, name)) == len(getattr(cs, name)), name
    assert isinstance(pg.movements["stock_id"].iloc[0], str)            # uuid -> str (psycopg devolve uuid.UUID)
    assert pg.movements["created_at"].dt.tz is None and pg.batches["expiration_date"].dt.tz is None


def test_weekly_demand_panel_is_identical_to_csv(both):
    pg, cs = both
    a, b = build_demand_panel(pg, DataConfig()), build_demand_panel(cs, DataConfig())
    m = a.merge(b, on=["location_id", "medicine_id", "week"], suffixes=("_pg", "_csv"))
    assert len(a) == len(b) == len(m)
    assert (m["demand_pg"] == m["demand_csv"]).all()                    # fuso America/Sao_Paulo ida-e-volta sem deslocar semanas


def test_current_snapshot_is_identical_to_csv(both):
    pg, cs = both
    key = ["location_id", "batch_id", "quantity"]
    a = current_snapshot(pg)[key].sort_values(key).reset_index(drop=True)
    b = current_snapshot(cs)[key].sort_values(key).reset_index(drop=True)
    pd.testing.assert_frame_equal(a, b)
    exp_pg = current_snapshot(pg).set_index("batch_id")["expiration_date"].sort_index()
    exp_cs = current_snapshot(cs).set_index("batch_id")["expiration_date"].sort_index()
    pd.testing.assert_series_equal(exp_pg, exp_cs, check_dtype=False, check_names=False)   # validade sem "voltar um dia"
