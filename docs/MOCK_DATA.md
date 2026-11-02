# Dados MOCK do MVP — cartão de dados

> **Tudo aqui é fictício.** Nenhum dado de paciente, unidade ou empresa real. Os números deste documento validam a
> esteira (dados → modelo → recomendação), **não** o desempenho em produção.

## 1. Por que um simulador e não linhas aleatórias

Gerei os dados a partir de um **processo gerador de dados (DGP) explícito e reprodutível**. Como a demanda verdadeira é
conhecida, dá para fazer o que dados reais não permitem:

1. **Validar o gerador contra a própria especificação.** Ele amostra mesmo a distribuição que declara.
2. **Medir o piso de erro irredutível com um oráculo.** O oráculo conhece a média verdadeira, então nenhum modelo pode
   ser melhor que ele de forma sustentável.
3. **Separar a demanda verdadeira da demanda observada.** A ruptura censura a dispensação, e dá para medir esse viés.

## 2. Processo gerador (duas camadas)

**Camada 1: demanda verdadeira, independente do estoque.** Para a unidade *u*, o medicamento *m* e a semana *t*:

```
log λ = log(pop_u/1000 · taxa_m) + a_um + b_uc + A_c·cos(2π(semana_t − pico_c)/52,18)
        + tendência_c·anos_t + β_c·ℓ_u·W_t + η_umt + γ_c·log(dias_úteis_t/5)
D ~ BinomialNegativa(média = λ, dispersão = k_c)
```

| Componente | Especificação |
|---|---|
| Escala | População atendida (log-normal: farmácias ~30 mil, UBS ~9 mil) × taxa por 1.000 hab./semana do catálogo |
| Heterogeneidade | Efeitos aleatórios unidade×medicamento (σ = 0,25) e unidade×classe (σ = 0,10). Cada unidade dispensa cada item com P = 0,92 |
| Sazonalidade por classe | Respiratórios: amplitude 0,45, pico na semana 28 (inverno). Alergia: 0,40, semana 38 (primavera). Analgésicos: 0,15, semana 14. Crônicos: 0,03 |
| Tendência | Crônicos +4 %/ano (envelhecimento); demais ≈ 0 |
| Surtos regionais `W_t` | AR(1) (ρ = 0,8) mais surtos com chegada de Poisson (1,5/ano, amplitude log-normal, duração de 3 a 8 semanas). São compartilhados entre unidades, com sensibilidade ℓ_u ~ N(1; 0,15) e β por classe (respiratórios 0,6) |
| Persistência `η` | AR(1) idiossincrático (ρ = 0,6, σ = 0,08), iniciado na distribuição estacionária |
| Calendário | Feriados nacionais, fixos e móveis (Páscoa, Carnaval, Corpus Christi), reduzem os dias de atendimento |
| Ruído de contagem | Binomial Negativa com dispersão k por classe: crônicos 40 (estável), respiratórios 6, baixo giro 2,5 (intermitente) |
| Dentro da semana | Divisão multinomial pelos dias de funcionamento. UBS: seg–sex. Farmácia: seg–sáb (sábado com peso 0,3) |

**Camada 2: o livro de movimentações que o backend vê.**

- Reposição *order-up-to*:
  - **Requisição:** quinzenal nas farmácias, mensal nas UBS.
  - **Lead time:** 1 a 3 semanas, com 10 % de atrasos adicionais.
  - **Tamanho do pedido:** baseado na média das últimas 8 semanas dispensadas e arredondado ao múltiplo de embalagem.
- Estilos de gestão por unidade: 6 normais, 2 que superestocam e 2 enxutas.
- Consumo FEFO dia a dia. Quando falta estoque, há **ruptura**: a demanda não é registrada (censura).
- Validade residual de 55–95 % da validade total. 15 % dos lotes chegam com validade curta (25–100 dias).
- Lotes que vencem geram movimentação `Loss`. Linhas de estoque zeradas recebem `removed_at`.
- Transferências manuais antigas (`TransferOut`/`TransferIn`) do excedente para unidades em ruptura.
- **Eventos de demonstração** (`--demo-events`, padrão 10 no `generate`, 0 na configuração-base): três semanas
  antes da data de referência entram entregas grandes de lotes com validade curta, como uma doação ou compra emergencial.
  Eles **só adicionam entradas**; um teste garante que a demanda verdadeira é idêntica com e sem eventos. Servem para que o
  estoque atual tenha itens em risco para a demonstração e estão listados em `dgp.json`.

**Boas práticas de reprodutibilidade aplicadas**

