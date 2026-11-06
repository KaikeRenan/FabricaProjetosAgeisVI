import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from redistml.api import Settings, create_app
from redistml.config import BacktestConfig
from redistml.pipeline import train

KEY = "k" * 32
H = {"x-api-key": KEY}


@pytest.fixture(scope="module")
def client(raw, mcfg, tmp_path_factory):
    d = tmp_path_factory.mktemp("models")
    train(raw, d, mcfg=mcfg, bcfg=BacktestConfig(n_splits=2, step_weeks=4))
    app = create_app(Settings(models_dir=str(d), database_url="x", api_key=KEY), raw_loader=lambda: raw)
    return TestClient(app)


def test_health_is_public(client):
    r = client.get("/health")
    assert r.status_code == 200 and r.json()["model_loaded"] is True


@pytest.mark.parametrize("headers", [{}, {"x-api-key": "errada"}])
def test_auth_required(client, headers):
    assert client.post("/v1/recommendations", json={}, headers=headers).status_code == 401
    assert client.get("/v1/model", headers=headers).status_code == 401


def test_model_info(client):
    j = client.get("/v1/model", headers=H).json()
    assert j["serving_tier"] in ("lgbm", "baseline") and len(j["available_versions"]) >= 1


def test_recommendations_contract_and_invariants(client):
    r = client.post("/v1/recommendations", json={"lead_time_days": 2}, headers=H)
    assert r.status_code == 200, r.text
    j = r.json()
    assert j["run"]["serving_tier"] in ("lgbm", "baseline") and j["run"]["model_version"]
    assert j["summary"]["units_rescued"] <= j["summary"]["units_at_risk"] + 1e-6
    for s in j["recommendations"]:
        assert s["from_location_id"] != s["to_location_id"] and s["quantity"] > 0
        assert s["from_location_kind"] in ("pharmacy", "health_unit") and s["shelf_life_at_arrival"] >= 7


def test_request_validation(client):
    assert client.post("/v1/recommendations", json={"lead_time_days": -1}, headers=H).status_code == 422
    assert client.post("/v1/recommendations", json={"campo_inexistente": 1}, headers=H).status_code == 422


def test_fails_closed_without_strong_key(monkeypatch):
    monkeypatch.setenv("ML_API_KEY", "curta")
    monkeypatch.setenv("ML_DATABASE_URL", "sqlite://")
    with pytest.raises(RuntimeError):
        Settings.from_env()


def test_contract_snapshot():
    """O .NET depende deste contrato. Se mudar de propósito: apague o snapshot, rode o teste (ele regenera),
    revise o diff e atualize o cliente .NET (ou versione /v2 se quebrar compatibilidade)."""
    from redistml import schemas as sc

    snap = Path(__file__).resolve().parents[2] / "docs" / "contracts" / "ml-service.schemas.v1.json"
    current = {m.__name__: m.model_json_schema() for m in (sc.RecommendationRequest, sc.RecommendationResponse,
                                                            sc.ModelInfo, sc.Health)}
    current = json.loads(json.dumps(current, sort_keys=True))
    if not snap.exists():
        snap.parent.mkdir(parents=True, exist_ok=True)
        snap.write_text(json.dumps(current, indent=2, sort_keys=True, ensure_ascii=False))
    assert json.loads(snap.read_text()) == current, "Contrato v1 do serviço de ML mudou."
