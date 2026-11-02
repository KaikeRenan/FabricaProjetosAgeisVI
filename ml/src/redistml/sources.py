"""Camada de dados. O modelo de ML NUNCA fala com o banco direto: recebe um RawData.

O schema abaixo é o gerado pelo EF Core do backend (mvp/Data/Context.cs): tabelas e colunas em snake_case,
`stock_movements.type` persistido como TEXTO ('Entry', 'Exit', ...), datas em `timestamp with time zone`.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import pandas as pd


@dataclass
class RawData:
    movements: pd.DataFrame   # movement_id, created_at, stock_id, batch_id, medicine_id, type, quantity
    stocks: pd.DataFrame      # stock_id, pharmacy_id, health_unit_id, batch_id, quantity, removed_at
    batches: pd.DataFrame     # batch_id, medicine_id, batch_number, fabrication_date, expiration_date[, removed_at]
    medicines: pd.DataFrame | None = None   # medicine_id, name
    locations: pd.DataFrame | None = None   # location_id, name, kind, zip_code, neighborhood, zone [, latitude, longitude]


class CsvSource:
    """Lê movements.csv, stocks.csv, batches.csv [, medicines.csv, locations.csv] (útil para demo/testes/offline)."""

    def __init__(self, directory: str | Path):
        self.dir = Path(directory)

    def _read(self, name: str, dates: list[str], required: bool = True):
        p = self.dir / f"{name}.csv"
        if not p.exists():
            if required:
                raise FileNotFoundError(p)
            return None
        df = pd.read_csv(p)
        for c in dates:
            if c in df:
                df[c] = pd.to_datetime(df[c])
        return df

    def load(self) -> RawData:
        return RawData(
            movements=self._read("movements", ["created_at"]),
            stocks=self._read("stocks", ["removed_at"]),
            batches=self._read("batches", ["fabrication_date", "expiration_date", "removed_at"]),
            medicines=self._read("medicines", [], required=False),
            locations=self._read("locations", [], required=False),
        )


# ----------------------------------------------------------------------------------------------------------
# SQL (PostgreSQL do backend). Somente SELECT: o serviço de ML deve usar um papel de BANCO SOMENTE-LEITURA.
# ----------------------------------------------------------------------------------------------------------
SQL_MOVEMENTS = """SELECT id AS movement_id, created_at, stock_id, batch_id, medicine_id, type, quantity
FROM stock_movements WHERE removed_at IS NULL"""
# Stocks INCLUI soft-deleted: movimentos históricos ainda apontam para stocks removidos (precisamos do local).
SQL_STOCKS = """SELECT id AS stock_id, pharmacy_id, health_unit_id, batch_id, quantity, removed_at FROM stocks"""
SQL_BATCHES = """SELECT id AS batch_id, medicine_id, batch_number, fabrication_date, expiration_date, removed_at
FROM batches"""
SQL_MEDICINES = """SELECT id AS medicine_id, name FROM medicines WHERE removed_at IS NULL"""
SQL_LOCATIONS = """SELECT id AS location_id, name, 'pharmacy' AS kind, zip_code, neighborhood, zone
FROM pharmacies WHERE removed_at IS NULL
UNION ALL
SELECT id AS location_id, name, 'health_unit' AS kind, zip_code, neighborhood, zone
FROM health_units WHERE removed_at IS NULL"""

_ID_COLS = {
    "movements": ["movement_id", "stock_id", "batch_id", "medicine_id"],
    "stocks": ["stock_id", "pharmacy_id", "health_unit_id", "batch_id"],
    "batches": ["batch_id", "medicine_id"],
    "medicines": ["medicine_id"],
    "locations": ["location_id"],
}


def _ids(df: pd.DataFrame, cols: list[str]) -> pd.DataFrame:
    """UUID (psycopg devolve uuid.UUID; SQLite devolve str) -> str minúscula; NULL permanece nulo."""
    for c in cols:
        if c in df:
            df[c] = df[c].map(lambda v: None if pd.isna(v) else str(v).lower())
    return df


def _naive(s: pd.Series, tz: str | None) -> pd.Series:
    """timestamptz -> datetime ingênuo. `tz` converte para o fuso LOCAL antes (agrupamento semanal correto)."""
    s = pd.to_datetime(s, utc=True)
    if tz:
        s = s.dt.tz_convert(tz)
    return s.dt.tz_localize(None)


class SqlSource:
    """Carrega do PostgreSQL do backend (ou qualquer engine SQLAlchemy — os testes usam SQLite)."""

    def __init__(self, url: str | None = None, engine=None, timezone: str = "America/Sao_Paulo"):
        if engine is None:
            from sqlalchemy import create_engine  # import tardio: dependência opcional

            engine = create_engine(url, pool_pre_ping=True)
        self.engine = engine
        self.timezone = timezone

    def _q(self, sql: str) -> pd.DataFrame:
        from sqlalchemy import text

        with self.engine.connect() as conn:
            return pd.read_sql(text(sql), conn)

    def load(self) -> RawData:
        mov, stk, bat = self._q(SQL_MOVEMENTS), self._q(SQL_STOCKS), self._q(SQL_BATCHES)
        med, loc = self._q(SQL_MEDICINES), self._q(SQL_LOCATIONS)
        # created_at: fuso local (dispensação de domingo 22h não pode cair na semana seguinte);
        # datas de validade/fabricação: o backend grava a DATA como 00:00Z, então mantemos UTC para não "voltar um dia".
        mov["created_at"] = _naive(mov["created_at"], self.timezone)
        stk["removed_at"] = _naive(stk["removed_at"], None)
        bat["removed_at"] = _naive(bat["removed_at"], None)
        for c in ("fabrication_date", "expiration_date"):
            bat[c] = _naive(bat[c], None)
        return RawData(_ids(mov, _ID_COLS["movements"]), _ids(stk, _ID_COLS["stocks"]),
                       _ids(bat, _ID_COLS["batches"]), _ids(med, _ID_COLS["medicines"]),
                       _ids(loc, _ID_COLS["locations"]))