- Semente única (`20261028`) dividida em **fluxos aleatórios independentes** (demanda, livro, IDs, entidades). Mudar a
  política de estoque não altera a demanda verdadeira, e um teste cobre isso.
- Toda a configuração e os parâmetros sorteados por unidade ficam em `dgp.json`.
- Mesma semente e mesma configuração geram arquivos idênticos (há teste).
- **Só os parâmetros operacionais foram calibrados**, com metas declaradas antes da calibração. Os parâmetros de demanda
  foram fixados a priori e nunca ajustados para favorecer o modelo.

## 3. O conjunto gerado

| Item | Valor |
|---|---|
| Período | 2023-10-30 a 2026-10-26 (exclusive), 156 semanas |
| Locais | 4 farmácias + 6 UBS (nomes com "(mock)", CEPs de Marília/SP, CNPJ/CNES fictícios) |
| Medicamentos | 30 da lista de essenciais, em 5 classes terapêuticas, incluindo 6 de baixo giro |
| Séries (unidade × medicamento) | 264 |
| Movimentações | 183,532 (Exit 170,994, Entry 11,879, TransferIn 254, TransferOut 254, Loss 151) |
| Lotes / linhas de estoque | 11,879 / 12,131 |
| Estoque atual | 85,107 unidades em 528 linhas. Vencem em até 30, 60 e 90 dias: 2,820, 8,493 e 9,829 unidades |

## 4. Validação

**4.1 Integridade do livro (11 checagens, todas aprovadas).**

- O saldo de cada estoque é igual a entradas menos saídas.
- O saldo corrente nunca fica negativo.
- Não há consumo após a validade, e perdas só ocorrem a partir dela.
- O lote e a movimentação são do mesmo medicamento.
- Integridade referencial completa.
- A fabricação vem antes da validade.
- Estoque vivo tem saldo maior que zero; estoque removido tem saldo zero.
- Cada estoque pertence a exatamente um local.
- Não há linha viva duplicada para o mesmo local e lote.

O teste foi repetido **em PostgreSQL 16 real** depois do `seed.sql`: 0 divergências de saldo em SQL.

**4.2 O gerador cumpre a especificação.** Os testes usam a média e a dispersão verdadeiras.

- A estatística de Pearson média é **1,0033** (IC95 0,9857–1,0210; o esperado é 1).
- O PIT randomizado contra Uniforme(0,1) dá KS = 0,0051 e **p = 0,2367**.

**4.3 Recuperação de parâmetros.** Regressão harmônica sobre o total semanal de cada classe, em escala log.
Cada célula mostra o valor especificado / o estimado.

| Classe | Amplitude | Semana de pico | Tendência anual | Autocorrelação lag 52 |
|---|---|---|---|---|
| Alergia | 0.4 / 0.4123 | 38 / 37.5 | 0.0 / -0.0011 | 0.826 |
| Analgésicos | 0.15 / 0.1436 | 14 / 15.9 | 0.01 / 0.0086 | 0.478 |
| Anti-infecciosos | 0.05 / 0.0638 | 20 / 23.4 | 0.0 / 0.0048 | 0.108 |
| Crônicos | 0.03 / 0.0368 | 28 / 29.6 | 0.04 / 0.0418 | 0.509 |
| Respiratórios | 0.45 / 0.4922 | 28 / 27.5 | 0.01 / 0.008 | 0.842 |

As classes de alta amplitude (respiratórios, alergia) são recuperadas com precisão. Nas classes de amplitude baixa, a
semana de pico fica imprecisa, como era de esperar: há pouco sinal sazonal para estimar.

**4.4 Propriedades típicas de demanda de medicamentos.**

- Classificação Syntetos–Boylan (ADI/CV²): 203 séries suaves, 36 intermitentes,
  22 erráticas e 3 irregulares.
- Índice de dispersão mediano: 6,31, ou seja, superdisperso.
- Demanda semanal média por série: P10 = 1.15, mediana = 47.49, P90 = 226.12.

**4.5 Fator comum entre unidades.** Correlação média dos resíduos suavizados, depois de remover a tendência e a sazonalidade de cada série.

| Classe | Na média verdadeira λ | Na demanda observada |
|---|---|---|
| Alergia | 0.237 | 0.025 |
| Analgésicos | 0.32 | 0.073 |
| Anti-infecciosos | 0.191 | -0.002 |
| Crônicos | 0.045 | 0.023 |
| Respiratórios | 0.466 | 0.063 |

O fator compartilhado existe no processo, sobretudo nos respiratórios e analgésicos. Na contagem semanal observada ele
quase desaparece sob o ruído de contagem. Por isso mostro os dois: é uma limitação honesta do quanto um modelo
consegue aproveitar esse fator.

