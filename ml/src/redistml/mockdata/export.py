"""Exportação: (a) CSVs no layout do ML, (b) CSVs + seed.sql no schema REAL do backend (PostgreSQL), (c) relatórios."""
from __future__ import annotations

import json
from pathlib import Path

import pandas as pd

from .simulate import MockDataset

LOCAL_TZ = "America/Sao_Paulo"
_TS = "%Y-%m-%d %H:%M:%S+00"


def _utc(s: pd.Series) -> pd.Series:
    """Instante local (ingênuo, America/Sao_Paulo) -> texto UTC para COPY em `timestamp with time zone`."""
    s = pd.to_datetime(s)
    return s.dt.tz_localize(LOCAL_TZ).dt.tz_convert("UTC").dt.strftime(_TS)


def _date_utc(s: pd.Series) -> pd.Series:
    return pd.to_datetime(s).dt.strftime("%Y-%m-%d 00:00:00+00")


def write_local_csv(ds: MockDataset, out: Path) -> list[str]:
    out.mkdir(parents=True, exist_ok=True)
    r = ds.raw
    r.movements.to_csv(out / "movements.csv", index=False)
    r.stocks.to_csv(out / "stocks.csv", index=False)
    r.batches.to_csv(out / "batches.csv", index=False)
    r.medicines.to_csv(out / "medicines.csv", index=False)
    r.locations.to_csv(out / "locations.csv", index=False)
    ds.truth.to_csv(out / "truth.csv", index=False)
    return ["movements", "stocks", "batches", "medicines", "locations", "truth"]


_SEED_HEADER = """-- Seed MOCK (dados FICTÍCIOS) para o MVP. Gerado por `redistml mock generate`.
-- Uso (dentro desta pasta, com as migrações do EF já aplicadas):  psql -d SEU_BANCO -f seed.sql
-- Segurança: aborta se o banco já tiver movimentações, para nunca misturar mock com dados reais.
BEGIN;
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM stock_movements) OR EXISTS (SELECT 1 FROM medicines) THEN
    RAISE EXCEPTION 'Banco já possui medicamentos/movimentações; seed abortado.';
  END IF;
END $$;
"""

_COLS = {
    "medicines": "id, name, active_ingredient, dosage, unit, created_at, updated_at, removed_at",
    "pharmacies": "id, name, cnpj, phone_number, street, number, neighborhood, zip_code, zone, created_at, updated_at, removed_at",
    "health_units": "id, name, cnes, street, number, neighborhood, zip_code, zone, created_at, updated_at, removed_at",
    "batches": "id, medicine_id, batch_number, fabrication_date, expiration_date, created_at, updated_at, removed_at",
    "stocks": "id, pharmacy_id, health_unit_id, batch_id, quantity, created_at, updated_at, removed_at",
    "stock_movements": "id, stock_id, batch_id, medicine_id, type, quantity, created_at, updated_at, removed_at",
}


def write_backend_seed(ds: MockDataset, out: Path) -> list[str]:
    """CSVs com os nomes de colunas do EF/PostgreSQL + seed.sql (\\copy em ordem de FK)."""
    out.mkdir(parents=True, exist_ok=True)
    r = ds.raw
    t0 = (pd.Timestamp(ds.meta["start"])).strftime(_TS.replace("%H:%M:%S", "00:00:00"))
    base = lambda n: dict(created_at=[t0] * n, updated_at=[t0] * n, removed_at=[None] * n)  # noqa: E731

    med = ds.medicines.drop(columns="therapeutic_class").assign(**base(len(ds.medicines)))
    ph = ds.pharmacies.assign(**base(len(ds.pharmacies)))
    hu = ds.health_units.assign(**base(len(ds.health_units)))

    stk = r.stocks.merge(_stock_times(ds), on="stock_id")
    first = stk.groupby("batch_id")["created_at"].min()
    bat = r.batches.rename(columns={"batch_id": "id"}).copy()
    bat["created_at"] = _utc(bat["id"].map(first))
    bat["updated_at"] = bat["created_at"]
    bat["removed_at"] = None
    bat["fabrication_date"] = _date_utc(bat["fabrication_date"])
    bat["expiration_date"] = _date_utc(bat["expiration_date"])

    st = stk.rename(columns={"stock_id": "id"}).copy()
    for c in ("created_at", "updated_at", "removed_at"):
        st[c] = _utc(st[c])
    mv = r.movements.rename(columns={"movement_id": "id"}).copy()
    mv["created_at"] = _utc(mv["created_at"])
    mv["updated_at"] = mv["created_at"]
    mv["removed_at"] = None

    frames = {"medicines": med, "pharmacies": ph, "health_units": hu, "batches": bat, "stocks": st, "stock_movements": mv}
    sql = [_SEED_HEADER]
    for name, df in frames.items():
        cols = [c.strip() for c in _COLS[name].split(",")]
        df[cols].to_csv(out / f"{name}.csv", index=False)
        sql.append(f"\\copy {name} ({_COLS[name]}) FROM '{name}.csv' CSV HEADER")
    sql.append("COMMIT;\n")
    (out / "seed.sql").write_text("\n".join(sql))
    return list(frames)


def _stock_times(ds: MockDataset) -> pd.DataFrame:
    # `stocks` do ML guarda só removed_at; created/updated vêm do 1º/último movimento de cada linha de estoque.
    m = ds.raw.movements
    g = m.groupby("stock_id")["created_at"].agg(created_at="min", updated_at="max").reset_index()
    return g


def write_reports(ds: MockDataset, diagnostics: dict, out: Path) -> None:
    out.mkdir(parents=True, exist_ok=True)
    (out / "dgp.json").write_text(json.dumps({"config": ds.config, "meta": {k: v for k, v in ds.meta.items() if k != "wave"},
                                              "wave_log_amplitude": ds.meta["wave"]}, indent=2, default=str))
    (out / "diagnostics.json").write_text(json.dumps(diagnostics, indent=2, default=str))
