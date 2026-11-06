import numpy as np
import pandas as pd

from redistml.config import OptimizerConfig
from redistml.optimizer import optimize, simulate_fefo

AS_OF = pd.Timestamp("2026-09-28")


def fc(loc, med, weekly, H=4):
    return pd.DataFrame({"location_id": loc, "medicine_id": med, "h": range(1, H + 1),
                         "mean": float(weekly), "q90": float(weekly) * 1.3})


def snap(rows):
    return pd.DataFrame(rows, columns=["location_id", "medicine_id", "batch_id", "batch_number", "quantity",
                                       "expiration_date"])


def test_fefo_consumes_oldest_first_and_reports_waste():
    waste, unmet = simulate_fefo([("old", 100, 10), ("new", 100, 20)], np.full(28, 5.0))
    assert abs(waste["old"] - 50) < 1e-6     # dias 0-9: 10 * 5 = 50 consumidos de 100
    assert abs(waste["new"] - 50) < 1e-6     # dias 10-19: 10 * 5 = 50 consumidos de 100
    assert abs(unmet.sum() - 8 * 5) < 1e-6   # dias 20-27 sem estoque: 8 * 5 de demanda não atendida


def test_transfers_respect_surplus_shelf_life_and_no_self():
    forecast = pd.concat([fc("A", "m", 7), fc("B", "m", 70)], ignore_index=True)
    s = snap([("A", "m", "b1", "L1", 500, AS_OF + pd.Timedelta(days=20)),   # excedente grande, vence em 20d
              ("B", "m", "b2", "L2", 10, AS_OF + pd.Timedelta(days=200))])
    res = optimize(forecast, s, AS_OF, OptimizerConfig())
    r = res.recommendations
    assert len(r) == 1
    row = r.iloc[0]
    assert (row.from_location_id, row.to_location_id) == ("A", "B")
    assert row.quantity <= 500 and row.shelf_life_at_arrival >= 7
    # não pode enviar mais do que o destino consegue consumir antes do vencimento (~10/dia * 18d)
    assert row.quantity <= 70 / 7 * 18 + 1
    assert res.summary["units_rescued"] <= res.summary["units_at_risk"]


def test_too_short_shelf_life_is_not_moved():
    forecast = pd.concat([fc("A", "m", 1), fc("B", "m", 70)], ignore_index=True)
    s = snap([("A", "m", "b1", "L1", 300, AS_OF + pd.Timedelta(days=5))])
    res = optimize(forecast, s, AS_OF, OptimizerConfig(lead_time_days=2, min_shelf_life_days=7))
    assert res.recommendations.empty and len(res.residual_waste) == 1


def test_expired_and_cold_start_are_reported():
    forecast = fc("A", "m", 10)
    s = snap([("A", "m", "b0", "X", 40, AS_OF - pd.Timedelta(days=3)),
              ("Z", "n", "b9", "Y", 40, AS_OF + pd.Timedelta(days=30))])
    res = optimize(forecast, s, AS_OF, OptimizerConfig())
    assert len(res.already_expired) == 1 and len(res.no_forecast) == 1


def test_conservative_donor_moves_less():
    forecast = pd.concat([fc("A", "m", 20), fc("B", "m", 70)], ignore_index=True)
    s = snap([("A", "m", "b1", "L1", 200, AS_OF + pd.Timedelta(days=24))])
    base = optimize(forecast, s, AS_OF, OptimizerConfig(donor_demand_z=0.0)).summary["units_at_risk"]
    cons = optimize(forecast, s, AS_OF, OptimizerConfig(donor_demand_z=2.0)).summary["units_at_risk"]
    assert cons <= base
