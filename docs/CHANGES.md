# Registro de mudanças da integração (ML ↔ backend)

Base: o repositório enviado (`FabricaProjetosAgeisVI-main`). Todas as mudanças estão no patch `integracao-ml.patch`
(`git apply`) e no repositório completo entregue.

## 1. Resumo
* **Novo:** serviço de ML (`ml/`), integração no backend (`/api/redistribution/*`), duas tabelas, verificação de contrato,
  infraestrutura (Docker, CI) e documentação.
* **Alterado:** enum `StockMovementType`, `Context`, `Program.cs`, configuração de exemplo, `.gitignore`, `mvp.http`, `README.md`.
* **Corrigido por ser pré-requisito** (bloqueava o login, logo os endpoints protegidos): `AuthController`, `JwtSettings`, `TokenService`.

## 2. Arquivos novos
* `(raiz)/` — `.env.example`, `docker-compose.yml`
* `.github/workflows/` — `ci.yml`
* `docs/` — `ARCHITECTURE.md`, `MOCK_DATA.md`, `MODEL_CARD.md`, `OPERATIONS.md`
* `docs/contracts/` — `ml-service.schemas.v1.json`
* `docs/sql/` — `ml_reader.sql`
* `ml/` — `Dockerfile`, `README.md`, `pyproject.toml`
* `ml/src/redistml/` — `__init__.py`, `api.py`, `baselines.py`, `cli.py`, `config.py`, `evaluation.py`, `features.py`, `model.py`, `optimizer.py`, `panel.py`, `pipeline.py`, `registry.py`, `schemas.py`, `sources.py`, `synthetic.py`
* `ml/src/redistml/mockdata/` — `__init__.py`, `dgp.py`, `export.py`, `simulate.py`, `validate.py`
* `ml/tests/` — `conftest.py`, `test_api.py`, `test_end_to_end.py`, `test_mockdata.py`, `test_optimizer.py`, `test_panel_features.py`, `test_postgres_seed.py`, `test_sql_contract.py`
* `ml/tests/pg/` — `schema.sql`
* `mvp/Clients/Contracts/` — `MlContracts.cs`
* `mvp/Clients/` — `IMlServiceClient.cs`, `MlServiceClient.cs`
* `mvp/Config/` — `MlServiceSettings.cs`
* `mvp/Controllers/` — `RedistributionController.cs`
* `mvp/DTOs/Redistribution/` — `MlModelInfoResponseDTO.cs`, `RedistributionRunRequestDTO.cs`, `RedistributionRunResponseDTO.cs`, `RedistributionSuggestionResponseDTO.cs`, `RejectSuggestionDTO.cs`
* `mvp/` — `Dockerfile`
* `mvp/Entities/` — `RedistributionRun.cs`, `RedistributionSuggestion.cs`
* `mvp/Enums/` — `LocationKind.cs`, `RedistributionRunStatus.cs`, `RedistributionSuggestionStatus.cs`
* `mvp/Exceptions/` — `ConflictException.cs`, `InsufficientStockException.cs`, `InvalidSuggestionStateException.cs`, `MlServiceUnavailableException.cs`, `NotFoundException.cs`, `RedistributionInProgressException.cs`, `RedistributionRunNotFoundException.cs`, `RedistributionSuggestionNotFoundException.cs`, `UpstreamUnavailableException.cs`
* `mvp/Interfaces/IRepositories/` — `IRedistributionRepository.cs`
* `mvp/Interfaces/IServices/` — `IRedistributionService.cs`
* `mvp/Middleware/` — `DomainExceptionHandler.cs`
* `mvp/Migrations/` — `20261028120000_AddRedistribution.Designer.cs`, `20261028120000_AddRedistribution.cs`
* `mvp/Repositories/` — `RedistributionRepository.cs`
* `mvp/Services/` — `RedistributionService.cs`
* `tests/ContractCheck/` — `ContractCheck.csproj`, `Program.cs`

## 3. Arquivos modificados e por quê

| Arquivo | Mudança | Motivo |
|---|---|---|
| `mvp/Enums/StockMovementType.cs` | + `TransferIn`, `TransferOut`, `Loss`; `Exit` documentado como consumo | Não confundir remanejamento/perda com demanda. Coluna é texto ⇒ **sem migração**; valores existentes intactos |
| `mvp/Data/Context.cs` | + 2 `DbSet` e mapeamento snake_case de `RedistributionRun`/`RedistributionSuggestion` (enums como texto, FKs `Restrict`, índice em `status`) | Persistência das execuções/sugestões, seguindo o padrão do arquivo |
| `mvp/Migrations/*AddRedistribution*` e `ContextModelSnapshot.cs` | Nova migração (`20261028120000`) + snapshot | Criar as duas tabelas. **Escritos à mão** (ver §6) |
| `mvp/Program.cs` | DI de serviço/repositório, `HttpClient` tipado do ML (opcional), `AddExceptionHandler`/`UseExceptionHandler` | Integração e erros HTTP consistentes |
| `mvp/appsettings.Example*.json` | + seção `MlService` | Configuração do cliente do ML |
| `mvp/Controllers/AuthController.cs` | Injeta `IAuthService` (interface registrada) e `await` em `LoginAsync` | Antes: falha de DI na ativação do controller e a resposta serializava um `Task` |
| `mvp/Config/JwtSettings.cs`, `mvp/Services/TokenService.cs` | `ExpirationHours` (chave que o appsettings já usava); `ExperationHours` mantido como obsoleto; padrão 8 h | Com a chave do appsettings de exemplo (`ExpirationHours`) o valor nunca era lido ⇒ token com validade 0 (só funcionava pela tolerância de relógio de 5 min do JwtBearer) |
| `.gitignore`, `README.md`, `mvp/mvp.http` | Ignorar artefatos Python/`.env`; documentação; exemplos das novas rotas | — |

