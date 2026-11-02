"""Gerador de dados sintéticos com sazonalidade, intermitência e desbalanço de estoque (demo e testes)."""
from __future__ import annotations

import uuid

import numpy as np
import pandas as pd

from .sources import RawData


def make_synthetic(seed: int = 7, n_locations: int = 6, n_medicines: int = 14, weeks: int = 130,
                   as_of: str = "2026-09-28", with_coords: bool = False) -> RawData:
    rng = np.random.default_rng(seed)
    as_of_ts = pd.Timestamp(as_of)
    last_week = as_of_ts - pd.Timedelta(weeks=1)
    week_starts = pd.date_range(end=last_week, periods=weeks, freq="7D")
    uid = lambda: str(uuid.UUID(int=int(rng.integers(0, 2**63)) << 64 | int(rng.integers(0, 2**63))))  # noqa: E731

    locs = [uid() for _ in range(n_locations)]
    meds = [uid() for _ in range(n_medicines)]
    # Espelha o schema real: Address sem latitude/longitude, apenas CEP/bairro/zona (coordenadas só se pedidas).
    zips = ["17500-010", "17500-300", "17509-110", "17525-050", "17560-200", "18000-100"]
    locations = pd.DataFrame({
        "location_id": locs, "name": [f"Unidade {i}" for i in range(n_locations)],
        "kind": ["pharmacy" if i < n_locations // 2 else "health_unit" for i in range(n_locations)],
        "zip_code": [zips[i % len(zips)] for i in range(n_locations)],
        "neighborhood": [f"Bairro {i}" for i in range(n_locations)], "zone": "Urbana"})
    if with_coords:
        locations["latitude"] = -22.2 + rng.normal(0, 0.15, n_locations)
        locations["longitude"] = -49.95 + rng.normal(0, 0.15, n_locations)
    medicines = pd.DataFrame({"medicine_id": meds, "name": [f"Medicamento {i}" for i in range(n_medicines)]})

    woy = week_starts.isocalendar().week.to_numpy().astype(float)
    stocks, batches, movs = [], [], []
    for mi, med in enumerate(meds):
        amp = rng.uniform(0, 0.6) if mi % 3 == 0 else 0.05          # 1/3 dos remédios com sazonalidade (inverno)
        season = 1 + amp * np.cos(2 * np.pi * (woy - 28) / 52.18)
        for li, loc in enumerate(locs):
            base = float(np.exp(rng.normal(2.3, 1.0)))                # escala heterogênea entre séries
            if rng.random() < 0.2:
                base *= 0.12                                          # série intermitente
            trend = 1 + 0.002 * np.arange(weeks)
            lam = base * season * trend
            demand = rng.negative_binomial(3, 3 / (3 + lam))
            # estoques "históricos" (soft-deleted) que carregam o histórico de movimentos
            hist = []
            for _ in range(2):
                sid, bid = uid(), uid()
                is_pharm = li < n_locations // 2
                stocks.append(dict(stock_id=sid, pharmacy_id=loc if is_pharm else None,
                                   health_unit_id=None if is_pharm else loc, batch_id=bid, quantity=0,
                                   removed_at=as_of_ts - pd.Timedelta(days=int(rng.integers(30, 300)))))
                batches.append(dict(batch_id=bid, medicine_id=med, batch_number=f"H{bid[:6]}",
                                    fabrication_date=as_of_ts - pd.Timedelta(days=700),
                                    expiration_date=as_of_ts - pd.Timedelta(days=60), removed_at=pd.NaT))
                hist.append((sid, bid))
            for w, q in zip(week_starts, demand):
                if q <= 0:
                    continue
                parts = rng.multinomial(int(q), np.ones(min(3, int(q))) / min(3, int(q)))
                for part in parts:
                    sid, bid = hist[int(rng.integers(0, 2))]
                    movs.append(dict(movement_id=uid(), created_at=w + pd.Timedelta(days=int(rng.integers(0, 7)),
                                     hours=int(rng.integers(8, 18))), stock_id=sid, batch_id=bid,
                                     medicine_id=med, type="Exit", quantity=int(part)))
            # ruído: perdas e transferências que NÃO podem virar demanda
            for _ in range(3):
                sid, bid = hist[0]
                movs.append(dict(movement_id=uid(), created_at=week_starts[int(rng.integers(0, weeks))],
                                 stock_id=sid, batch_id=bid, medicine_id=med,
                                 type=str(rng.choice(["TransferOut", "Loss"])), quantity=int(rng.integers(5, 40))))
            # estoque atual: alguns locais sobrestocados com lotes curtos, outros subestocados
            mult = rng.choice([0.3, 1.0, 2.5, 7.0], p=[0.25, 0.3, 0.25, 0.2])
            for _ in range(int(rng.integers(1, 3))):
                sid, bid = uid(), uid()
                qty = int(max(1, base * mult * rng.uniform(0.5, 1.5)))
                is_pharm = li < n_locations // 2
                stocks.append(dict(stock_id=sid, pharmacy_id=loc if is_pharm else None,
                                   health_unit_id=None if is_pharm else loc, batch_id=bid, quantity=qty,
                                   removed_at=pd.NaT))
                batches.append(dict(batch_id=bid, medicine_id=med, batch_number=f"B{bid[:6]}",
                                    fabrication_date=as_of_ts - pd.Timedelta(days=200),
                                    expiration_date=as_of_ts + pd.Timedelta(days=int(rng.integers(10, 140))), removed_at=pd.NaT))
    return RawData(movements=pd.DataFrame(movs), stocks=pd.DataFrame(stocks), batches=pd.DataFrame(batches),
                   medicines=medicines, locations=locations)
