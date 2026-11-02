-- Papel SOMENTE-LEITURA para o serviço de ML. Rode DEPOIS de aplicar as migrações do EF (as tabelas precisam existir).
-- Princípio do menor privilégio: o serviço de ML lê apenas o que o modelo usa e NÃO enxerga a tabela "users"
-- (que contém e-mails e hashes de senha). Troque a senha e, de preferência, exija SSL na conexão.
CREATE ROLE ml_reader LOGIN PASSWORD 'TROQUE-ESTA-SENHA';
GRANT CONNECT ON DATABASE "yourDatabase" TO ml_reader;
GRANT USAGE ON SCHEMA public TO ml_reader;
GRANT SELECT ON stock_movements, stocks, batches, medicines, pharmacies, health_units TO ml_reader;
-- Nada de INSERT/UPDATE/DELETE: o backend .NET é o único escritor do banco.
