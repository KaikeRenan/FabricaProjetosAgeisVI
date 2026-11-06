# Arquitetura e decisões técnicas

## 1. Contexto

O backend (.NET 9 + EF Core + PostgreSQL) controla estoque por local (farmácia **ou** unidade de saúde), lote e
medicamento. O objetivo desta integração é **recomendar remanejamentos** para evitar vencimento (desperdício) sem
causar ruptura, usando previsão de demanda por série temporal. O sistema é *apoio à decisão*: uma pessoa aceita ou recusa.

## 2. Componentes e fluxo

```
Usuário ──JWT──► API .NET ──POST /v1/recommendations (X-Api-Key)──► Serviço de ML ──SELECT──► PostgreSQL
                   │  grava Run + Suggestions (Pending)                  │ carrega modelo (current)
                   │                                                     ▼
                   └─ accept: UPDATE atômico + Stock + 2 StockMovements   Registro de modelos (versões imutáveis)
Job semanal (redistml train) ──SELECT──► PostgreSQL ──► backtest ► gate ► nova versão ► current
```

1. `POST /api/redistribution/runs` → a API chama o ML; o ML lê movimentações/estoques, monta a série semanal de demanda por
   (local, medicamento), prevê 12 semanas, simula o consumo FEFO e casa excedentes com necessidades.
2. A API persiste a execução (modelo, *tier*, avisos, totais) e as sugestões `Pending`; as pendentes antigas viram `Superseded`.
3. Uma pessoa **aceita** (a API executa a transferência na mesma transação) ou **recusa** com motivo.

## 3. Decisões (formato: decisão → motivo → consequência)

**D1. Serviço Python separado, não ML.NET/ONNX dentro da API.**
O ecossistema de séries temporais (LightGBM, pandas, avaliação por origem móvel) está em Python, e reimplementar features
e otimizador em C# criaria *training-serving skew* (a mesma lógica em dois lugares, divergindo). Consequência: um
processo a mais para operar e um contrato HTTP a manter (D4).

**D2. Inferência em lote com resultado persistido, não previsão no caminho de cada requisição.**
A decisão "o que remanejar" é semanal, não por clique. Persistir a execução dá auditoria (qual modelo/dados geraram
cada sugestão), latência previsível e a trilha de decisões que serve de rótulo para avaliar o modelo. O treino fica
*fora* do HTTP (job agendado): é caro, não-idempotente em tempo e não deve ser acionável por qualquer cliente.

**D3. O ML lê o banco com papel somente-leitura (`docs/sql/ml_reader.sql`); o .NET é o único escritor.**
Evita enviar milhares de linhas por HTTP a cada execução e mantém uma só fonte de verdade do schema (migrações do EF).
O papel enxerga apenas as seis tabelas necessárias — **não** `users` (e-mails e hashes). Custo: o ML acopla-se aos nomes
de tabelas/colunas; mitigado por teste de contrato SQL (hoje em SQLite; deve ser repetido em PostgreSQL no CI).

**D4. Contrato-primeiro, versionado (`/v1`), com *snapshot* dos schemas e verificação cruzada Python↔C#.**
`docs/contracts/ml-service.schemas.v1.json` é gerado dos modelos Pydantic; o teste falha se o contrato mudar sem
revisão. `tests/ContractCheck` roda o cliente C# real contra o serviço real. Nomes JSON são explícitos no C#
(`[JsonPropertyName]`) para não depender de política de nomenclatura com siglas (`n_transfers`).

**D5. Degradação graciosa em dois *tiers* (`baseline` → `lgbm`) com gate de promoção.**
O projeto é novo; modelos globais precisam de ≥ 52 semanas de alvos e o backtest exige mais (padrão: 82 semanas).
Em vez de recusar servir, o serviço usa um baseline estatístico (TSB + desvio das últimas 13 semanas) e **diz** que
está nele (`serving_tier` + aviso em cada resposta). O LightGBM só é promovido se vencer o melhor baseline em ≥ 3 % de
WAPE e tiver |viés| ≤ 15 %. Nunca se serve um modelo não promovido.

