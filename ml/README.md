# redistml — previsão de demanda e recomendação de remanejamento

Pacote Python do serviço de ML. Visão geral do sistema: [../README.md](../README.md) · decisões: [../docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md) · [model card](../docs/MODEL_CARD.md).

## Módulos (`src/redistml`)

| Módulo | Responsabilidade |
|---|---|
| `config.py` | Todos os hiperparâmetros e o mapa de tipos de movimentação (serializados junto com o modelo) |
| `sources.py` | `SqlSource` (PostgreSQL do backend, só SELECT), `CsvSource`; `RawData` desacopla o ML do banco |
| `panel.py` | Movimentações → painel semanal de demanda (só `Exit`; semanas completas; zeros explícitos); validações |
| `features.py` | Features sem vazamento (tudo ≤ origem) e empilhamento por horizonte |
| `baselines.py` | TSB e `baseline_forecast_next` (tier `baseline`) |
| `model.py` | LightGBM global: média Tweedie + quantil p90, *early stopping* temporal |
| `evaluation.py` | Backtest de origem móvel, métricas, **gate de promoção** |
| `optimizer.py` | Simulação FEFO + casamento guloso doador→receptor |
| `registry.py` | Versões imutáveis + ponteiro `current.json` (troca atômica) |
| `pipeline.py` | `train` (backtest → gate → registro) e `recommend` (previsão → otimização, com tiers) |
| `schemas.py` / `api.py` | Contrato HTTP v1 e serviço FastAPI |
| `synthetic.py` | Dados sintéticos simples (testes rápidos) |
| `mockdata/` | **Mock do MVP**: DGP documentado (`dgp.py`), simulação do livro (`simulate.py`), validação estatística e oráculo (`validate.py`), exportação CSV/seed.sql (`export.py`) — ver [../docs/MOCK_DATA.md](../docs/MOCK_DATA.md) |

## Comandos

```bash
pip install -e ".[api,sql,dev]" && pytest          # 37 testes (+3 com PG_TEST_URL)
redistml mock generate --out data/mock            # dados MOCK do MVP (+ seed.sql do backend)
redistml mock evaluate --data-dir data/mock       # backtest + oráculo + IC do ganho
redistml demo                                      # treina e recomenda com dados sintéticos
redistml serve-demo                                # API sem banco (porta 8765, chave de 32 "k")
redistml train --db-url "$ML_DATABASE_URL" --models-dir artifacts/models
redistml recommend --db-url "$ML_DATABASE_URL" --models-dir artifacts/models
redistml promote <versao> --models-dir artifacts/models
redistml serve                                     # exige ML_API_KEY e ML_DATABASE_URL
```

## Princípios aplicados nos testes
Sem vazamento (truncar o futuro não altera features passadas; treino do backtest só vê alvos ≤ origem), alinhamento de
alvo, reprodutibilidade (mesma semente + dados ⇒ mesmas métricas), invariantes do otimizador (nunca envia além do
excedente, respeita validade, nunca para si mesmo), versões imutáveis, fallback de *tier*, falha fechada de segurança,
contrato do SQL (SQLite) e *snapshot* do contrato HTTP.
