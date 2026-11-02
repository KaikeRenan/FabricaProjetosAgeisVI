from __future__ import annotations

import argparse
import json

import pandas as pd

from .pipeline import load_bundle, recommend, train
from .registry import ModelRegistry
from .sources import CsvSource, SqlSource
from .synthetic import make_synthetic


def _show(out):
    pd.set_option("display.width", 220, "display.max_columns", 30)
    print("\nEXECUÇÃO:", json.dumps({k: out.run[k] for k in ("model_version", "serving_tier", "status", "warnings")},
                                    indent=2, default=str))
    print("RESUMO:", json.dumps(out.result.summary, indent=2, default=str))
    r = out.result.recommendations
    if len(r):
        r = r.copy()
        for c in ("medicine_id", "from_location_id", "to_location_id"):
            r[c] = r[c].str[:8]
        print("\nTOP REMANEJAMENTOS:\n", r.sort_values("days_to_expiry").head(12)
              .drop(columns=["batch_id", "from_location_kind", "to_location_kind"]).to_string(index=False))


def _source(a):
    if getattr(a, "db_url", None):
        return SqlSource(a.db_url)
    return CsvSource(a.data_dir)


def _mock(a):
    from dataclasses import replace
    from pathlib import Path

    from .mockdata import MockConfig, simulate
    from .mockdata.export import write_backend_seed, write_local_csv, write_reports
    from .mockdata.validate import validate_dataset

    out = Path(a.out if a.mock_cmd == "generate" else a.data_dir)
    if a.mock_cmd == "generate":
        cfg = MockConfig()
        if a.seed is not None:
            cfg = replace(cfg, seed=a.seed)
        if a.weeks is not None:
            cfg = replace(cfg, weeks=a.weeks)
        cfg = replace(cfg, demo_events=a.demo_events)
        ds = simulate(cfg)
        diag = validate_dataset(ds)
        write_local_csv(ds, out)
        write_backend_seed(ds, out / "backend")
        write_reports(ds, diag, out)
        print(f"gerado em {out}/  (CSV do ML) e {out}/backend/ (seed.sql + CSVs do schema real)")
        print("integridade do livro-razão:", "OK" if diag["todas_as_checagens_passaram"] else "FALHOU",
              {k: v for k, v in diag["integridade_do_livro_razao"].items() if not v} or "")
        print(json.dumps({k: diag[k] for k in ("gerador_vs_especificacao", "livro_razao")}, indent=2, default=str))
        return
    # evaluate: backtest + baselines + oráculo
    from .config import BacktestConfig, DataConfig, ModelConfig
    from .evaluation import paired_bootstrap_gain, promotion_decision, rolling_origin_backtest
    from .mockdata.validate import oracle_benchmark
    from .panel import build_demand_panel
    raw = CsvSource(out).load()
    truth = pd.read_csv(out / "truth.csv", parse_dates=["week"])
    panel = build_demand_panel(raw, DataConfig())
    bt = rolling_origin_backtest(panel, ModelConfig(), BacktestConfig())
    dec = promotion_decision(bt, BacktestConfig())
    res = dict(backtest=bt.summary.round(4).to_dict("index"), promocao=dec,
               ganho_vs_melhor_baseline=paired_bootstrap_gain(bt.predictions, "lgbm", dec["best_baseline"]),
               ganho_por_dobra=bt.by_fold.pivot(index="fold", columns="model", values="wape").assign(
                   ganho_vs_tsb=lambda d: (1 - d["lgbm"] / d["tsb"]).round(4))["ganho_vs_tsb"].to_dict(),
               oraculo=oracle_benchmark(bt.predictions, truth), cobertura_q90=round(bt.coverage_q90, 4),
               wape_por_horizonte=bt.by_horizon["wape"].round(4).to_dict())
    (out / "model_evaluation.json").write_text(json.dumps(res, indent=2, default=str))
    print(json.dumps(res, indent=2, default=str))


def main():
    ap = argparse.ArgumentParser("redistml")
    sub = ap.add_subparsers(dest="cmd", required=True)
    d = sub.add_parser("demo", help="dados sintéticos: treina e recomenda")
    d.add_argument("--models-dir", default="artifacts/demo")
    for name in ("train", "recommend"):
        p = sub.add_parser(name)
        p.add_argument("--data-dir")
        p.add_argument("--db-url", help="URL SQLAlchemy do PostgreSQL (usuário somente-leitura)")
        p.add_argument("--models-dir", default="artifacts/models")
    pr = sub.add_parser("promote", help="aponta 'current' para uma versão (rollback)")
    pr.add_argument("version")
    pr.add_argument("--models-dir", default="artifacts/models")
    sub.add_parser("serve", help="sobe a API (requer ML_API_KEY, ML_DATABASE_URL)").add_argument("--port", type=int, default=8000)
    mk = sub.add_parser("mock", help="gera/valida/avalia o conjunto de dados MOCK do MVP")
    msub = mk.add_subparsers(dest="mock_cmd", required=True)
    mg = msub.add_parser("generate", help="simula, valida e exporta (CSV do ML + seed.sql do backend)")
    mg.add_argument("--out", default="data/mock")
    mg.add_argument("--seed", type=int, default=None)
    mg.add_argument("--weeks", type=int, default=None)
    mg.add_argument("--demo-events", type=int, default=10,
                    help="entregas de lotes de validade curta 3 semanas antes de as_of (0 = histórico puramente calibrado)")
    me = msub.add_parser("evaluate", help="backtest + baselines + ORÁCULO sobre o mock gerado")
    me.add_argument("--data-dir", default="data/mock")
    sd = sub.add_parser("serve-demo", help="API com dados SINTÉTICOS, sem banco (desenvolvimento/contrato)")
    sd.add_argument("--port", type=int, default=8765)
    sd.add_argument("--api-key", default="k" * 32)
    a = ap.parse_args()

    if a.cmd == "demo":
        raw = make_synthetic()
        meta = train(raw, a.models_dir)
        print(json.dumps({k: meta[k] for k in ("model_version", "serving_tier", "backtest_summary", "coverage_q90",
                                                  "promotion")}, indent=2, default=str))
        _show(recommend(raw, a.models_dir))
    elif a.cmd == "train":
        meta = train(_source(a).load(), a.models_dir)
        print(json.dumps({k: meta.get(k) for k in ("model_version", "serving_tier", "n_weeks", "promotion")},
                         indent=2, default=str))
    elif a.cmd == "recommend":
        _show(recommend(_source(a).load(), a.models_dir))
    elif a.cmd == "promote":
        ModelRegistry(a.models_dir).set_current(a.version)
        print("current ->", load_bundle(a.models_dir).version)
    elif a.cmd == "mock":
        _mock(a)
    elif a.cmd == "serve-demo":
        import tempfile

        import uvicorn

        from .api import Settings, create_app
        raw = make_synthetic(seed=7, n_locations=6, n_medicines=10, weeks=110)
        d = tempfile.mkdtemp(prefix="redistml-demo-")
        train(raw, d)
        print(f"modelo demo em {d}; API em http://127.0.0.1:{a.port} (header X-Api-Key: {a.api_key})")
        uvicorn.run(create_app(Settings(models_dir=d, database_url="demo", api_key=a.api_key), raw_loader=lambda: raw),
                    host="127.0.0.1", port=a.port, log_level="warning")
    else:
        import uvicorn
        uvicorn.run("redistml.api:app_factory", factory=True, host="0.0.0.0", port=a.port)


if __name__ == "__main__":
    main()
