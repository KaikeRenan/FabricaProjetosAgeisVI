"""Simulador em 2 camadas.

Camada 1 — DEMANDA VERDADEIRA (independente de estoque):
    log λ[u,m,t] = log(pop_u/1000) + log(taxa_m) + a_um + b_uc + A_c·cos(2π(sem_t − pico_c)/52,18)
                   + tendência_c·t/52,18 + β_c·ℓ_u·W_t + η[u,m,t] + γ_c·log(dias_úteis_t/5)
    D[u,m,t] ~ NegBin(média=λ, dispersão=k_c)         (W_t: onda/surtos regionais compartilhados; η: AR(1))
Camada 2 — LIVRO-RAZÃO: reposição periódica com lead time, FEFO, ruptura (CENSURA da demanda), vencimento (Loss),
    transferências manuais históricas. O que o backend "vê" é a camada 2; a camada 1 é a verdade para validação.
"""
from __future__ import annotations

import math
import uuid
from dataclasses import dataclass, field

import numpy as np
import pandas as pd

from ..panel import monday
from ..sources import RawData
from .dgp import CATALOG, CLASS_PARAMS, MockConfig, br_holidays, fake_cnes, fake_cnpj

_NEIGHBORHOODS = ["Centro", "Jardim Primavera", "Vila Nova", "Jardim América", "Palmital", "Alto Cafezal",
                  "Bela Vista", "Santa Antonieta", "Cascata", "Jardim Tropical", "Parque Industrial", "Lácio"]
_STREETS = ["Rua das Acácias", "Av. Brasil", "Rua Sete de Setembro", "Rua dos Ipês", "Av. Rio Branco",
            "Rua das Palmeiras", "Rua Goiás", "Av. Carlos Gomes"]


@dataclass
class MockDataset:
    raw: RawData                      # o que o backend/ML enxergam (schema real)
    truth: pd.DataFrame               # verdade: true_mean, true_demand, observed, unmet por (local, medicamento, semana)
    pharmacies: pd.DataFrame          # entidades completas p/ seed do backend
    health_units: pd.DataFrame
    medicines: pd.DataFrame           # inclui classe terapêutica
    config: dict
    meta: dict = field(default_factory=dict)


def _ar1(rng: np.random.Generator, n: int, rho: float, sigma: float) -> np.ndarray:
    out = np.empty(n)
    out[0] = rng.normal(0, sigma / math.sqrt(1 - rho**2))     # inicia na distribuição estacionária
    for i in range(1, n):
        out[i] = rho * out[i - 1] + rng.normal(0, sigma)
    return out


def _regional_wave(rng: np.random.Generator, weeks: int, cfg: MockConfig) -> np.ndarray:
    w = _ar1(rng, weeks, cfg.wave_rho, cfg.wave_sigma)
    for _ in range(rng.poisson(cfg.outbreaks_per_year * weeks / 52.18)):
        start, dur = int(rng.integers(0, weeks)), int(rng.integers(3, 9))
        amp = rng.lognormal(math.log(cfg.outbreak_amp_median), cfg.outbreak_amp_sigma)
        bump = amp * np.sin(np.pi * (np.arange(dur) + 0.5) / dur) ** 2
        w[start:start + dur] += bump[: max(0, min(dur, weeks - start))]
    return w


class _Ids:
    def __init__(self, rng: np.random.Generator):
        self.rng = rng

    def __call__(self) -> str:
        return str(uuid.UUID(bytes=self.rng.bytes(16), version=4))