## 4. Mudanças de comportamento a conhecer
* `UseExceptionHandler()` agora está ativo também em Desenvolvimento: exceções não tratadas voltam como `ProblemDetails`
  (500) em vez da página de erro do desenvolvedor.
* Erros de login (`UnauthorizedAccessException`) passam de **500 para 401**.
* Novas movimentações de remanejamento/perda devem usar os novos tipos; `Exit` continua sendo consumo.
* Aceitar uma sugestão **altera estoque** (origem/destino) e cria duas movimentações.

## 5. Problemas pré-existentes **não** alterados (recomendações)
1. **Nenhum controller anterior tem `[Authorize]`** — o CRUD inteiro é público. Não mudei para não quebrar clientes existentes; só os endpoints novos exigem JWT.
2. **`StockMovementService` não atualiza `Stock.Quantity`**: o saldo é manual (`PUT /api/stocks`) e pode divergir das movimentações. Como as sugestões partem do saldo, isso afeta a qualidade.
3. **`Stock` permite local nulo/duplo** (`PharmacyId`/`HealthUnitId` ambos nulos ou ambos preenchidos) e `BatchId` nulo; o validador do ML avisa, mas o domínio deveria impedir.
4. `StockMovement` não tem data do evento (só `CreatedAt`) nem a contraparte da transferência; importações retroativas distorcem a série semanal.
5. `Address` sem latitude/longitude (hoje a proximidade é um proxy por CEP).
6. Pharmacy não tem repositório/serviço/controller registrados (só a interface do repositório).

## 6. Estado de verificação (o que foi e o que não foi executado)

| Parte | Estado |
|---|---|
| Python (`ml/`) | **37 testes passam** (vazamento, alvo, reprodutibilidade, registro, tiers, SQL, API, contrato, otimizador, mock) + **3 em PostgreSQL real** (executados com `PG_TEST_URL`) |
| Consultas SQL e seed | Testadas em SQLite **e em PostgreSQL 16 real**: `seed.sql` carrega, saldo fecha em SQL (0 divergências), papel `ml_reader` lê e não escreve, `SqlSource` idêntico ao CSV, treino+recomendação direto do banco |
| C# — sintaxe | 38 arquivos novos/alterados analisados sem erro |
| C# — compilação real (SDK .NET 9) | DTOs, serviço, cliente, handler, entidades, enums, exceções e controller: **0 erros, 0 avisos** |
| C# — `RedistributionRepository` | Compilado **contra stubs** das assinaturas do EF Core 9 (o pacote real não estava acessível). Comportamento transacional **não testado** |
| Contrato Python↔C# | `tests/ContractCheck`: **32 verificações** do cliente C# real contra o serviço Python rodando (campos, datas, GUIDs, falhas→503, serviço com repositório falso, execução concorrente, decisões, handler de exceções) |
| **Não compilados/executados** | `Context.cs` (mapeamento), migração + `Designer` + snapshot (escritos à mão), `Program.cs`, `TokenService`, `AuthController` (dependem de EF Core/Npgsql/JwtBearer); a **API .NET** nunca rodou contra PostgreSQL; Docker/Compose; workflow de CI. O DDL de teste (`ml/tests/pg/schema.sql`) foi derivado à mão do snapshot |

**Faça antes de usar:** `dotnet build`; `dotnet ef migrations has-pending-model-changes --project mvp` (se acusar diferença,
apague os 3 arquivos de migração `AddRedistribution` e rode `dotnet ef migrations add AddRedistribution`); aplicar a
migração em um banco de teste; aceitar uma sugestão de ponta a ponta e conferir `stocks` e `stock_movements`;
carregar o mock (`docs/MOCK_DATA.md`) num banco de teste e exercitar `POST /api/redistribution/runs` e o aceite.

## 7. Rodada 2 — dados MOCK do MVP

* **Novo `ml/src/redistml/mockdata/`**: simulador com DGP documentado (demanda verdadeira em 2 camadas + livro-razão com
  reposição, FEFO, ruptura, vencimento e transferências), validação estatística (integridade, Pearson/PIT, recuperação de
  parâmetros, Syntetos–Boylan, fator comum, censura), **oráculo** e exportação (CSV do ML + `seed.sql` no schema real).
  Comandos `redistml mock generate|evaluate`. Documentação: [MOCK_DATA.md](MOCK_DATA.md).
* **`evaluation.paired_bootstrap_gain`**: IC95 do ganho do modelo por *cluster bootstrap* sobre séries; gravado no
  `metadata.json` de cada treino e no `model_evaluation.json` do mock.
* **Correção:** `default_as_of` descartava a última semana completa quando os dados terminavam no sábado/domingo
  (encontrado no teste em PostgreSQL); agora usa a segunda-feira seguinte. Teste de regressão incluído.
* `CsvSource` lê `batches.removed_at` como data.
* Testes novos: `test_mockdata.py` (12, incl. guarda de deriva entre colunas do seed e o `Context.cs`) e
  `test_postgres_seed.py` (3, opcional). CI ganhou o job `postgres`.
* Nenhum arquivo C# foi alterado nesta rodada.

## 8. Como aplicar
`git apply integracao-ml.patch` na raiz do seu repositório original, ou use o repositório completo entregue.
