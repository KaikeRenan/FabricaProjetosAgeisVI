"""Toda configuração/hiperparâmetro fica aqui e é serializada junto com o modelo (reprodutibilidade)."""
from __future__ import annotations

from dataclasses import dataclass, field

# Mapa do valor persistido em stock_movements.type (EF: HasConversion<string>) -> semântica de negócio.
# O enum original só tinha Entry/Exit; TransferIn/TransferOut/Loss foram adicionados nesta integração para que
# remanejamento e perda NÃO sejam confundidos com consumo. Exit continua sendo tratado como dispensação (demanda).
DEFAULT_MOVEMENT_MAP: dict = {
    "Entry": "entry",
    "Exit": "dispense",          # ÚNICA que representa DEMANDA
    "TransferIn": "transfer_in",
    "TransferOut": "transfer_out",
    "Loss": "loss",
}


@dataclass(frozen=True)
class DataConfig:
    movement_type_map: dict = field(default_factory=lambda: dict(DEFAULT_MOVEMENT_MAP))
    demand_kinds: tuple = ("dispense",)


@dataclass(frozen=True)
class ModelConfig:
    horizon_weeks: int = 12
    lags: tuple = (0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 25, 51)
    rolling_windows: tuple = (4, 8, 13, 26)
    # Tweedie (1<p<2): massa em zero + cauda positiva = demanda intermitente
    tweedie_variance_power: float = 1.3
    quantile: float = 0.9
    n_estimators: int = 600
    learning_rate: float = 0.04
    num_leaves: int = 31
    min_child_samples: int = 40
    subsample: float = 0.8
    colsample_bytree: float = 0.8
    early_stopping_rounds: int = 40
    val_weeks: int = 12
    min_train_weeks: int = 52        # abaixo disso (+ backtest) o serviço opera no tier 'baseline'
    seed: int = 42


@dataclass(frozen=True)
class BacktestConfig:
    n_splits: int = 4
    step_weeks: int = 6
    min_improvement: float = 0.03   # challenger precisa ganhar >=3% de WAPE do melhor baseline
    max_abs_bias: float = 0.15      # |viés| agregado máximo aceito


@dataclass(frozen=True)
class OptimizerConfig:
    lead_time_days: int = 2          # dias entre decidir e o lote estar disponível no destino
    min_shelf_life_days: int = 7     # validade mínima restante exigida no destino na chegada
    min_transfer_qty: int = 1
    donor_demand_z: float = 0.0      # >0: doador assume mais demanda -> menos excedente (mais seguro)
    receiver_demand_z: float = 0.0   # >0: receptor assume mais demanda -> mais necessidade
    max_distance_km: float | None = None


@dataclass(frozen=True)
class ServingConfig:
    max_data_age_days: int = 21      # dado mais velho que isso gera aviso e marca a execução como 'degraded'
