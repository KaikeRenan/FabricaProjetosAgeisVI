import json

import pytest

from redistml.config import BacktestConfig
from redistml.evaluation import rolling_origin_backtest
from redistml.pipeline import load_bundle, recommend, train
from redistml.registry import ModelRegistry

BT = BacktestConfig(n_splits=2, step_weeks=4)


def test_backtest_has_no_target_leakage(panel, mcfg):
    bt = rolling_origin_backtest(panel, mcfg, BT)
    for fold, T in enumerate(bt.origins):
        p = bt.predictions[bt.predictions["fold"] == fold]
        assert (p["week"] == T).all() and (p["target_week"] > T).all()
    assert bt.origins[-1] + mcfg.horizon_weeks * (panel["week"].iloc[1] - panel["week"].iloc[0]) <= panel["week"].max()


def test_train_registers_immutable_versions_and_is_reproducible(raw, mcfg, tmp_path):
    m1 = train(raw, tmp_path, mcfg=mcfg, bcfg=BT)
    reg = ModelRegistry(tmp_path)
    assert reg.current_version() == m1["model_version"]
    assert (reg.version_dir(m1["model_version"]) / "metadata.json").exists()
    assert 0 < m1["coverage_q90"] <= 1
    m2 = train(raw, tmp_path, mcfg=mcfg, bcfg=BT, set_current=False)
    assert m1["backtest_summary"] == m2["backtest_summary"]            # mesma seed + mesmos dados => mesmas métricas
    assert m1["data_fingerprint"] == m2["data_fingerprint"]
    assert reg.current_version() == m1["model_version"]                # set_current=False não move o ponteiro
    with pytest.raises(FileExistsError):                               # versão existente jamais é sobrescrita
        reg.new_version(m1["model_version"])


def test_gate_decides_tier_and_recommend_never_serves_unpromoted_model(raw, mcfg, tmp_path):
    meta = train(raw, tmp_path, mcfg=mcfg, bcfg=BT)
    bundle = load_bundle(tmp_path)
    assert bundle.tier == ("lgbm" if meta["promotion"]["promoted"] else "baseline")
    out = recommend(raw, tmp_path)
    assert out.run["serving_tier"] == bundle.tier
    assert out.result.summary["units_rescued"] <= out.result.summary["units_at_risk"] + 1e-6
    if bundle.tier == "baseline":
        assert out.run["status"] == "degraded" and any("baseline" in w for w in out.run["warnings"])


def test_insufficient_history_falls_back_to_baseline_without_backtest(raw, mcfg, tmp_path):
    from redistml.config import DataConfig
    short = raw.__class__(movements=raw.movements[raw.movements["created_at"] >= "2026-07-20"],
                          stocks=raw.stocks, batches=raw.batches, medicines=raw.medicines, locations=raw.locations)
    meta = train(short, tmp_path, mcfg=mcfg, bcfg=BT, data_cfg=DataConfig())
    assert meta["serving_tier"] == "baseline" and "insuficiente" in meta["promotion"]["reason"]
    assert meta["backtest_summary"] is None
    assert not (ModelRegistry(tmp_path).version_dir(meta["model_version"]) / "model.joblib").exists()
    out = recommend(short, tmp_path)                                    # serviço funciona desde o dia 1
    assert out.run["serving_tier"] == "baseline" and out.run["status"] == "degraded"
    assert any("insuficiente" in w for w in out.run["warnings"])
    assert out.result.summary["unmet_demand_before"] > 0 and out.result.no_forecast.empty   # baseline alimenta o otimizador


def test_stale_data_is_flagged(raw, mcfg, tmp_path):
    train(raw, tmp_path, mcfg=mcfg, bcfg=BT)
    out = recommend(raw, tmp_path, as_of=raw.movements["created_at"].max() + __import__("pandas").Timedelta(days=60))
    assert out.run["status"] == "degraded" and any("defasados" in w for w in out.run["warnings"])


def test_paired_bootstrap_gain_is_consistent_and_deterministic():
    import numpy as np
    import pandas as pd

    from redistml.evaluation import paired_bootstrap_gain
    rng = np.random.default_rng(1)
    n_series, n = 40, 25
    y = rng.poisson(50, n_series * n).astype(float)
    df = pd.DataFrame({"location_id": np.repeat(np.arange(n_series), n), "medicine_id": "m", "y": y,
                       "good": y + rng.normal(0, 3, y.size), "bad": y + rng.normal(0, 12, y.size)})
    a = paired_bootstrap_gain(df, "good", "bad", n_boot=400, seed=3)
    assert a == paired_bootstrap_gain(df, "good", "bad", n_boot=400, seed=3)          # determinístico
    assert a["ic95"][0] <= a["ganho_relativo"] <= a["ic95"][1] and a["ic95"][0] > 0   # melhor de verdade => IC > 0
    b = paired_bootstrap_gain(df, "bad", "good", n_boot=400, seed=3)
    assert b["ic95"][1] < 0 and b["prob_ganho_positivo"] < 0.05                       # pior => IC < 0
