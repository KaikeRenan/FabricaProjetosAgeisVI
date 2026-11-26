#!/bin/sh
set -eu

# papel somente-leitura do ML (idempotente; roda em todo up)
if [ -n "${ML_READER_PASSWORD:-}" ]; then
  if [ "$(psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='ml_reader'")" = "1" ]; then
    psql -v ON_ERROR_STOP=1 -v pw="$ML_READER_PASSWORD" <<'SQL'
ALTER ROLE ml_reader LOGIN PASSWORD :'pw';
SQL
  else
    psql -v ON_ERROR_STOP=1 -v pw="$ML_READER_PASSWORD" <<'SQL'
CREATE ROLE ml_reader LOGIN PASSWORD :'pw';
SQL
  fi

  psql -v ON_ERROR_STOP=1 -v db="$PGDATABASE" <<'SQL'
GRANT CONNECT ON DATABASE :"db" TO ml_reader;
GRANT USAGE ON SCHEMA public TO ml_reader;
GRANT SELECT ON stock_movements, stocks, batches, medicines, pharmacies, health_units TO ml_reader;
SQL
fi

if [ "${SEED_DEMO_DATA:-true}" != "true" ]; then
  echo "seed: desabilitado (SEED_DEMO_DATA=${SEED_DEMO_DATA})"
  exit 0
fi

n=$(psql -tAc "SELECT count(*) FROM medicines")
if [ "${n:-0}" -gt 0 ]; then
  echo "seed: banco ja tem ${n} medicamentos - nada a fazer"
  exit 0
fi

echo "seed: banco vazio - carregando dados ficticios..."
psql -v ON_ERROR_STOP=1 -f /seed/seed.sql
echo "seed: OK"