"""Especificação do processo gerador de dados (DGP): catálogo, classes terapêuticas, calendário e configuração."""
from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date, timedelta

import numpy as np


@dataclass(frozen=True)
class MedicineSpec:
    name: str
    active_ingredient: str
    dosage: str
    unit: str
    cls: str
    rate: float          # consumo médio por 1.000 habitantes atendidos por semana (em unidades de dispensação)
    pack: int = 10       # múltiplo mínimo de pedido


# Parâmetros por classe terapêutica (escala LOG da demanda). Valores escolhidos a priori pela plausibilidade
# epidemiológica do Sudeste do Brasil (sazonalidade respiratória no inverno, alergia na primavera, arboviroses no
# outono) — NÃO foram ajustados para favorecer nenhum modelo.
CLASS_PARAMS: dict[str, dict] = {
    #                amp   pico(sem. do ano)  beta(onda regional)  k(dispersão NB)  tendência/ano  gamma(feriado)  validade (meses)
    "chronic":        dict(amp=0.03, peak=28, beta=0.00, k=40.0, trend=0.04, gamma=0.35, shelf=(24, 36)),
    "respiratory":    dict(amp=0.45, peak=28, beta=0.60, k=6.0,  trend=0.01, gamma=0.80, shelf=(18, 30)),
    "analgesic":      dict(amp=0.15, peak=14, beta=0.30, k=10.0, trend=0.01, gamma=0.80, shelf=(24, 36)),
    "allergy":        dict(amp=0.40, peak=38, beta=0.10, k=8.0,  trend=0.00, gamma=0.80, shelf=(24, 36)),
    "anti_infective": dict(amp=0.05, peak=20, beta=0.00, k=2.5,  trend=0.00, gamma=0.80, shelf=(24, 36)),
}

CATALOG: list[MedicineSpec] = [
    MedicineSpec("Losartana Potássica", "losartana potássica", "50 mg", "caixa", "chronic", 22, 20),
    MedicineSpec("Metformina", "cloridrato de metformina", "850 mg", "caixa", "chronic", 20, 20),
    MedicineSpec("Enalapril", "maleato de enalapril", "10 mg", "caixa", "chronic", 16, 20),
    MedicineSpec("Hidroclorotiazida", "hidroclorotiazida", "25 mg", "caixa", "chronic", 12, 20),
    MedicineSpec("Sinvastatina", "sinvastatina", "20 mg", "caixa", "chronic", 11, 20),
    MedicineSpec("Omeprazol", "omeprazol", "20 mg", "caixa", "chronic", 14, 20),
    MedicineSpec("Atenolol", "atenolol", "50 mg", "caixa", "chronic", 7, 10),
    MedicineSpec("Ácido Acetilsalicílico", "ácido acetilsalicílico", "100 mg", "caixa", "chronic", 6, 10),
    MedicineSpec("Levotiroxina", "levotiroxina sódica", "50 mcg", "caixa", "chronic", 5, 10),
    MedicineSpec("Captopril", "captopril", "25 mg", "caixa", "chronic", 5, 10),
    MedicineSpec("Amoxicilina", "amoxicilina", "500 mg", "caixa", "respiratory", 6, 10),
    MedicineSpec("Azitromicina", "azitromicina", "500 mg", "caixa", "respiratory", 2.5, 10),
    MedicineSpec("Prednisona", "prednisona", "20 mg", "caixa", "respiratory", 3, 10),
    MedicineSpec("Salbutamol", "sulfato de salbutamol", "100 mcg/dose", "frasco", "respiratory", 1.2, 5),
    MedicineSpec("Ambroxol", "cloridrato de ambroxol", "15 mg/5 mL", "frasco", "respiratory", 2.0, 5),
    MedicineSpec("Dipirona", "dipirona sódica", "500 mg", "caixa", "analgesic", 14, 20),
    MedicineSpec("Paracetamol", "paracetamol", "750 mg", "caixa", "analgesic", 10, 20),
    MedicineSpec("Ibuprofeno", "ibuprofeno", "600 mg", "caixa", "analgesic", 6, 10),
    MedicineSpec("Diclofenaco", "diclofenaco sódico", "50 mg", "caixa", "analgesic", 4, 10),
    MedicineSpec("Loratadina", "loratadina", "10 mg", "caixa", "allergy", 4, 10),
    MedicineSpec("Dexclorfeniramina", "maleato de dexclorfeniramina", "2 mg", "caixa", "allergy", 2.5, 10),
    MedicineSpec("Albendazol", "albendazol", "400 mg", "caixa", "anti_infective", 0.35, 5),
    MedicineSpec("Fluconazol", "fluconazol", "150 mg", "caixa", "anti_infective", 0.25, 5),
    MedicineSpec("Metronidazol", "metronidazol", "250 mg", "caixa", "anti_infective", 1.0, 5),
    # itens de BAIXO giro (demanda intermitente): relevantes porque vencem mais e testam o TSB/portão do modelo
    MedicineSpec("Cefalexina", "cefalexina", "500 mg", "caixa", "anti_infective", 0.15, 5),
    MedicineSpec("Aciclovir", "aciclovir", "200 mg", "caixa", "anti_infective", 0.10, 5),
    MedicineSpec("Ivermectina", "ivermectina", "6 mg", "caixa", "anti_infective", 0.12, 5),
    MedicineSpec("Nistatina", "nistatina", "100.000 UI/mL", "frasco", "anti_infective", 0.12, 5),
    MedicineSpec("Prometazina", "cloridrato de prometazina", "25 mg", "caixa", "allergy", 0.08, 5),
    MedicineSpec("Cetoconazol", "cetoconazol", "20 mg/g creme", "bisnaga", "anti_infective", 0.06, 5),
]


