"""Métricas e backtest de origem móvel (rolling-origin) com baselines."""
from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd

from .baselines import tsb_forecast
from .config import BacktestConfig, ModelConfig
from .features import build_origin_features, stack_horizons
from .model import GlobalDemandForecaster
from .panel import KEYS


def wape(y, yhat) -> float:
    d = float(np.sum(np.abs(y)))
    return float(np.sum(np.abs(y - yhat)) / d) if d > 0 else float("nan")


def bias(y, yhat) -> float:
    """(Σŷ-Σy)/Σy. >0 superestima (risco de sobra/vencimento); <0 subestima (risco de ruptura)."""
    d = float(np.sum(y))
    return float((np.sum(yhat) - d) / d) if d > 0 else float("nan")


def pinball(y, q, alpha: float) -> float:
    diff = y - q
    return float(np.mean(np.maximum(alpha * diff, (alpha - 1) * diff)))


@dataclass
class BacktestResult:
    predictions: pd.DataFrame
    summary: pd.DataFrame       # por modelo, agregado
    by_fold: pd.DataFrame
    by_horizon: pd.DataFrame    # só LightGBM
    coverage_q90: float
    pinball_q90: float
    origins: list


def make_origins(last_week: pd.Timestamp, mcfg: ModelConfig, bcfg: BacktestConfig) -> list[pd.Timestamp]:
    n = bcfg.n_splits
    return [last_week - pd.Timedelta(weeks=mcfg.horizon_weeks + (n - 1 - i) * bcfg.step_weeks) for i in range(n)]


def rolling_origin_backtest(panel: pd.DataFrame, mcfg: ModelConfig, bcfg: BacktestConfig) -> BacktestResult:
    table = stack_horizons(build_origin_features(panel, mcfg), mcfg, with_target=True)
    origins = make_origins(panel["week"].max(), mcfg, bcfg)
    preds = []
    for i, T in enumerate(origins):
        train = table[table["target_week"] <= T]        # nenhum alvo > T entra no treino (anti-vazamento)
        test = table[table["week"] == T].copy()
        if train["target_week"].nunique() < mcfg.min_train_weeks:
            raise ValueError(f"Histórico insuficiente na origem {T.date()} (< {mcfg.min_train_weeks} semanas de alvos).")
        model = GlobalDemandForecaster(mcfg).fit(train)
        p = model.predict(test)
        test["lgbm"], test["q90"] = p["mean"], p["q90"]
        test["ma8"] = test["rmean_8"]
        test["naive"] = test["lag_0"]
        test = test.merge(tsb_forecast(panel[panel["week"] <= T]), on=KEYS, how="left")
        test["fold"] = i
        preds.append(test[KEYS + ["fold", "week", "h", "target_week", "y", "lgbm", "q90", "ma8", "naive", "tsb"]])
    P = pd.concat(preds, ignore_index=True)

    models = ["lgbm", "ma8", "tsb", "naive"]
    rows = [(m, f, wape(g["y"], g[m]), bias(g["y"], g[m]), float(np.mean(np.abs(g["y"] - g[m]))))
            for f, g in P.groupby("fold") for m in models]
    by_fold = pd.DataFrame(rows, columns=["model", "fold", "wape", "bias", "mae"])
    summary = pd.DataFrame([(m, wape(P["y"], P[m]), bias(P["y"], P[m]), float(np.mean(np.abs(P["y"] - P[m]))))
                            for m in models], columns=["model", "wape", "bias", "mae"]).set_index("model")
    by_h = (P.groupby("h").apply(lambda g: pd.Series({"wape": wape(g["y"], g["lgbm"]),
                                                      "bias": bias(g["y"], g["lgbm"])}), include_groups=False))
    return BacktestResult(P, summary, by_fold, by_h, float((P["y"] <= P["q90"]).mean()),
                          pinball(P["y"].to_numpy(), P["q90"].to_numpy(), mcfg.quantile), origins)


def paired_bootstrap_gain(predictions: pd.DataFrame, model: str, baseline: str, n_boot: int = 1000,
                          seed: int = 0) -> dict:
    """Ganho relativo de WAPE (baseline→modelo) com IC95 por *cluster bootstrap* sobre SÉRIES (local×medicamento).
    Reamostrar linhas soltas subestimaria a incerteza: as previsões da mesma série são correlacionadas."""
    g = predictions.assign(e_m=(predictions["y"] - predictions[model]).abs(),
                           e_b=(predictions["y"] - predictions[baseline]).abs()).groupby(KEYS)[["e_m", "e_b", "y"]].sum()
    em, eb, y = g["e_m"].to_numpy(), g["e_b"].to_numpy(), g["y"].to_numpy()
    point = 1.0 - em.sum() / eb.sum()
    rng = np.random.default_rng(seed)
    idx = rng.integers(0, len(g), size=(n_boot, len(g)))
    gains = 1.0 - em[idx].sum(axis=1) / eb[idx].sum(axis=1)
    lo, hi = np.percentile(gains, [2.5, 97.5])
    return {"ganho_relativo": round(float(point), 4), "ic95": [round(float(lo), 4), round(float(hi), 4)],
            "prob_ganho_positivo": round(float((gains > 0).mean()), 4), "series": int(len(g)), "n_boot": n_boot}


def promotion_decision(res: BacktestResult, bcfg: BacktestConfig) -> dict:
    """Champion/challenger: só promove se vencer o melhor baseline com folga E não tiver viés grande."""
    base = res.summary.drop(index="lgbm")
    best_name = base["wape"].idxmin()
    best, cand = float(base.loc[best_name, "wape"]), float(res.summary.loc["lgbm", "wape"])
    b = float(res.summary.loc["lgbm", "bias"])
    gain = (best - cand) / best if best > 0 else 0.0
    return {"promoted": bool(gain >= bcfg.min_improvement and abs(b) <= bcfg.max_abs_bias),
            "best_baseline": best_name, "baseline_wape": best, "lgbm_wape": cand,
            "relative_gain": gain, "lgbm_bias": b}