**D6. Humano no circuito e *feedback loop* explícito.**
Aceite/recusa (com motivo) é gravado de forma atômica. Taxa de aceite e motivos de recusa são o único sinal sobre se as
sugestões são úteis *na prática* — o backtest só mede erro de previsão, não utilidade operacional.

**D7. Aceitar uma sugestão é uma transação atômica com *compare-and-set*.**
`UPDATE … WHERE status='Pending'` garante que só uma requisição concorrente "ganha"; a baixa na origem é
`UPDATE … WHERE quantity >= q` (sem *lost update* com dispensações simultâneas); entrada no destino e os dois
`StockMovement` entram na mesma transação. Qualquer falha desfaz tudo. Não usei *rowversion*/`xmin` para não
complicar o modelo do EF; o CAS cobre o caso.

**D8. Semântica explícita de movimentação.**
Só `Exit` é demanda. Remanejamentos e perdas têm tipos próprios (`TransferOut/In`, `Loss`); do contrário o modelo
aprenderia o remanejamento passado como consumo e se retroalimentaria. Como a coluna é texto, não houve migração.
*Atenção:* saídas históricas que na verdade foram remanejamento/perda continuam como `Exit` e superestimam a demanda.

**D9. Tempo e fuso.**
Semanas começam na segunda-feira e usam `created_at` convertido para `America/Sao_Paulo` (uma dispensação de domingo
22h não pode cair na semana seguinte). Datas de validade são gravadas como 00:00Z e permanecem em UTC (para não
"voltar um dia"). No .NET, datas para `timestamptz` são marcadas `Utc` (exigência do Npgsql).

**D10. Erros e resiliência.**
Falha de rede/timeout/HTTP≠2xx/resposta fora do contrato no cliente do ML → `MlServiceUnavailableException` → 503
genérico (detalhes só em log). Falhas ficam registradas como `Run` com status `Failed`. Uma execução por vez por
processo (semáforo) evita rajadas ao ML. Exceções de domínio viram `ProblemDetails` num único handler.

**D11. Segurança.**
ML exige `X-Api-Key` (comparação em tempo constante) e **falha fechada**: sem chave forte (≥ 24 caracteres) o serviço
não sobe. Segredos só por variável de ambiente. Endpoints novos exigem JWT. Limites de validação (Pydantic/DataAnnotations)
nos parâmetros do otimizador.

## 4. Modos de falha conhecidos

| Falha | Comportamento |
|---|---|
| ML fora do ar / timeout | API responde 503; `Run` `Failed` registrado; sugestões pendentes anteriores **permanecem** |
| Dados defasados (> 21 dias sem movimentação) | Execução segue com `status=degraded` e aviso |
| Histórico insuficiente / modelo não promovido | *Tier* `baseline` + aviso |
| Par (local, medicamento) com estoque e sem histórico | Entra em `no_forecast`; não é recomendado nem para doar nem para receber |
| Origem sem saldo no aceite (consumo concorrente) | 409 `InsufficientStock`; nada é alterado |
| Duas pessoas aceitam a mesma sugestão | A segunda recebe 409 (estado inválido) |

## 5. Limitações de projeto (assumidas)

* Heurística gulosa (lote mais urgente primeiro, destino mais próximo), **não ótima** globalmente; explicável e auditável.
* Proximidade por prefixo de CEP (o `Address` não tem coordenadas).
* Sem custo/preço, janelas de transporte, cadeia fria ou regras de medicamentos controlados.
* O ML lê todas as movimentações a cada execução (ok para MVP; veja OPERATIONS.md para escala).
* O semáforo de execução é por processo; com várias instâncias da API use *advisory lock* do PostgreSQL.
