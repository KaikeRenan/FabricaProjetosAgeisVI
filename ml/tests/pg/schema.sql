-- DDL das 6 tabelas lidas pelo ML, DERIVADO MANUALMENTE de mvp/Migrations/ContextModelSnapshot.cs (não gerado pelo EF).
-- Serve apenas para testar seed.sql e as consultas do SqlSource em PostgreSQL real. Em CI com .NET, prefira
-- aplicar as migrações reais (`dotnet ef database update`) antes do seed.
CREATE TABLE medicines (
  id uuid PRIMARY KEY, name text NOT NULL, active_ingredient text NOT NULL, dosage text NOT NULL, unit text NOT NULL,
  created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL, removed_at timestamptz);
CREATE TABLE pharmacies (
  id uuid PRIMARY KEY, name text NOT NULL, cnpj text NOT NULL, phone_number text NOT NULL,
  street text NOT NULL, number text NOT NULL, neighborhood text NOT NULL, zip_code text NOT NULL, zone text NOT NULL,
  created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL, removed_at timestamptz);
CREATE TABLE health_units (
  id uuid PRIMARY KEY, name text NOT NULL, cnes text NOT NULL,
  street text NOT NULL, number text NOT NULL, neighborhood text NOT NULL, zip_code text NOT NULL, zone text NOT NULL,
  created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL, removed_at timestamptz);
CREATE TABLE batches (
  id uuid PRIMARY KEY, medicine_id uuid NOT NULL REFERENCES medicines(id), batch_number text NOT NULL,
  fabrication_date timestamptz NOT NULL, expiration_date timestamptz NOT NULL,
  created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL, removed_at timestamptz);
CREATE TABLE stocks (
  id uuid PRIMARY KEY, pharmacy_id uuid REFERENCES pharmacies(id) ON DELETE SET NULL,
  health_unit_id uuid REFERENCES health_units(id) ON DELETE SET NULL, batch_id uuid REFERENCES batches(id) ON DELETE SET NULL,
  quantity integer NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL, removed_at timestamptz);
CREATE TABLE stock_movements (
  id uuid PRIMARY KEY, stock_id uuid NOT NULL REFERENCES stocks(id), batch_id uuid NOT NULL REFERENCES batches(id),
  medicine_id uuid NOT NULL REFERENCES medicines(id), type text NOT NULL, quantity integer NOT NULL,
  created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL, removed_at timestamptz);
CREATE INDEX ix_stocks_batch ON stocks(batch_id);
CREATE INDEX ix_mov_stock ON stock_movements(stock_id);
