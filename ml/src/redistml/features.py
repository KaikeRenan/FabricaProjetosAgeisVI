"""Engenharia de features SEM vazamento: toda feature da linha (série, origem t) usa apenas dados <= t."""
from __future__ import annotations

import numpy as np
import pandas as pd

from .config import ModelConfig
from .panel import KEYS


def _grp(df: pd.DataFrame, s: pd.Series):
    return s.groupby([df["location_id"], df["medicine_id"]], sort=False)


def build_origin_features(panel: pd.DataFrame, cfg: ModelConfig) -> pd.DataFrame:
    df = panel.sort_values(KEYS + ["week"]).reset_index(drop=True)
    out = df.copy()
    g = _grp(df, df["demand"])

    for k in cfg.lags:
        out[f"lag_{k}"] = g.shift(k)
    for w in cfg.rolling_windows:
        out[f"rmean_{w}"] = g.rolling(w, min_periods=1).mean().reset_index(level=[0, 1], drop=True)
    out["rstd_13"] = g.rolling(13, min_periods=2).std().reset_index(level=[0, 1], drop=True)

    # Intermitência: o que separa "demanda zero" de "série morta" para o modelo
    nz = (df["demand"] > 0).astype(float)
    out["nz_rate_13"] = _grp(df, nz).rolling(13, min_periods=1).mean().reset_index(level=[0, 1], drop=True)
    pos = g.cumcount()
    last_nz = _grp(df, pos.where(df["demand"] > 0)).ffill()
    out["weeks_since_nz"] = (pos - last_nz).fillna(pos + 1)
    out["series_age"] = pos
    out["expanding_mean"] = g.cumsum() / (pos + 1)

    # Sinal da rede: demanda média do medicamento em todas as unidades (ajuda séries curtas/esparsas)
    net = df.groupby(["medicine_id", "week"], as_index=False)["demand"].mean().sort_values(["medicine_id", "week"])
    for w in (4, 13):
        net[f"net_mean_{w}"] = (net.groupby("medicine_id")["demand"].rolling(w, min_periods=1).mean()
                                   .reset_index(level=0, drop=True))
    out = out.merge(net.drop(columns="demand"), on=["medicine_id", "week"], how="left")
    return out


def stack_horizons(feats: pd.DataFrame, cfg: ModelConfig, with_target: bool) -> pd.DataFrame:
    """Um único modelo global 'ciente do horizonte': linhas (origem t, horizonte h) -> y[t+h]."""
    g = _grp(feats, feats["demand"])
    frames = []
    for h in range(1, cfg.horizon_weeks + 1):
        f = feats.copy()
        f["h"] = h
        f["target_week"] = f["week"] + pd.Timedelta(weeks=h)
        if with_target:
            f["y"] = g.shift(-h)
            f = f[f["y"].notna()]
        frames.append(f)
    out = pd.concat(frames, ignore_index=True)
    woy = out["target_week"].dt.isocalendar().week.astype(int)
    out["t_woy_sin"] = np.sin(2 * np.pi * woy / 52.18)
    out["t_woy_cos"] = np.cos(2 * np.pi * woy / 52.18)
    out["t_month"] = out["target_week"].dt.month
    return out


def numeric_feature_columns(cfg: ModelConfig) -> list[str]:
    cols = [f"lag_{k}" for k in cfg.lags] + [f"rmean_{w}" for w in cfg.rolling_windows]
    cols += ["rstd_13", "nz_rate_13", "weeks_since_nz", "series_age", "expanding_mean",
             "net_mean_4", "net_mean_13", "h", "t_woy_sin", "t_woy_cos", "t_month"]
    return cols
