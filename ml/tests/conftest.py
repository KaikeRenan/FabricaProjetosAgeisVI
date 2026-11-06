import pytest

from redistml.config import DataConfig, ModelConfig
from redistml.panel import build_demand_panel
from redistml.synthetic import make_synthetic


@pytest.fixture(scope="session")
def raw():
    return make_synthetic(seed=3, n_locations=4, n_medicines=6, weeks=110)


@pytest.fixture(scope="session")
def panel(raw):
    return build_demand_panel(raw, DataConfig())


@pytest.fixture(scope="session")
def mcfg():
    return ModelConfig(horizon_weeks=8, n_estimators=80)