def simulate(cfg: MockConfig = MockConfig()) -> MockDataset:
    ss = np.random.SeedSequence(cfg.seed)
    r_dem, r_led, r_ids, r_ent = (np.random.default_rng(s) for s in ss.spawn(4))   # fluxos independentes
    new_id = _Ids(r_ids)

    as_of = pd.Timestamp(cfg.as_of)
    assert as_of.weekday() == 0, "as_of deve ser uma segunda-feira"
    W = cfg.weeks
    D = W * 7
    start = as_of - pd.Timedelta(days=D)
    week_starts = pd.date_range(start, periods=W, freq="7D")
    days = pd.date_range(start, periods=D, freq="D")

    # ---------------------------------------------------------------- calendário
    hol = br_holidays(range(start.year, as_of.year + 1))
    is_hol = np.array([d.date() in hol for d in days])
    weekday = days.weekday.to_numpy()
    biz_days = np.array([sum(1 for i in range(7) if weekday[w * 7 + i] < 5 and not is_hol[w * 7 + i]) for w in range(W)])
    woy = week_starts.isocalendar().week.to_numpy().astype(float)

    # ---------------------------------------------------------------- entidades
    n_loc = cfg.n_pharmacies + cfg.n_health_units
    kinds = ["pharmacy"] * cfg.n_pharmacies + ["health_unit"] * cfg.n_health_units
    loc_ids = [new_id() for _ in range(n_loc)]
    pops = np.array([r_ent.lognormal(math.log(cfg.pop_pharmacy[0] if k == "pharmacy" else cfg.pop_health_unit[0]),
                                     cfg.pop_pharmacy[1] if k == "pharmacy" else cfg.pop_health_unit[1]) for k in kinds])
    n_over = max(1, round(cfg.frac_over * n_loc))
    n_tight = max(1, round(cfg.frac_tight * n_loc))
    styles = ["over"] * n_over + ["tight"] * n_tight + ["normal"] * (n_loc - n_over - n_tight)
    r_ent.shuffle(styles)
    covers = np.array([max(-0.5, r_ent.normal(*cfg.cover_weeks[s])) for s in styles])
    review = np.array([cfg.review_weeks[k] for k in kinds])
    offset = r_ent.integers(0, review.max(), n_loc) % review               # escalona as requisições entre unidades
    horizon_w = review + cfg.mean_lead_weeks + covers                       # alvo order-up-to, em semanas de demanda
    prefixes = r_ent.choice([17500, 17501, 17502, 17505, 17510, 17511, 17520, 17560], n_loc)
    hoods = r_ent.permutation(_NEIGHBORHOODS)[:n_loc] if n_loc <= len(_NEIGHBORHOODS) else \
        r_ent.choice(_NEIGHBORHOODS, n_loc)
    rows_p, rows_h, loc_rows = [], [], []
    for i, (lid, kind) in enumerate(zip(loc_ids, kinds)):
        street, number = str(r_ent.choice(_STREETS)), str(int(r_ent.integers(10, 2000)))
        zipc = f"{prefixes[i]}-{int(r_ent.integers(0, 1000)):03d}"
        name = (f"Farmácia Distrital {i + 1} (mock)" if kind == "pharmacy"
                else f"UBS {hoods[i]} (mock)")
        common = dict(id=lid, name=name, street=street, number=number, neighborhood=str(hoods[i]),
                      zip_code=zipc, zone="Urbana")
        if kind == "pharmacy":
            rows_p.append({**common, "cnpj": fake_cnpj(r_ent), "phone_number": f"(14) 3{int(r_ent.integers(200, 999))}-{int(r_ent.integers(1000, 9999))}"})
        else:
            rows_h.append({**common, "cnes": fake_cnes(r_ent)})
        loc_rows.append(dict(location_id=lid, name=name, kind=kind, zip_code=zipc, neighborhood=str(hoods[i]), zone="Urbana"))

    specs = CATALOG[: cfg.n_medicines]
    med_ids = [new_id() for _ in specs]
    med_df = pd.DataFrame([dict(id=mid, name=s.name, active_ingredient=s.active_ingredient, dosage=s.dosage,
                                unit=s.unit, therapeutic_class=s.cls) for mid, s in zip(med_ids, specs)])

    # ---------------------------------------------------------------- CAMADA 1: demanda verdadeira
    wave = _regional_wave(r_dem, W, cfg)
    loading = np.clip(r_dem.normal(1.0, cfg.unit_load_sigma, n_loc), 0.5, None)
    uc = {(u, c): r_dem.normal(0, cfg.uc_sigma) for u in range(n_loc) for c in CLASS_PARAMS}
    t_years = np.arange(W) / 52.18
    series = []     # (u, m)
    lam_rows, dem_rows = [], []
    for u in range(n_loc):
        for m, sp in enumerate(specs):
            if r_dem.random() > cfg.carries_prob:
                continue                                      # a unidade não dispensa este medicamento
            cp = CLASS_PARAMS[sp.cls]
            a_um = r_dem.normal(0, cfg.um_sigma)
            eta = _ar1(r_dem, W, cfg.ar_rho, cfg.ar_sigma)
            loglam = (math.log(pops[u] / 1000.0 * sp.rate) + a_um + uc[(u, sp.cls)]
                      + cp["amp"] * np.cos(2 * np.pi * (woy - cp["peak"]) / 52.18)
                      + cp["trend"] * t_years + cp["beta"] * loading[u] * wave + eta
                      + cp["gamma"] * np.log(np.maximum(biz_days, 0.5) / 5.0))
            lam = np.exp(loglam)
            k = cp["k"]
            dem = r_dem.negative_binomial(k, k / (k + lam))
            series.append((u, m))
            lam_rows.append(lam)
            dem_rows.append(dem)
    S = len(series)
    LAM, DEM = np.vstack(lam_rows), np.vstack(dem_rows).astype(int)

    # divisão semanal -> diária (pesos de funcionamento; feriados fecham)
    open_w = np.where(is_hol[None, :], 0.0, 1.0)
    daily = np.zeros((S, D), dtype=int)
    for si, (u, m) in enumerate(series):
        sat = 0.3 if kinds[u] == "pharmacy" else 0.0
        wd = np.array([1, 1, 1, 1, 1, sat, 0.0])
        for w in range(W):
            p = wd[weekday[w * 7:(w + 1) * 7]] * open_w[0, w * 7:(w + 1) * 7]
            if p.sum() == 0:
                p = np.array([1, 1, 1, 1, 1, 0, 0], dtype=float)         # semana inteira fechada: reabre depois
            daily[si, w * 7:(w + 1) * 7] = r_dem.multinomial(DEM[si, w], p / p.sum())

    # ---------------------------------------------------------------- CAMADA 2: livro-razão
    movs: list[tuple] = []
    batches: dict[str, dict] = {}
    stocks: dict[str, dict] = {}
    disp = np.zeros((S, D), dtype=int)
    unmet = np.zeros((S, D), dtype=int)
    state = []        # por série: lotes vivos [batch_id, stock_id, qty, exp_day] e pedidos pendentes

    def ts(day: int, h0: int, h1: int):
        return days[day] + pd.Timedelta(hours=int(r_led.integers(h0, h1)), minutes=int(r_led.integers(0, 60)))

    def residual_days(sp) -> int:
        lo, hi = CLASS_PARAMS[sp.cls]["shelf"]
        if r_led.random() < cfg.short_dated_prob:
            return int(r_led.integers(25, 100))
        return int(r_led.uniform(0.55, 0.95) * r_led.uniform(lo, hi) * 30.4)

    def receive(si: int, day: int, qty: int, exp_day: int, mov_type: str, batch_id: str | None = None):
        u, m = series[si]
        sp = specs[m]
        if batch_id is None:
            batch_id = new_id()
            fab = days[day] - pd.Timedelta(days=int(r_led.integers(30, 180)))
            batches[batch_id] = dict(batch_id=batch_id, medicine_id=med_ids[m],
                                     batch_number=f"L{fab:%y%m}-{int(r_led.integers(1000, 9999))}",
                                     fabrication_date=fab.normalize(), expiration_date=days[0] + pd.Timedelta(days=exp_day),
                                     removed_at=pd.NaT)
        sid = new_id()
        stocks[sid] = dict(stock_id=sid, pharmacy_id=loc_ids[u] if kinds[u] == "pharmacy" else None,
                           health_unit_id=loc_ids[u] if kinds[u] == "health_unit" else None, batch_id=batch_id,
                           created_at=ts(day, 6, 8), updated_at=None, removed_at=pd.NaT, qty=qty)
        stocks[sid]["updated_at"] = stocks[sid]["created_at"]
        movs.append((new_id(), stocks[sid]["created_at"], sid, batch_id, med_ids[m], mov_type, qty))
        st = state[si]
        st["batches"].append([batch_id, sid, qty, exp_day])
        st["batches"].sort(key=lambda b: b[3])
        return sid

    def close_stock(sid: str, when):
        stocks[sid]["qty"] = 0
        stocks[sid]["removed_at"] = when
        stocks[sid]["updated_at"] = when

    for si, (u, m) in enumerate(series):
        sp = specs[m]
        state.append(dict(batches=[], pending=[]))
        init = max(0, int(horizon_w[u] * LAM[si, 0] * r_led.uniform(0.4, 1.0)))
        if init > 0:
            for part in ([init] if init < 2 * sp.pack else [init // 2, init - init // 2]):
                receive(si, 0, part, residual_days(sp), "Entry")   # saldo inicial lançado como entrada

    ev_day = D - 21
    ev_series = set(r_led.choice(S, size=min(cfg.demo_events, S), replace=False).tolist()) if cfg.demo_events > 0 else set()
    demo_batches: list[dict] = []

    for d in range(D):
        monday_ = d % 7 == 0
        if d == ev_day:                                           # eventos de demonstração (ver MockConfig.demo_events)
            for si in sorted(ev_series):
                u, m = series[si]
                avg = disp[si, max(0, d - 56):d].sum() / 8
                qty = int(math.ceil(avg * r_led.uniform(6, 12) / specs[m].pack)) * specs[m].pack
                if qty > 0:
                    exp_day = d + int(r_led.integers(35, 75))
                    sid = receive(si, d, qty, exp_day, "Entry")
                    demo_batches.append(dict(batch_id=stocks[sid]["batch_id"], location_id=loc_ids[u],
                                             medicine=specs[m].name, quantity=qty, expiry_in_days_from_event=exp_day - d))
        for si, (u, m) in enumerate(series):
            st, sp = state[si], specs[m]
            # 1) chegadas
            while st["pending"] and st["pending"][0][0] <= d:
                _, qty, res = st["pending"].pop(0)
                receive(si, d, qty, d + res, "Entry")
            # 2) vencimentos -> Loss
            for b in [b for b in st["batches"] if b[3] <= d and b[2] > 0]:
                t = days[d] + pd.Timedelta(hours=6, minutes=int(r_led.integers(0, 60)))
                movs.append((new_id(), t, b[1], b[0], med_ids[m], "Loss", b[2]))
                close_stock(b[1], t)
                b[2] = 0
            st["batches"] = [b for b in st["batches"] if b[2] > 0]
            # 3) dispensação FEFO (censurada pelo estoque)
            need = int(daily[si, d])
            if need > 0:
                for b in st["batches"]:
                    if need == 0:
                        break
                    take = min(b[2], need)
                    t = ts(d, 8, 17)
                    movs.append((new_id(), t, b[1], b[0], med_ids[m], "Exit", take))
                    b[2] -= take
                    need -= take
                    stocks[b[1]]["qty"] = b[2]
                    stocks[b[1]]["updated_at"] = t
                    if b[2] == 0:
                        stocks[b[1]]["removed_at"] = t
                st["batches"] = [b for b in st["batches"] if b[2] > 0]
                disp[si, d] = int(daily[si, d]) - need
                unmet[si, d] = need
            # 4) revisão semanal (segunda): pedido order-up-to com ruído
            if monday_ and d > 0 and (d // 7) % review[u] == offset[u]:
                hist = disp[si, max(0, d - 56):d].sum() / min(8, d / 7)
                on_hand = sum(b[2] for b in st["batches"])
                on_order = sum(p[1] for p in st["pending"])
                need_q = horizon_w[u] * hist - (on_hand + on_order)
                if need_q >= max(1.0, 0.5 * hist):
                    qty = int(math.ceil(need_q * r_led.lognormal(0, cfg.order_noise_sigma) / sp.pack)) * sp.pack
                    lead = int(r_led.choice([1, 2, 3], p=[0.3, 0.5, 0.2])) * 7 + (14 if r_led.random() < 0.1 else 0)
                    if d + lead < D:
                        st["pending"].append((d + lead, qty, residual_days(sp)))
                        st["pending"].sort()
        # 5) transferências manuais históricas (após as revisões da segunda-feira)
        if monday_ and d >= 14:
            for m in range(len(specs)):
                idx = [si for si, (_, mm) in enumerate(series) if mm == m]
                avg = {si: disp[si, d - 56 if d >= 56 else 0:d].sum() / min(8, d / 7) for si in idx}
                recv = [si for si in idx if unmet[si, d - 7:d].sum() > 0
                        and sum(b[2] for b in state[si]["batches"]) < avg[si]]
                r_led.shuffle(recv)
                for rsi in recv:
                    don = [si for si in idx if si != rsi and avg[si] > 0
                           and sum(b[2] for b in state[si]["batches"]) > 5 * avg[si]]
                    if not don or r_led.random() > cfg.transfer_prob:
                        continue
                    dsi = max(don, key=lambda s_: sum(b[2] for b in state[s_]["batches"]) - 3 * avg[s_])
                    surplus = int(sum(b[2] for b in state[dsi]["batches"]) - 3 * avg[dsi])
                    qty = min(surplus, int(2 * avg[rsi]))
                    cands = [b for b in state[dsi]["batches"] if b[3] - d >= 45 and b[2] >= max(3, qty)] or []
                    if qty < 3 or not cands:
                        continue
                    b = cands[0]
                    t = ts(d, 9, 16)
                    movs.append((new_id(), t, b[1], b[0], med_ids[m], "TransferOut", qty))
                    b[2] -= qty
                    stocks[b[1]]["qty"] = b[2]
                    stocks[b[1]]["updated_at"] = t
                    if b[2] == 0:
                        stocks[b[1]]["removed_at"] = t
                        state[dsi]["batches"] = [x for x in state[dsi]["batches"] if x[2] > 0]
                    ex = next((x for x in state[rsi]["batches"] if x[0] == b[0]), None)
                    if ex is not None:                      # já há linha (local, lote): soma nela (sem duplicar)
                        movs.append((new_id(), t, ex[1], b[0], med_ids[m], "TransferIn", qty))
                        ex[2] += qty
                        stocks[ex[1]]["qty"] = ex[2]
                        stocks[ex[1]]["updated_at"] = t
                    else:
                        receive(rsi, d, qty, b[3], "TransferIn", batch_id=b[0])

    # ---------------------------------------------------------------- empacota
    mov_df = pd.DataFrame(movs, columns=["movement_id", "created_at", "stock_id", "batch_id", "medicine_id", "type", "quantity"])
    mov_df = mov_df.sort_values("created_at", kind="stable").reset_index(drop=True)
    stk_df = pd.DataFrame(stocks.values()).rename(columns={"qty": "quantity"})
    bat_df = pd.DataFrame(batches.values())
    raw = RawData(movements=mov_df,
                  stocks=stk_df[["stock_id", "pharmacy_id", "health_unit_id", "batch_id", "quantity", "removed_at"]],
                  batches=bat_df, medicines=med_df.rename(columns={"id": "medicine_id"})[["medicine_id", "name"]],
                  locations=pd.DataFrame(loc_rows))
    wk = lambda a: a.reshape(S, W, 7).sum(axis=2)  # noqa: E731
    wd, wu = wk(disp), wk(unmet)
    truth = pd.DataFrame({
        "location_id": np.repeat([loc_ids[u] for u, _ in series], W),
        "medicine_id": np.repeat([med_ids[m] for _, m in series], W),
        "therapeutic_class": np.repeat([specs[m].cls for _, m in series], W),
        "week": np.tile(week_starts.to_numpy(), S),
        "true_mean": LAM.ravel(), "true_demand": DEM.ravel(), "observed": wd.ravel(), "unmet": wu.ravel(),
        "nb_k": np.repeat([CLASS_PARAMS[specs[m].cls]["k"] for _, m in series], W)})
    ledger = dict(wave=wave.round(4).tolist(), safety_weeks={loc_ids[i]: round(float(covers[i]), 2) for i in range(n_loc)},
                  review_weeks={loc_ids[i]: int(review[i]) for i in range(n_loc)},
                  styles={loc_ids[i]: styles[i] for i in range(n_loc)},
                  population={loc_ids[i]: int(pops[i]) for i in range(n_loc)})
    from dataclasses import asdict
    return MockDataset(raw, truth, pd.DataFrame(rows_p), pd.DataFrame(rows_h), med_df, asdict(cfg),
                       meta=dict(n_series=S, weeks=W, as_of=str(as_of.date()), start=str(start.date()), demo_events=demo_batches, **ledger))