@dataclass(frozen=True)
class MockConfig:
    seed: int = 20261028
    as_of: str = "2026-10-26"            # segunda-feira; a última semana COMPLETA termina no domingo anterior
    weeks: int = 156                      # 3 anos: >= 82 semanas para o tier LightGBM + >= 2 ciclos sazonais
    n_pharmacies: int = 4
    n_health_units: int = 6
    n_medicines: int = len(CATALOG)
    # demanda
    pop_pharmacy: tuple = (30000.0, 0.30)   # (mediana, sigma log) de habitantes atendidos
    pop_health_unit: tuple = (9000.0, 0.40)
    um_sigma: float = 0.25                  # efeito aleatório (unidade x medicamento): prática prescritiva local
    uc_sigma: float = 0.10                  # efeito aleatório (unidade x classe)
    carries_prob: float = 0.92              # P(a unidade dispensa o medicamento)
    ar_rho: float = 0.60                    # persistência do ruído idiossincrático (log)
    ar_sigma: float = 0.08
    wave_rho: float = 0.80                  # AR(1) da onda regional compartilhada
    wave_sigma: float = 0.05
    outbreaks_per_year: float = 1.5         # surtos regionais (Poisson)
    outbreak_amp_median: float = 0.35
    outbreak_amp_sigma: float = 0.40
    unit_load_sigma: float = 0.15           # quão sensível cada unidade é à onda regional
    # política de reposição. METAS declaradas ANTES de calibrar: ruptura em 3-8% das semanas-série e perda por vencimento
    # de 0,3-3% das unidades recebidas (ordem de grandeza compatível com a literatura de gestão de estoque público de
    # medicamentos; ver docs/MOCK_DATA.md). Só estes parâmetros operacionais foram calibrados — nunca a demanda.
    # estoque de segurança (semanas de demanda ACIMA de revisão + lead time médio de 2 semanas)
    cover_weeks: dict = field(default_factory=lambda: {"normal": (2.0, 0.4), "over": (6.0, 1.2), "tight": (0.8, 0.3)})
    review_weeks: dict = field(default_factory=lambda: {"pharmacy": 2, "health_unit": 4})   # requisição quinzenal / mensal
    mean_lead_weeks: float = 2.0
    frac_over: float = 0.2
    frac_tight: float = 0.2
    order_noise_sigma: float = 0.15
    short_dated_prob: float = 0.15          # lotes que chegam com validade residual curta (~1 a 3,5 meses)
    transfer_prob: float = 0.30             # chance semanal de transferência manual histórica (receptor em ruptura)
    # EVENTOS DE DEMONSTRAÇÃO (padrão 0 = histórico puramente calibrado). Injeta, 3 semanas antes de as_of, entregas grandes
    # de lotes com validade curta (ex.: doação/compra emergencial) para que o snapshot atual contenha estoque em risco.
    # Só adiciona ENTRADAS: não altera a demanda nem as dispensações. Os lotes injetados ficam listados em dgp.json.
    demo_events: int = 0


# --------------------------------------------------------------------------------------------- calendário
def easter(year: int) -> date:
    """Algoritmo de Meeus/Jones/Butcher (calendário gregoriano)."""
    a, b, c = year % 19, year // 100, year % 100
    d, e = b // 4, b % 4
    f = (b + 8) // 25
    g = (b - f + 1) // 3
    h = (19 * a + b - d - g + 15) % 30
    i, k = c // 4, c % 4
    l = (32 + 2 * e + 2 * i - h - k) % 7
    m = (a + 11 * h + 22 * l) // 451
    month, day = divmod(h + l - 7 * m + 114, 31)
    return date(year, month, day + 1)


def br_holidays(years: range) -> set[date]:
    """Feriados nacionais (fixos + móveis ligados à Páscoa) em que o atendimento fica fechado."""
    out: set[date] = set()
    for y in years:
        e = easter(y)
        out |= {date(y, 1, 1), date(y, 4, 21), date(y, 5, 1), date(y, 9, 7), date(y, 10, 12), date(y, 11, 2),
                date(y, 11, 15), date(y, 11, 20), date(y, 12, 25),
                e - timedelta(days=48), e - timedelta(days=47),      # Carnaval (seg e ter)
                e - timedelta(days=2),                               # Sexta-feira Santa
                e + timedelta(days=60)}                              # Corpus Christi
    return out


# ------------------------------------------------------------------------------------------ identificadores
def _check_digits(base: list[int], weights: list[int]) -> int:
    s = sum(d * w for d, w in zip(base, weights))
    r = s % 11
    return 0 if r < 2 else 11 - r


def fake_cnpj(rng: np.random.Generator) -> str:
    """CNPJ FICTÍCIO com dígitos verificadores válidos (pode coincidir com um real por acaso; nunca use fiscalmente)."""
    base = [int(x) for x in rng.integers(0, 10, 8)] + [0, 0, 0, 1]
    d1 = _check_digits(base, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])
    d2 = _check_digits(base + [d1], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2])
    n = "".join(map(str, base + [d1, d2]))
    return f"{n[:2]}.{n[2:5]}.{n[5:8]}/{n[8:12]}-{n[12:]}"


def fake_cnes(rng: np.random.Generator) -> str:
    return "".join(map(str, rng.integers(0, 10, 7)))
