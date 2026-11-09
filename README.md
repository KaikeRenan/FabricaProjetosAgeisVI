# Fábrica de Projetos Ágeis VI — Estoque de medicamentos com remanejamento guiado por previsão

Projeto da disciplina *Fábrica de Projetos Ágeis VI*. API de controle de estoque de medicamentos (farmácias e unidades
de saúde) que, além do CRUD, **prevê a demanda de cada unidade e sugere quais lotes remanejar, de onde para onde, antes
que vençam** — reduzindo desperdício e ruptura. Uma pessoa sempre decide: o sistema recomenda, a pessoa aceita ou recusa.

## Visão geral da arquitetura

```mermaid
flowchart LR
  FE[Front-end / cliente HTTP] -- JWT --> API["API .NET 9 (mvp)"]
  API -- EF Core --> DB[(PostgreSQL)]
  API -- "HTTP + X-Api-Key<br/>POST /v1/recommendations" --> ML["Serviço de ML (FastAPI, Python)"]
  ML -- "SELECT (papel somente-leitura)" --> DB
  JOB["Job semanal<br/>redistml train"] -- SELECT --> DB
  JOB -- "versão imutável + ponteiro current" --> REG[(Registro de modelos)]
  ML -- carrega --> REG
```

* **API .NET** — dona do banco e das regras de negócio. Guarda as execuções e sugestões, executa o remanejamento aceito
  de forma transacional e registra a decisão humana (que vira rótulo para avaliar o modelo).
* **Serviço de ML** — *stateless* e somente-leitura: lê o histórico, prevê demanda (12 semanas) e calcula as sugestões.
  Opera em dois *tiers*: `baseline` (estatístico; ponto de partida e *fallback* seguro) e `lgbm` (só quando há histórico
  suficiente **e** vence os baselines no backtest).
* **Job de treino** — roda fora do caminho de requisição (agendado), faz backtest com origem móvel, aplica o gate de
  promoção e registra uma versão imutável do modelo.

Detalhes e justificativas das decisões: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Estrutura do repositório

| Caminho | Conteúdo |
|---|---|
| `mvp/` | API ASP.NET Core 9 (controllers → services → repositories → EF Core/PostgreSQL) |
| `mvp/Clients/` | Cliente tipado do serviço de ML + contratos JSON (v1) |
| `mvp/Middleware/` | Tradução de exceções de domínio em HTTP (ProblemDetails) |
| `ml/` | Pacote Python `redistml`: dados → features → modelo → otimização → API HTTP → CLI |
| `tests/ContractCheck/` | Verificação de contrato Python↔C# e de regras do serviço (sem banco, sem NuGet) |
| `docs/` | Arquitetura, *model card*, operação, mudanças, contrato e SQL de papel somente-leitura |
| `docker-compose.yml` | Stack local: PostgreSQL + migrações + API + front-end Nuxt (ML opcional) |

## Como rodar

### Stack completo com Docker

O `docker-compose.yml` sobe PostgreSQL, aplica as migrações, inicia a API .NET e
o front-end Nuxt. O front-end é construído a partir do repositório irmão
`../fabrica-projetos-vi`; use `FRONTEND_PATH` caso ele esteja em outro caminho.
Se a porta `3000` já estiver em uso, defina `FRONTEND_PORT` (por exemplo,
`FRONTEND_PORT=3001`); o CORS da API é configurado automaticamente para a porta
escolhida.

```bash
cp .env.example .env        # preencha as senhas/chaves
docker compose up --build
```

Depois acesse:

- Front-end: `http://localhost:3000`
- API/Swagger: `http://localhost:5080/swagger`
- PostgreSQL: `localhost:5432`

Pare a stack com `docker compose down` ou apague também os dados com
`docker compose down -v`.

O serviço de ML não sobe por padrão. Para incluí-lo, use os profiles e siga a
ordem de inicialização do ML descrita em [docs/OPERATIONS.md](docs/OPERATIONS.md):

```bash
docker compose --profile ml up -d
docker compose --profile jobs run --rm ml-train
```

**Backend** (requer .NET 9 e PostgreSQL)
```bash
cd mvp
cp appsettings.Example.json appsettings.json        # ajuste ConnectionStrings, Jwt e MlService
dotnet ef database update                           # aplica as migrações (inclui AddRedistribution)
dotnet run --launch-profile http                    # http://localhost:5080  (Swagger em /swagger)
```

**Serviço de ML** (requer Python 3.10+)
```bash
cd ml
pip install -e ".[api,sql,dev]"
pytest                                              # 37 testes (+3 que exigem PostgreSQL)
redistml serve-demo                                 # API com dados sintéticos, sem banco (porta 8765)
# produção: crie o papel somente-leitura (docs/sql/ml_reader.sql) e então
export ML_API_KEY=$(openssl rand -hex 32) ML_DATABASE_URL='postgresql+psycopg://ml_reader:***@host:5432/db'
redistml train --db-url "$ML_DATABASE_URL" --models-dir artifacts/models   # job (agende semanalmente)
redistml serve                                      # sobe a API de inferência
```

**Dados MOCK para o MVP** (fictícios, com processo gerador documentado e validado): veja [docs/MOCK_DATA.md](docs/MOCK_DATA.md)
```bash
redistml mock generate --out data/mock      # CSVs do ML + seed.sql do PostgreSQL do backend
redistml mock evaluate --data-dir data/mock # backtest + baselines + oráculo
```

**Verificação de contrato** (com `redistml serve-demo` rodando): `dotnet run --project tests/ContractCheck`

## API de remanejamento (exige JWT)

| Método e rota | O que faz |
|---|---|
| `POST /api/redistribution/runs` | Gera sugestões chamando o ML; substitui as pendentes anteriores |
| `GET  /api/redistribution/runs/{id}` | Execução (modelo, *tier*, avisos, totais) + suas sugestões |
| `GET  /api/redistribution/suggestions?status=&runId=&take=` | Lista sugestões (mais urgentes primeiro) |
| `POST /api/redistribution/suggestions/{id}/accept` | Aceita **e executa** (baixa na origem, entrada no destino, `TransferOut`/`TransferIn`) |
| `POST /api/redistribution/suggestions/{id}/reject` | Recusa com motivo obrigatório |
| `GET  /api/redistribution/model` | Qual modelo/*tier* está em uso |

Erros: `404` não encontrado · `409` estado inválido / estoque insuficiente / execução em andamento · `503` serviço de ML
indisponível. Exemplos prontos em [`mvp/mvp.http`](mvp/mvp.http).

CRUD existente (inalterado): `api/users`, `api/healthUnits`, `api/medicines`, `api/batches`, `api/stocks`,
`api/stockMovements`, `api/expirationAlerts`, `api/auth/login`.

## Documentação

* [Arquitetura e decisões](docs/ARCHITECTURE.md) · [Model card](docs/MODEL_CARD.md) · [Operação (runbook)](docs/OPERATIONS.md) · [Dados mock](docs/MOCK_DATA.md)
* [Registro das mudanças desta integração, estado de verificação e problemas pré-existentes](docs/CHANGES.md)
* Contrato do serviço de ML: [docs/contracts/ml-service.schemas.v1.json](docs/contracts/ml-service.schemas.v1.json)

## Leia antes de confiar nos números

Este repositório tem poucos dados reais. Com histórico curto o serviço opera no *tier* `baseline` e avisa isso em todas
as respostas; o modelo de ML só é promovido quando comprova ganho sobre os baselines. Os números do *model card* vêm de
**dados mock/sintéticos** e provam o funcionamento da esteira, não o desempenho no seu caso.