**4.6 Calibração operacional** (metas declaradas antes de calibrar).

| Métrica | Meta | Obtido |
|---|---|---|
| Semanas-série com ruptura | 3–8 % | 7,6 % |
| Viés da demanda observada (censura) | (medido) | -4,5 % |
| Perda por vencimento / recebido | 0,3–3 % | 0,25 %: **abaixo da meta**; mantido sem novo ajuste |

## 5. O que o modelo consegue nesses dados

Backtest de origem móvel: 4 origens, horizonte de 12 semanas, 12,672 previsões. O "oráculo" usa a média verdadeira λ.

| Modelo | WAPE | Viés |
|---|---|---|
| **Oráculo (piso irredutível)** | **0,2337** | — |
| LightGBM | 0,2499 | 0,0024 |
| TSB | 0,2595 | -0,0345 |
| Média móvel 8 semanas | 0,2697 | -0,0146 |
| Naive | 0,3617 | 0,0119 |

- **Ganho do LightGBM sobre o melhor baseline:** 3,7 %, IC95 [2,5 %; 5,1 %]
  por *cluster bootstrap* sobre as 264 séries, com P(ganho > 0) = 1,0.
- **Ganho por dobra:** 1,6 %, 3,8 %, 4,3 %, 5,2 %. O ganho é positivo em todas, mas a primeira dobra fica **abaixo** dos 3 % do gate: a promoção
  passa no agregado com margem pequena.
- O modelo **fecha 37 %** da distância entre o melhor baseline e o oráculo.
- Cobertura do p90: 0,8943 (alvo 0,90).
- Resultado: **promovido** (tier `lgbm`). Treinei **direto do PostgreSQL** com o papel somente-leitura, usando o histórico
  ainda sem os eventos de demonstração, e apliquei o modelo ao estoque atual final: 14 transferências, resgatando 877 de
  1.175 unidades projetadas para vencer.

**Leia com cuidado.** O DGP *contém* sazonalidade, tendência e surtos. O ganho mostra que a esteira aprende estrutura
quando ela existe; não é estimativa do ganho com dados reais.

## 6. Como usar

```bash
cd ml
redistml mock generate --out data/mock        # simula, valida e exporta (~10 s); --demo-events 0 = só histórico calibrado
redistml mock evaluate --data-dir data/mock   # backtest + baselines + oráculo + IC do ganho (~4 min)
redistml train --data-dir data/mock --models-dir artifacts/models
redistml recommend --data-dir data/mock --models-dir artifacts/models
```

**Carregar no PostgreSQL do backend**, num banco **novo**, depois de `dotnet ef database update`:
```bash
cd ml/data/mock/backend && psql -d SEU_BANCO -f seed.sql      # aborta se já houver medicamentos/movimentações
psql -d SEU_BANCO -f ../../../../docs/sql/ml_reader.sql        # papel somente-leitura do ML
```

Depois:
1. Crie um usuário pela API (`POST /api/users`); o seed não cria usuários, para não versionar credenciais.
2. Faça login.
3. Rode `redistml train --db-url ...` e chame `POST /api/redistribution/runs`.

**Arquivos gerados**

| Arquivo | Conteúdo |
|---|---|
| `movements/stocks/batches/medicines/locations.csv` | Layout do ML (`CsvSource`), com horário local |
| `backend/*.csv` + `backend/seed.sql` | Schema **real** do EF (snake_case, `timestamptz` em UTC) |
| `truth.csv` | Média e demanda verdadeiras, observado e não atendido por série-semana (**só para avaliação; nunca para treino**) |
| `dgp.json` | Configuração, semente, parâmetros sorteados, onda regional e eventos de demonstração |
| `diagnostics.json` / `model_evaluation.json` | As validações deste documento |

## 7. Limitações

- Ainda é um modelo simplificado do mundo real. Não há preços, cadeia fria, medicamentos controlados, falta no
  fornecedor central, nem mudança de protocolo clínico.
- Não há pedidos em trânsito no schema do backend. Por isso o otimizador desconsidera reposições a caminho, e
  `unmet_demand_before` deve ser lido como "demanda não coberta pelo estoque atual", não como previsão de ruptura.
- A perda por vencimento no histórico ficou abaixo da meta. O estoque em risco do estoque atual depende dos eventos de demonstração.
- **Os CNPJs são fictícios, mas têm dígitos verificadores válidos**, então podem coincidir com um real por acaso. Nunca use
  para nada fiscal.
- O `ml/tests/pg/schema.sql` usado no teste foi derivado à mão do snapshot do EF. No CI com .NET, prefira as migrações reais.
