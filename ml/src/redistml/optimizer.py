"""Previsão de demanda -> decisão de remanejamento (de qual unidade, para qual, qual lote, quanto)."""
from __future__ import annotations

import math
from dataclasses import dataclass

import numpy as np
import pandas as pd

from .config import OptimizerConfig
from .panel import KEYS

Z90 = 1.2816


@dataclass
class OptimizationResult:
    recommendations: pd.DataFrame
    residual_waste: pd.DataFrame     # desperdício que NÃO dá para evitar por remanejamento (doar/descontar/descartar)
    already_expired: pd.DataFrame
    no_forecast: pd.DataFrame        # pares com estoque mas sem histórico (cold start)
    summary: dict


def _daily(mean_w: np.ndarray, sig_w: np.ndarray, z: float) -> np.ndarray:
    return np.repeat(np.maximum(mean_w + z * sig_w, 0.0), 7) / 7.0


def simulate_fefo(batches: list[tuple], daily_demand: np.ndarray):
    """batches: [(batch_id, qty, expiry_day)] ; consome FEFO dia a dia.
    Retorna (waste por lote que vence DENTRO do horizonte, demanda não atendida por dia)."""
    D = len(daily_demand)
    rem = {b: float(q) for b, q, _ in batches}
    exp = {b: int(e) for b, _, e in batches}
    order = sorted(rem, key=lambda b: exp[b])
    unmet = np.zeros(D)
    for d in range(D):
        need = float(daily_demand[d])
        for b in order:
            if need <= 1e-12:
                break
            if exp[b] > d and rem[b] > 0:
                take = min(rem[b], need)
                rem[b] -= take
                need -= take
        unmet[d] = need
    waste = {b: rem[b] for b in order if exp[b] <= D and rem[b] > 1e-9}
    return waste, unmet


def _zip_digits(z) -> str:
    return "".join(ch for ch in str(z) if ch.isdigit()) if z is not None and not pd.isna(z) else ""


def _zip_tier(za: str, zb: str) -> float | None:
    """PROXY de proximidade por prefixo de CEP (o Address do backend não tem latitude/longitude):
    1 = mesmo subsetor (5 dígitos), 2 = mesma região/cidade (3), 3 = mesma macro-região (2), 4 = distante."""
    if len(za) < 5 or len(zb) < 5:
        return None
    if za[:5] == zb[:5]:
        return 1.0
    if za[:3] == zb[:3]:
        return 2.0
    return 3.0 if za[:2] == zb[:2] else 4.0


def _haversine(a, b) -> float:
    la1, lo1, la2, lo2 = map(math.radians, (a[0], a[1], b[0], b[1]))
    h = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 6371.0 * 2 * math.asin(math.sqrt(h))


