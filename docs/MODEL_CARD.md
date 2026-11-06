# Model card — previsão de demanda e recomendação de remanejamento

## Propósito e uso pretendido
Apoiar gestores na decisão de remanejar lotes **antes do vencimento**. Não é dispositivo de decisão clínica, não
decide sozinho e não deve ser usado para outra finalidade (ex.: avaliar desempenho de profissionais).

## Dados
* **Demanda** = soma semanal (segunda a domingo, fuso `America/Sao_Paulo`) de `stock_movements` do tipo `Exit`,
  por (local, medicamento). Local = `PharmacyId` ou `HealthUnitId` do `Stock` ao qual a movimentação pertence
  (inclui `Stock` removido logicamente, pois o histórico aponta para eles).
* Painel completo com zeros explícitos a partir da primeira demanda da série; nunca usa a semana parcial.
* **Não usa dados pessoais** de pacientes — só quantidades agregadas por semana.

## Modelo
| Item | Escolha |
|---|---|
| Tier `baseline` | TSB (Teunter–Syntetos–Babai) para a média + desvio padrão das últimas 13 semanas (p90 ≈ média + 1,28·σ) |
| Tier `lgbm` | LightGBM global (uma só base para todas as séries), **horizonte como feature** (h = 1..12), duas saídas: média (objetivo Tweedie, p = 1,3) e quantil 0,90 |
| Features | defasagens (0–7, 11, 12, 25, 51), médias móveis (4/8/13/26), desvio 13s, taxa de semanas com demanda, semanas desde a última demanda, idade da série, média acumulada, média da rede para o medicamento, sazonalidade da semana-alvo, códigos de local/medicamento |
| Incerteza | σ semanal ≈ (q90 − média)/1,2816; usado como `z·σ` nos cenários do otimizador (aproximação, assume semanas independentes) |
| Decisão | simulação diária FEFO → excedente do doador (o que vence sem ser consumido) e demanda não atendida do receptor → casamento guloso (lote mais urgente primeiro; destino mais próximo; *lead time* 2 dias; validade mínima de 7 dias na chegada; não envia mais do que o destino consome antes do vencimento) |

## Avaliação
Backtest de **origem móvel** (4 origens, passo de 6 semanas, horizonte 12): em cada origem T o treino só vê alvos ≤ T.
Métricas: WAPE (ponderado por volume), viés (Σŷ−Σy)/Σy, MAE, pinball e cobertura do p90. Baselines: média móvel 8 semanas,
TSB, *naive*. **Gate:** LightGBM só é promovido com histórico ≥ 82 semanas (padrão), ganho ≥ 3 % de WAPE sobre o melhor
baseline e |viés| ≤ 15 %.

### Resultados disponíveis (nenhum com dados reais)
| Conjunto | LightGBM | Melhor baseline (TSB) | Oráculo | Ganho (IC95) | Decisão |
|---|---|---|---|---|---|
| **Mock do MVP** (DGP documentado, 264 séries, 156 semanas; ver [MOCK_DATA.md](MOCK_DATA.md)) | 0,2499 | 0,2595 | 0,2337 | 3,7 % (2,5 % a 5,1 %) | promovido, por pouco |
| Sintético simples dos testes (130 semanas, ruído dominante) | 0,487 | 0,491 | — | 0,8 % | não promovido (correto) |

No mock, o ganho é positivo nas 4 dobras (1,6 %, 3,8 %, 4,3 %, 5,2 %), mas a 1ª fica abaixo do limiar de 3 %. A cobertura
do p90 é 0,894 (alvo 0,90). O modelo fecha cerca de 37 % da distância entre o baseline e o oráculo. **O mock contém
sazonalidade e surtos por construção**: o resultado prova que a esteira aprende estrutura quando ela existe, não o ganho real.
A incerteza do ganho é estimada por *cluster bootstrap* sobre séries (`paired_bootstrap_gain`) e gravada em
`metadata.json` a cada treino.

## Limitações e riscos
* **Demanda censurada:** quando o estoque zera, a dispensação observada é menor que a demanda real e o modelo tende a subestimar (no mock, a censura reduz a demanda observada em ~4,5 %).
* **`Exit` histórico ambíguo:** se havia remanejamento/perda lançados como `Exit`, a demanda é superestimada até o histórico ser corrigido.
* **Sem sazonalidade/tendência no tier `baseline`.**
* **Séries novas ou esparsas** têm previsão pouco confiável; pares sem histórico não recebem previsão.
* `Stock.Quantity` é mantido manualmente (não é derivado das movimentações): se estiver errado, as sugestões estarão erradas.
* O otimizador é heurístico e usa proximidade por CEP; não considera custo, prazo real de transporte, cadeia fria nem
  regras de medicamentos controlados — **a pessoa que aceita é responsável por verificá-las.**
* Pesos iguais para todos os medicamentos: um item crítico com pouco volume pesa pouco no WAPE.

## Monitoramento e retreino
Acompanhar semanalmente: `serving_tier`, `status/warnings`, idade do dado, WAPE/viés realizados (comparar previsão da
execução com a demanda observada depois), **taxa de aceite e motivos de recusa**, unidades resgatadas vs. vencidas.
Retreinar semanalmente (job); promoção automática só via gate; *rollback* com `redistml promote <versão>`.
Sinais de que é hora de revisar: viés persistente > ±15 %, cobertura do p90 < 0,8, aceite < 30 %, mudança de catálogo/rede.
