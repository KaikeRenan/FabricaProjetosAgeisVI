# Fábrica de Projetos Ágeis VI — Estoque de medicamentos com remanejamento guiado por previsão

Projeto da disciplina _Fábrica de Projetos Ágeis VI_. API de controle de estoque de medicamentos (farmácias e unidades
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

- **API .NET** — dona do banco e das regras de negócio. Guarda as execuções e sugestões, executa o remanejamento aceito
  de forma transacional e registra a decisão humana (que vira rótulo para avaliar o modelo).
- **Serviço de ML** — _stateless_ e somente-leitura: lê o histórico, prevê demanda (12 semanas) e calcula as sugestões.
  Opera em dois _tiers_: `baseline` (estatístico; ponto de partida e _fallback_ seguro) e `lgbm` (só quando há histórico
  suficiente **e** vence os baselines no backtest).
- **Job de treino** — roda fora do caminho de requisição (agendado), faz backtest com origem móvel, aplica o gate de
  promoção e registra uma versão imutável do modelo.

Detalhes e justificativas das decisões: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Estrutura do repositório

| Caminho                | Conteúdo                                                                               |
| ---------------------- | -------------------------------------------------------------------------------------- |
| `mvp/`                 | API ASP.NET Core 9 (controllers → services → repositories → EF Core/PostgreSQL)        |
| `mvp/Clients/`         | Cliente tipado do serviço de ML + contratos JSON (v1)                                  |
| `mvp/Middleware/`      | Tradução de exceções de domínio em HTTP (ProblemDetails)                               |
| `ml/`                  | Pacote Python `redistml`: dados → features → modelo → otimização → API HTTP → CLI      |
| `tests/ContractCheck/` | Verificação de contrato Python↔C# e de regras do serviço (sem banco, sem NuGet)        |
| `docs/`                | Arquitetura, _model card_, operação, mudanças, contrato e SQL de papel somente-leitura |
| `docker-compose.yml`   | Stack local: PostgreSQL + migrações + API + front-end Nuxt (ML opcional)               |

## Como rodar

Dois caminhos: **Docker** (recomendado — é o que vamos usar na apresentação) e **manual** (para desenvolver sem containers).

### Resumo

```powershell
# 0. Docker Desktop aberto
Copy-Item .env.example .env      # Linux/macOS: cp .env.example .env

make up-full                     # banco + seed + API + ML + front-end
make train                       # gera o primeiro modelo (uma vez por volume)

# Front-end: http://localhost:3000
# Swagger:   http://localhost:5080/swagger
```

### 1. Pré-requisitos

- **Docker Desktop** com WSL2, no mínimo ~8 GB de disco livre.
- **Internet no primeiro build**: o front-end Nuxt é clonado do GitHub durante o build e as imagens Python/.NET são baixadas.
- `make` é opcional (já vem no Git Bash/WSL). Sem ele, use os comandos `docker compose` indicados ao lado de cada alvo.

### 2. Configurar o `.env` (uma vez)

```powershell
Copy-Item .env.example .env      # Windows
```

```bash
cp .env.example .env             # Linux/macOS/Git Bash
```

O `.env.example` já funciona como está (valores de demonstração). Para trocar os segredos:

| Variável             | Regra                                                                                  |
| -------------------- | -------------------------------------------------------------------------------------- |
| `POSTGRES_PASSWORD`  | senha do banco                                                                         |
| `ML_READER_PASSWORD` | senha do papel somente-leitura do ML; **sem `@ : / %`** porque entra na URL de conexão |
| `ML_API_KEY`         | mínimo 24 caracteres (`openssl rand -hex 32`)                                          |
| `JWT_PRIVATE_KEY`    | mínimo 40 caracteres (`openssl rand -hex 32`)                                          |

> ⚠️ Se você mudar `POSTGRES_PASSWORD` **depois** que o volume já existe, rode `make down-v` antes: o PostgreSQL guarda a senha na inicialização do volume e não a relê.

As demais variáveis (`SEED_DEMO_DATA`, `BOOTSTRAP_USERS_ENABLED`, `ADMIN_*`, `MANAGER_*`, `OPERATOR_*`, `FRONTEND_PORT`) estão comentadas no próprio `.env.example`.

### 3. Subir a stack completa

```bash
make up-full
# equivale a: docker compose --profile frontend --profile ml up --build
```

O Compose executa nesta ordem:

1. **`db`** sobe e fica `healthy`;
2. **`migrate`** aplica as migrations do EF Core e encerra;
3. **`seed`** cria/atualiza o papel `ml_reader` e, **somente se o banco estiver vazio**, carrega os dados fictícios
   (30 medicamentos, 4 farmácias, 6 UBS, ~183 mil movimentações). Se já houver dados, imprime
   `seed: banco ja tem 30 medicamentos - nada a fazer` e não toca em nada;
4. **`api`** sobe na porta `5080` e cria os usuários de bootstrap que faltarem;
5. **`ml`** sobe interno na porta `8000` (ainda sem modelo);
6. **`frontend`** sobe na porta `3000`.

Primeiro build: **3–5 min**. Subidas seguintes: segundos.

Conferir o estado:

```powershell
docker compose --profile frontend --profile ml ps
```

### 4. Treinar o modelo (uma vez por volume)

```bash
make train
# equivale a: docker compose --profile jobs run --rm ml-train
```

Treina contra o banco populado, roda o backtest com origem móvel, aplica o gate de promoção e grava uma versão em
`/models`. O serviço `ml` detecta o novo `current.json` **sem reiniciar**.

```powershell
docker compose exec ml python -c "import urllib.request;print(urllib.request.urlopen('http://127.0.0.1:8000/health').read().decode())"
# {"status":"ok","model_loaded":true}
```

### 5. Acessar

| O quê         | URL                                           |
| ------------- | --------------------------------------------- |
| Front-end     | http://localhost:3000                         |
| API / Swagger | http://localhost:5080/swagger                 |
| PostgreSQL    | `localhost:5432` (banco `mvp`, usuário `mvp`) |

Usuários criados automaticamente no primeiro start (valores do `.env.example`):

| Papel    | E-mail               | Senha        |
| -------- | -------------------- | ------------ |
| Admin    | `admin@teste.com`    | `1234567890` |
| Manager  | `manager@teste.com`  | `1234567890` |
| Operator | `operator@teste.com` | `1234567890` |

No Swagger: `POST /api/auth/login` → copie o `token` → **Authorize** → `Bearer <token>`.

### 6. Parar e limpar

```bash
make down       # para a stack, mantém banco e modelo
make down-v     # para e apaga os volumes (o próximo up re-seeda e exige novo make train)
```

Sem `make`:

```bash
docker compose --profile frontend --profile ml down        # mantém dados
docker compose --profile frontend --profile ml down -v     # apaga dados
```

### 7. Problemas comuns

| Sintoma                                         | Causa / solução                                                         |
| ----------------------------------------------- | ----------------------------------------------------------------------- |
| `make: command not found`                       | use os equivalentes `docker compose ...`                                |
| `port 3000 is already allocated`                | defina `FRONTEND_PORT=3001` no `.env` (o CORS acompanha)                |
| `password authentication failed for user "mvp"` | `.env` mudou depois do volume existir → `make down-v`                   |
| `service "seed" didn't complete successfully`   | `docker compose logs seed`; volume antigo/inconsistente → `make down-v` |
| Front-end não abre                              | o primeiro build precisa de internet para clonar o repositório          |
| `model_loaded: false`                           | rode `make train`                                                       |
| `ml` reiniciando                                | confira `ML_READER_PASSWORD` no `.env` (o papel é criado pelo `seed`)   |

### 8. Alternativa: só banco + API + ML (sem front-end)

```bash
make up-ml      # docker compose --profile ml up --build
make up         # docker compose up --build (banco + API, sem ML)
```

### 9. Configuração manual (sem Docker)

**Backend** (requer .NET 9 e PostgreSQL):

```bash
cd mvp
cp appsettings.Example.json appsettings.json        # ajuste ConnectionStrings, Jwt e MlService
dotnet ef database update
dotnet run --launch-profile http                    # http://localhost:5080
```

**Serviço de ML** (requer Python 3.10+):

```bash
cd ml
pip install -e ".[api,sql,dev]"
pytest                                              # 41 testes (38 sem banco + 3 com PG_TEST_URL)
redistml serve-demo                                 # API com dados sintéticos, sem banco (porta 8765)
export ML_API_KEY=$(openssl rand -hex 32) ML_DATABASE_URL='postgresql+psycopg://ml_reader:***@host:5432/db'
redistml train --db-url "$ML_DATABASE_URL" --models-dir artifacts/models
redistml serve
```

No modo manual o papel `ml_reader` não é criado automaticamente: use [`docs/sql/ml_reader.sql`](docs/sql/ml_reader.sql)
depois das migrations (no Docker isso é feito pelo serviço `seed`).

**Dados mock fora do Docker** (o seed via Docker já gera e aplica automaticamente):

```bash
cd ml
redistml mock generate --out data/mock       # CSVs do ML + backend/seed.sql
cd data/mock/backend && psql -d "$SEU_BANCO" -f seed.sql
```

**Verificação de contrato** (com `redistml serve-demo` rodando): `dotnet run --project tests/ContractCheck`

## API de remanejamento (exige JWT)

| Método e rota                                               | O que faz                                                                              |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `POST /api/redistribution/runs`                             | Gera sugestões chamando o ML; substitui as pendentes anteriores                        |
| `GET  /api/redistribution/runs/{id}`                        | Execução (modelo, _tier_, avisos, totais) + suas sugestões                             |
| `GET  /api/redistribution/suggestions?status=&runId=&take=` | Lista sugestões (mais urgentes primeiro)                                               |
| `POST /api/redistribution/suggestions/{id}/accept`          | Aceita **e executa** (baixa na origem, entrada no destino, `TransferOut`/`TransferIn`) |
| `POST /api/redistribution/suggestions/{id}/reject`          | Recusa com motivo obrigatório                                                          |
| `GET  /api/redistribution/model`                            | Qual modelo/_tier_ está em uso                                                         |

Erros: `404` não encontrado · `409` estado inválido / estoque insuficiente / execução em andamento · `503` serviço de ML
indisponível. Exemplos prontos em [`mvp/mvp.http`](mvp/mvp.http).

CRUD existente (inalterado): `api/users`, `api/healthUnits`, `api/medicines`, `api/batches`, `api/stocks`,
`api/stockMovements`, `api/expirationAlerts`, `api/auth/login`.

## Documentação

- [Arquitetura e decisões](docs/ARCHITECTURE.md) · [Model card](docs/MODEL_CARD.md) · [Operação (runbook)](docs/OPERATIONS.md) · [Dados mock](docs/MOCK_DATA.md)
- [Registro das mudanças desta integração, estado de verificação e problemas pré-existentes](docs/CHANGES.md)
- Contrato do serviço de ML: [docs/contracts/ml-service.schemas.v1.json](docs/contracts/ml-service.schemas.v1.json)

## Leia antes de confiar nos números

Este repositório tem poucos dados reais. Com histórico curto o serviço opera no _tier_ `baseline` e avisa isso em todas
as respostas; o modelo de ML só é promovido quando comprova ganho sobre os baselines. Os números do _model card_ vêm de
**dados mock/sintéticos** e provam o funcionamento da esteira, não o desempenho no seu caso.