def optimize(forecast: pd.DataFrame, snapshot: pd.DataFrame, as_of, cfg: OptimizerConfig,
             locations: pd.DataFrame | None = None) -> OptimizationResult:
    """forecast: location_id, medicine_id, h, mean, q90
       snapshot: location_id, medicine_id, batch_id, batch_number, quantity, expiration_date"""
    as_of = pd.Timestamp(as_of).normalize()
    H = int(forecast["h"].max())
    D = H * 7

    coords, zips = {}, {}
    if locations is not None:
        if {"latitude", "longitude"} <= set(locations.columns):
            coords = {r.location_id: (r.latitude, r.longitude) for r in locations.itertuples()
                      if pd.notna(r.latitude) and pd.notna(r.longitude)}
        if "zip_code" in locations.columns:
            zips = {r.location_id: _zip_digits(r.zip_code) for r in locations.itertuples()}

    def pair_cost(a, b) -> tuple[float, float | None]:
        """(custo usado para ordenar destinos, distância em km quando há coordenadas)."""
        if a in coords and b in coords:
            km = _haversine(coords[a], coords[b])
            return km, km
        tier = _zip_tier(zips.get(a, ""), zips.get(b, ""))
        return (tier if tier is not None else 1.0), None

    snap = snapshot[snapshot["quantity"] > 0].copy()
    snap["expiry_day"] = (snap["expiration_date"].dt.normalize() - as_of).dt.days
    expired = snap[snap["expiry_day"] <= 0]
    live = snap[snap["expiry_day"] > 0]

    # séries semanais (média, sigma) por (local, medicamento)
    series: dict[tuple, tuple[np.ndarray, np.ndarray]] = {}
    for key, g in forecast.sort_values("h").groupby(KEYS, sort=False):
        if len(g) != H:
            continue
        mean = g["mean"].to_numpy()
        series[key] = (mean, np.maximum(g["q90"].to_numpy() - mean, 0.0) / Z90)

    stock_by_pair = {k: g for k, g in live.groupby(KEYS, sort=False)}
    no_fc = pd.concat([g for k, g in stock_by_pair.items() if k not in series], ignore_index=True) \
        if any(k not in series for k in stock_by_pair) else live.iloc[0:0]

    # 1) DOADORES: unidades que vencerão sem serem consumidas (cenário de demanda do doador)
    donors = []
    at_risk_total = 0.0
    for key, g in stock_by_pair.items():
        if key not in series:
            continue
        m, s = series[key]
        waste, _ = simulate_fefo([(r.batch_id, r.quantity, r.expiry_day) for r in g.itertuples()],
                                 _daily(m, s, cfg.donor_demand_z))
        info = g.set_index("batch_id")
        for b, w in waste.items():
            at_risk_total += w
            donors.append(dict(location_id=key[0], medicine_id=key[1], batch_id=b,
                               batch_number=info.loc[b, "batch_number"],
                               expiry_day=int(info.loc[b, "expiry_day"]),
                               expiration_date=info.loc[b, "expiration_date"],
                               at_risk=w, surplus=int(math.floor(w + 1e-9))))

    # 2) RECEPTORES: demanda projetada que o próprio estoque não cobre (por dia)
    unmet: dict[tuple, np.ndarray] = {}
    receivers_by_med: dict = {}
    for key, (m, s) in series.items():
        g = stock_by_pair.get(key)
        batches = [] if g is None else [(r.batch_id, r.quantity, r.expiry_day) for r in g.itertuples()]
        _, u = simulate_fefo(batches, _daily(m, s, cfg.receiver_demand_z))
        if u.sum() > 0:
            unmet[key] = u
            receivers_by_med.setdefault(key[1], []).append(key[0])
    unmet_before = {k: v.sum() for k, v in unmet.items()}

    # 3) CASAMENTO GULOSO: lote mais urgente primeiro; destino mais próximo que consegue consumir antes de vencer
    arr = int(cfg.lead_time_days)
    recs, residual = [], []
    for d in sorted(donors, key=lambda x: (x["expiry_day"], -x["surplus"])):
        left, e = d["surplus"], d["expiry_day"]
        cands = []
        if e - arr >= cfg.min_shelf_life_days and left >= cfg.min_transfer_qty:
            for dest in receivers_by_med.get(d["medicine_id"], []):
                if dest == d["location_id"]:
                    continue
                cost, km = pair_cost(d["location_id"], dest)
                if cfg.max_distance_km is not None and km is not None and km > cfg.max_distance_km:
                    continue
                cap = int(math.floor(unmet[(dest, d["medicine_id"])][arr:e].sum() + 1e-9))
                if cap >= cfg.min_transfer_qty:
                    cands.append((cost, -cap, dest, km))
        for cost, _, dest, km in sorted(cands, key=lambda c: (c[0], c[1], str(c[2]))):
            u = unmet[(dest, d["medicine_id"])]
            cap = int(math.floor(u[arr:e].sum() + 1e-9))
            q = min(left, cap)
            if q < cfg.min_transfer_qty:
                continue
            todo = float(q)
            for day in range(arr, e):      # consome a demanda não atendida cronologicamente
                take = min(u[day], todo)
                u[day] -= take
                todo -= take
                if todo <= 1e-9:
                    break
            recs.append(dict(
                medicine_id=d["medicine_id"], batch_id=d["batch_id"], batch_number=d["batch_number"],
                from_location_id=d["location_id"], to_location_id=dest, quantity=int(q),
                expiration_date=d["expiration_date"], days_to_expiry=e, shelf_life_at_arrival=e - arr,
                distance_km=round(km, 1) if km is not None else None, proximity_cost=round(cost, 2),
                projected_waste_at_source=round(d["at_risk"], 1)))
            left -= q
            if left < cfg.min_transfer_qty:
                break
        if d["at_risk"] - (d["surplus"] - left) > 1e-9:
            residual.append(dict(location_id=d["location_id"], medicine_id=d["medicine_id"],
                                 batch_id=d["batch_id"], batch_number=d["batch_number"],
                                 expiration_date=d["expiration_date"], days_to_expiry=e,
                                 unavoidable_waste=round(d["at_risk"] - (d["surplus"] - left), 1)))

    R = pd.DataFrame(recs)
    rescued = float(R["quantity"].sum()) if len(R) else 0.0
    summary = dict(units_at_risk=round(at_risk_total, 1), units_rescued=rescued,
                   rescue_rate=round(rescued / at_risk_total, 3) if at_risk_total else None,
                   n_transfers=len(R), unmet_demand_before=round(sum(unmet_before.values()), 1))
    return OptimizationResult(R, pd.DataFrame(residual), expired, no_fc, summary)
