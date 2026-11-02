import re
from dataclasses import replace
from pathlib import Path

import numpy as np
import pandas as pd
import pytest

from redistml.mockdata import MockConfig, simulate
from redistml.mockdata.dgp import fake_cnpj
from redistml.mockdata.export import _COLS, write_backend_seed, write_local_csv
from redistml.mockdata.validate import ledger_checks, sampler_checks, validate_dataset

SMALL = MockConfig(weeks=70, n_pharmacies=2, n_health_units=3, n_medicines=10)


@pytest.fixture(scope="module")
def ds():
    return simulate(SMALL)


def test_reproducible_and_seed_sensitive(ds):
    again = simulate(SMALL)
    pd.testing.assert_frame_equal(ds.raw.movements, again.raw.movements)
    other = simulate(replace(SMALL, seed=SMALL.seed + 1))
    assert not ds.truth["true_demand"].equals(other.truth["true_demand"])


def test_ledger_integrity(ds):
    checks = ledger_checks(ds)
    assert all(checks.values()), [k for k, v in checks.items() if not v]


def test_demand_is_independent_of_operational_policy(ds):
    """RNG em fluxos independentes: mudar a política de reposição NÃO pode mudar a demanda verdadeira."""
    other = simulate(replace(SMALL, cover_weeks={"normal": (4.0, 0.4), "over": (9.0, 1.0), "tight": (2.0, 0.3)}))
    np.testing.assert_array_equal(ds.truth["true_demand"].to_numpy(), other.truth["true_demand"].to_numpy())
    assert not ds.truth["observed"].equals(other.truth["observed"])        # mas o OBSERVADO (censurado) muda


def test_observed_never_exceeds_true_demand_and_censoring_is_negative(ds):
    t = ds.truth
    assert (t["observed"] <= t["true_demand"]).all()
    assert (t["observed"] + t["unmet"] == t["true_demand"]).all()


def test_generator_matches_its_specification():
    big = simulate(replace(SMALL, weeks=104, n_medicines=14))
    r = sampler_checks(big.truth)
    lo, hi = r["pearson_ic95"]
    assert lo <= 1.0 <= hi, r                      # variância de Pearson ≈ 1 sob NB(λ, k) verdadeiros
    assert r["pit_ks_pvalor"] > 0.001, r           # PIT ≈ Uniforme(0,1)


def test_only_dispensing_is_demand_and_other_types_exist(ds):
    assert {"Entry", "Exit"} <= set(ds.raw.movements["type"])
    assert set(ds.raw.movements["type"]) <= {"Entry", "Exit", "TransferIn", "TransferOut", "Loss"}


def test_fake_cnpj_has_valid_check_digits():
    rng = np.random.default_rng(0)
    for _ in range(50):
        n = re.sub(r"\D", "", fake_cnpj(rng))
        assert len(n) == 14
        for pos, weights in ((12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]), (13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])):
            r = sum(int(d) * w for d, w in zip(n[:pos], weights)) % 11
            assert int(n[pos]) == (0 if r < 2 else 11 - r)


def test_validate_dataset_all_green(ds):
    v = validate_dataset(ds)
    assert v["todas_as_checagens_passaram"]
    assert v["demanda"]["censura"]["vies_da_demanda_observada"] <= 0


def test_export_layout_and_seed_script(ds, tmp_path):
    write_local_csv(ds, tmp_path)
    names = write_backend_seed(ds, tmp_path / "backend")
    sql = (tmp_path / "backend" / "seed.sql").read_text()
    assert sql.index("\\copy medicines") < sql.index("\\copy batches") < sql.index("\\copy stocks") < sql.index("\\copy stock_movements")
    assert "RAISE EXCEPTION" in sql                                          # guarda contra mistura com dados reais
    for n in names:
        df = pd.read_csv(tmp_path / "backend" / f"{n}.csv")
        assert list(df.columns) == [c.strip() for c in _COLS[n].split(",")]
    mv = pd.read_csv(tmp_path / "backend" / "stock_movements.csv")
    assert mv["created_at"].str.endswith("+00").all() and mv["type"].isin(["Entry", "Exit", "TransferIn", "TransferOut", "Loss"]).all()
    st = pd.read_csv(tmp_path / "backend" / "stocks.csv")
    assert (st["pharmacy_id"].notna() ^ st["health_unit_id"].notna()).all()


def test_seed_columns_exist_in_the_backend_ef_model():
    """Guarda contra deriva: toda coluna do seed deve existir no mapeamento real do EF (mvp/Data/Context.cs)."""
    ctx = Path(__file__).resolve().parents[2] / "mvp" / "Data" / "Context.cs"
    if not ctx.exists():
        pytest.skip("Context.cs não encontrado (pacote ML fora do monorepo)")
    text = ctx.read_text(encoding="utf-8-sig")
    mapped = set(re.findall(r'HasColumnName\("([a-z_]+)"\)', text))
    tables = set(re.findall(r'ToTable\("([a-z_]+)"\)', text))
    for table, cols in _COLS.items():
        assert table in tables, table
        missing = [c.strip() for c in cols.split(",") if c.strip() not in mapped]
        assert not missing, (table, missing)


def test_mock_feeds_the_real_pipeline(ds, tmp_path):
    from redistml.pipeline import recommend, train
    meta = train(ds.raw, tmp_path)                               # 70 semanas < 82 => tier baseline (comportamento esperado)
    assert meta["serving_tier"] == "baseline"
    out = recommend(ds.raw, tmp_path)
    assert out.run["as_of"] == pd.Timestamp(SMALL.as_of)
    assert out.result.summary["units_at_risk"] >= 0
    assert out.result.no_forecast.empty or len(out.result.no_forecast) < len(ds.raw.stocks)


def test_demo_events_only_add_stock_and_do_not_touch_demand():
    base = simulate(SMALL)
    ev = simulate(replace(SMALL, demo_events=4))
    np.testing.assert_array_equal(base.truth["true_demand"].to_numpy(), ev.truth["true_demand"].to_numpy())
    assert len(ev.meta["demo_events"]) == 4 and base.meta["demo_events"] == []
    assert all(ledger_checks(ev).values())
    added = ev.raw.movements["type"].eq("Entry").sum() - base.raw.movements["type"].eq("Entry").sum()
    assert added >= 4                                                  # entradas extras (a política pode reagir depois)
