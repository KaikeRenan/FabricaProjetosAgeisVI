"""Modelo global LightGBM: média (Tweedie) + quantil superior (p90) para incerteza."""
from __future__ import annotations

import warnings

import lightgbm as lgb
import numpy as np
import pandas as pd

from .config import ModelConfig
from .features import numeric_feature_columns


class GlobalDemandForecaster:
    def __init__(self, cfg: ModelConfig):
        self.cfg = cfg
        self.loc_index: dict = {}
        self.med_index: dict = {}
        self.mean_model: lgb.LGBMRegressor | None = None
        self.q_model: lgb.LGBMRegressor | None = None
        self.feature_names: list[str] = []

    # ---- encoding -------------------------------------------------------------------------
    @staticmethod
    def _cat(ids: pd.Series, index: dict) -> pd.Categorical:
        codes = ids.map(index).fillna(-1).astype(int).to_numpy()   # id desconhecido (cold start) -> NaN
        return pd.Categorical.from_codes(codes, categories=list(range(len(index))))

    def _X(self, df: pd.DataFrame) -> pd.DataFrame:
        X = df[numeric_feature_columns(self.cfg)].copy()
        X["location_code"] = self._cat(df["location_id"], self.loc_index)
        X["medicine_code"] = self._cat(df["medicine_id"], self.med_index)
        return X

    # ---- training -------------------------------------------------------------------------
    def _params(self, objective: str, **extra) -> dict:
        c = self.cfg
        return dict(objective=objective, n_estimators=c.n_estimators, learning_rate=c.learning_rate,
                    num_leaves=c.num_leaves, min_child_samples=c.min_child_samples,
                    subsample=c.subsample, subsample_freq=1, colsample_bytree=c.colsample_bytree,
                    random_state=c.seed, deterministic=True, force_row_wise=True, n_jobs=4,
                    verbose=-1, **extra)

    def _fit_one(self, X, y, is_val, **params) -> lgb.LGBMRegressor:
        # 1) early stopping em holdout TEMPORAL; 2) refit em tudo com o nº ótimo de árvores
        m = lgb.LGBMRegressor(**params)
        with warnings.catch_warnings():   # 'eval_set' é o nome estável entre versões; só silencia o aviso de depreciação
            warnings.simplefilter("ignore", category=lgb.sklearn.LGBMDeprecationWarning)
            m.fit(X[~is_val], y[~is_val], eval_set=[(X[is_val], y[is_val])],
                  callbacks=[lgb.early_stopping(self.cfg.early_stopping_rounds, verbose=False)])
        best = int(m.best_iteration_ or self.cfg.n_estimators)
        final = lgb.LGBMRegressor(**{**params, "n_estimators": max(best, 20)})
        final.fit(X, y)
        return final

    def fit(self, train: pd.DataFrame) -> "GlobalDemandForecaster":
        c = self.cfg
        train = train.sort_values("target_week").reset_index(drop=True)
        self.loc_index = {k: i for i, k in enumerate(sorted(train["location_id"].unique(), key=str))}
        self.med_index = {k: i for i, k in enumerate(sorted(train["medicine_id"].unique(), key=str))}
        X, y = self._X(train), train["y"].to_numpy()
        self.feature_names = list(X.columns)
        cutoff = train["target_week"].max() - pd.Timedelta(weeks=c.val_weeks)
        is_val = (train["target_week"] > cutoff).to_numpy()
        self.mean_model = self._fit_one(X, y, is_val, **self._params(
            "tweedie", tweedie_variance_power=c.tweedie_variance_power))
        self.q_model = self._fit_one(X, y, is_val, **self._params("quantile", alpha=c.quantile))
        return self

    # ---- inference ------------------------------------------------------------------------
    def predict(self, df: pd.DataFrame) -> pd.DataFrame:
        X = self._X(df)
        mean = np.clip(self.mean_model.predict(X), 0, None)
        q = np.maximum(np.clip(self.q_model.predict(X), 0, None), mean)   # garante q >= média
        return pd.DataFrame({"mean": mean, "q90": q}, index=df.index)

    def feature_importance(self) -> dict[str, float]:
        imp = self.mean_model.booster_.feature_importance(importance_type="gain")
        tot = float(imp.sum()) or 1.0
        return dict(sorted(zip(self.feature_names, (imp / tot).round(4).tolist()), key=lambda kv: -kv[1]))
