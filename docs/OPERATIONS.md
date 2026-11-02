# Operação (runbook)

## 1. Variáveis de ambiente / configuração

| Onde | Chave | Descrição |
|---|---|---|
| API .NET | `ConnectionStrings:DefaultConnection` | PostgreSQL (usuário com escrita) |
| API .NET | `Jwt:PrivateKey`, `Jwt:ExpirationHours` | Assinatura/validade do token (≥ 40 caracteres na chave) |
| API .NET | `MlService:BaseUrl`, `MlService:ApiKey`, `MlService:TimeoutSeconds` | Opcional: sem isso só `/api/redistribution/*` responde 503 |
| ML | `ML_API_KEY` | Obrigatória, ≥ 24 caracteres (`openssl rand -hex 32`); a mesma do `MlService:ApiKey` |
| ML | `ML_DATABASE_URL` | `postgresql+psycopg://ml_reader:***@host:5432/db` (papel **somente-leitura**) |
| ML | `ML_MODELS_DIR` | Pasta do registro de modelos (volume persistente) |
| ML | `ML_TIMEZONE` | Padrão `America/Sao_Paulo` |

Segredos nunca no repositório (`.env` está no `.gitignore`; use `.env.example` como modelo).

## 2. Ordem de implantação (primeira vez)

1. Backup do banco. Aplicar migrações: `dotnet ef database update --project mvp` (cria `redistribution_runs` e `redistribution_suggestions`).
2. Criar o papel `ml_reader` **depois** das migrações: `psql -f docs/sql/ml_reader.sql` (troque a senha).
3. Gerar o primeiro modelo: `redistml train --db-url "$ML_DATABASE_URL" --models-dir "$ML_MODELS_DIR"`.
   Com pouco histórico ele registra o *tier* `baseline` — é o esperado.
4. Subir o serviço de ML (`redistml serve` ou container) e conferir `GET /health` (`model_loaded: true`).
5. Subir a API .NET e chamar `GET /api/redistribution/model` (com JWT).

## 2.1 Ambiente de demonstração com dados MOCK (MVP)

Use **somente em banco novo**: o seed aborta se já houver medicamentos ou movimentações.
```bash
cd ml && redistml mock generate --out data/mock                       # dados fictícios + validação
cd data/mock/backend && psql -d mvp_demo -f seed.sql                   # depois de dotnet ef database update
redistml train --db-url "$ML_DATABASE_URL" --models-dir "$ML_MODELS_DIR"
```
Detalhes, validação e limitações: [MOCK_DATA.md](MOCK_DATA.md). Nunca carregue o mock no banco de produção.

## 3. Rotina

| Quando | O quê |
|---|---|
| Segunda de manhã (após fechar a semana) | `redistml train …` (cron/Agendador) e depois `POST /api/redistribution/runs` |
| Diariamente | Pessoas aceitam/recusam sugestões pendentes |
| Mensalmente | Revisar métricas do item 4 e os motivos de recusa |

Exemplo de cron: `0 5 * * 1  redistml train --db-url "$ML_DATABASE_URL" --models-dir /models`.
O serviço detecta a nova versão sozinho (ponteiro `current.json`), sem reiniciar.

## 4. O que monitorar (SQL no banco do backend)

```sql
-- Qual tier/modelo está gerando as execuções e quantas falham
SELECT serving_tier, status, count(*) FROM redistribution_runs GROUP BY 1, 2;
-- Aceite/recusa/substituição nos últimos 30 dias (sinal de utilidade real)
SELECT status, count(*) FROM redistribution_suggestions WHERE created_at > now() - interval '30 days' GROUP BY 1;
-- Por que recusam
SELECT rejection_reason, count(*) FROM redistribution_suggestions WHERE status = 'Rejected' GROUP BY 1 ORDER BY 2 DESC;
-- Desperdício efetivo (requer lançar vencimentos como tipo Loss)
SELECT date_trunc('month', created_at) AS mes, sum(quantity) FROM stock_movements WHERE type = 'Loss' GROUP BY 1 ORDER BY 1;
```
Alertas sugeridos: execução `Failed` ou `degraded` por mais de 2 semanas; `serving_tier = baseline` quando já houver
histórico suficiente; aceite em queda; `warnings` com "dados defasados" (alguém parou de lançar saídas).

## 5. Rollback de modelo

`redistml promote <versao> --models-dir /models` (as versões ficam em `<models_dir>/versions/`; nunca são sobrescritas).
O serviço passa a usar a versão na próxima requisição.

## 6. Diagnóstico rápido

| Sintoma | Causa provável |
|---|---|
| `503` em `/api/redistribution/*` | `MlService` não configurado, ML fora do ar, chave diferente (o ML responde 401) |
| ML não sobe: "ML_API_KEY ausente ou curta" | Falha fechada intencional |
| `503 Nenhum modelo registrado` | Rodar o job de treino |
| `422 Dados inválidos` | Tipo de movimentação fora do mapa (ver `DEFAULT_MOVEMENT_MAP` em `ml/src/redistml/config.py`) |
| Muitos `no_forecast` | Séries sem saídas registradas (estoque cadastrado, mas nenhuma dispensação lançada) |
| `409 InsufficientStock` no aceite | Origem consumiu o estoque depois da sugestão; gerar nova execução |
| Sugestões estranhamente grandes | Conferir `Stock.Quantity` (é manual) e se há `Exit` que eram remanejamento/perda |

## 7. Escala e custos (quando crescer)

* O ML carrega **todas** as movimentações a cada execução. Quando passar de algumas centenas de milhares de linhas,
  agregue a demanda semanal no SQL (ou numa visão materializada) e leia só o agregado.
* Com mais de uma instância da API, troque o semáforo por *advisory lock* do PostgreSQL.
* Treino e inferência já são separados: escale/agende cada um independentemente.

## 8. Checklist de segurança

* `ml_reader` sem acesso a `users`; conexão com SSL; chave do ML rotacionada junto com a da API.
* Os controllers **anteriores** a esta integração continuam sem `[Authorize]` (ver CHANGES.md) — decida antes de expor a API.
* Swagger só em Desenvolvimento (já era assim).
