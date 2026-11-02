import numpy as np
import pandas as pd

from redistml.config import DataConfig
from redistml.features import build_origin_features, stack_horizons, numeric_feature_columns
from redistml.panel import KEYS, build_demand_panel


def test_only_dispense_is_demand(raw, panel):
    # perdas (4) e transferências (3) existem nos dados mas não podem entrar na demanda
    disp = raw.movements[raw.movements["type"] == "Exit"]["quantity"].sum()
    assert panel["demand"].sum() == disp
    assert raw.movements["type"].isin(["TransferOut", "Loss"]).any()


def test_panel_complete_and_no_partial_week(panel):
    counts = panel.groupby(KEYS)["week"].agg(["min", "max", "count"])
    assert (counts["max"] == panel["week"].max()).all()
    span = ((counts["max"] - counts["min"]).dt.days // 7 + 1)
    assert (span == counts["count"]).all()           # zeros explícitos, sem buracos


def test_features_have_no_future_leakage(panel, mcfg):
    """Truncar o futuro não pode alterar features de uma origem passada."""
    T = panel["week"].max() - pd.Timedelta(weeks=20)
    full = build_origin_features(panel, mcfg)
    trunc = build_origin_features(panel[panel["week"] <= T], mcfg)
    cols = [c for c in numeric_feature_columns(mcfg) if c not in ("h", "t_woy_sin", "t_woy_cos", "t_month")]
    a = full[full["week"] == T].set_index(KEYS)[cols].sort_index()
    b = trunc[trunc["week"] == T].set_index(KEYS)[cols].sort_index()
    pd.testing.assert_frame_equal(a, b, check_exact=False, atol=1e-9)


def test_target_alignment(panel, mcfg):
    t = stack_horizons(build_origin_features(panel, mcfg), mcfg, with_target=True)
    lookup = panel.set_index(KEYS + ["week"])["demand"]
    s = t.sample(200, random_state=0)
    expected = lookup.loc[list(zip(s["location_id"], s["medicine_id"], s["target_week"]))].to_numpy()
    np.testing.assert_allclose(s["y"].to_numpy(), expected)
    assert (t["target_week"] > t["week"]).all()


def test_default_as_of_does_not_drop_a_closed_weekend_week():
    import pandas as pd

    from redistml.panel import default_as_of
    from redistml.sources import RawData

    def raw_ending(ts):
        m = pd.DataFrame({"created_at": [pd.Timestamp(ts)]})
        return RawData(m, None, None)

    assert default_as_of(raw_ending("2026-10-24 15:00")) == pd.Timestamp("2026-10-26")   # sábado -> segunda seguinte
    assert default_as_of(raw_ending("2026-10-25 09:00")) == pd.Timestamp("2026-10-26")   # domingo
    assert default_as_of(raw_ending("2026-10-22 15:00")) == pd.Timestamp("2026-10-23")   # quinta: semana ainda aberta
