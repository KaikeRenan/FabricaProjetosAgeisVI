"""Baselines obrigatórios: se o modelo não os vence com folga, ele não vai para produção."""
from __future__ import annotations

import numpy as np
import pandas as pd

from .panel import KEYS


def tsb_forecast(panel_upto: pd.DataFrame, alpha: float = 0.1, beta: float = 0.1) -> pd.DataFrame:
    """Teunter-Syntetos-Babai: padrão-ouro clássico para demanda intermitente (p = prob. de demanda, z = tamanho)."""
    rows = []
    for (loc, med), s in panel_upto.groupby(KEYS, sort=False)["demand"]:
        y = s.to_numpy()
        head = y[:8]
        p = float((head > 0).mean())
        z = float(head[head > 0].mean()) if (head > 0).any() else 1.0
        for v in y:
            p += beta * ((v > 0) - p)
            if v > 0:
                z += alpha * (v - z)
        rows.append((loc, med, p * z))
    return pd.DataFrame(rows, columns=KEYS + ["tsb"])


def baseline_forecast_next(panel: pd.DataFrame, horizon_weeks: int, z90: float = 1.2816) -> pd.DataFrame:
    """Tier 'baseline' (poucos dados / modelo não promovido): TSB para a média + desvio das últimas 13 semanas.
    Não captura sazonalidade nem tendência — por isso é rotulado como tier 'baseline' em todas as respostas."""
    rows = []
    for (loc, med), s in panel.groupby(KEYS, sort=False)["demand"]:
        y = s.to_numpy()
        head = y[:8]
        p = float((head > 0).mean())
        z = float(head[head > 0].mean()) if (head > 0).any() else 1.0
        for v in y:
            p += 0.1 * ((v > 0) - p)
            if v > 0:
                z += 0.1 * (v - z)
        mean = p * z
        tail = y[-13:]
        sd = float(np.std(tail, ddof=1)) if len(tail) >= 3 else float(np.sqrt(max(mean, 0.5)))
        q90 = mean + z90 * sd
        for h in range(1, horizon_weeks + 1):
            rows.append((loc, med, h, mean, q90))
    return pd.DataFrame(rows, columns=KEYS + ["h", "mean", "q90"])
