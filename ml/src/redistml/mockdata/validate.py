"""Validação do mock: (1) integridade do livro-razão, (2) o gerador cumpre a própria especificação,
(3) propriedades estatísticas esperadas de demanda de medicamentos, (4) piso de erro (oráculo)."""
from __future__ import annotations

import numpy as np
import pandas as pd
from scipy import stats

from ..panel import KEYS, add_location
from .simulate import MockDataset

IN_TYPES, OUT_TYPES = ("Entry", "TransferIn"), ("Exit", "TransferOut", "Loss")


# ------------------------------------------------------------------------------------ 1) livro-razão
def ledger_checks(ds: MockDataset) -> dict[str, bool]:
    r = ds.raw
    m, s, b = r.movements, r.stocks, r.batches
    signed = m["quantity"].where(m["type"].isin(IN_TYPES), -m["quantity"])
    bal = signed.groupby(m["stock_id"]).sum().reindex(s["stock_id"]).fillna(0).to_numpy()
    ms = m.assign(signed=signed).sort_values(["stock_id", "created_at"], kind="stable")
    running_min = ms.groupby("stock_id")["signed"].cumsum().min()
    mb = m.merge(b[["batch_id", "medicine_id", "expiration_date"]], on="batch_id", suffixes=("", "_batch"))
    consume = mb[mb["type"].isin(["Exit", "TransferOut"])]
    loss = mb[mb["type"] == "Loss"]
    live = s["removed_at"].isna()
    return {
        "saldo_do_estoque_igual_a_entradas_menos_saidas": bool((bal == s["quantity"].to_numpy()).all()),
        "saldo_corrente_nunca_negativo": bool(running_min >= 0),
        "quantidades_inteiras_positivas": bool((m["quantity"] > 0).all()),
        "sem_consumo_apos_a_validade": bool((consume["created_at"] < consume["expiration_date"]).all()),
        "perda_somente_apos_a_validade": bool((loss["created_at"] >= loss["expiration_date"]).all()),
        "movimento_e_lote_do_mesmo_medicamento": bool((mb["medicine_id"] == mb["medicine_id_batch"]).all()),
        "integridade_referencial": bool(m["stock_id"].isin(s["stock_id"]).all() and m["batch_id"].isin(b["batch_id"]).all()
                                        and s["batch_id"].isin(b["batch_id"]).all()),
        "fabricacao_antes_da_validade": bool((b["fabrication_date"] < b["expiration_date"]).all()),
        "estoque_vivo_tem_saldo_e_removido_tem_zero": bool((s.loc[live, "quantity"] > 0).all() and (s.loc[~live, "quantity"] == 0).all()),
        "exatamente_um_local_por_estoque": bool((s["pharmacy_id"].notna() ^ s["health_unit_id"].notna()).all()),
        "sem_duplicidade_local_lote_vivo": bool(~add_location(s[live]).duplicated(["location_id", "batch_id"]).any()),
    }


# ----------------------------------------------------------------------- 2) o gerador cumpre a especificação
def sampler_checks(truth: pd.DataFrame, seed: int = 0) -> dict:
    """Com λ e k conhecidos: (a) estatística de Pearson média ≈ 1; (b) PIT randomizado ~ Uniforme(0,1) (KS)."""
    lam, k, y = truth["true_mean"].to_numpy(), truth["nb_k"].to_numpy(), truth["true_demand"].to_numpy()
    z = (y - lam) ** 2 / (lam + lam**2 / k)
    se = z.std(ddof=1) / np.sqrt(len(z))
    p = k / (k + lam)
    rng = np.random.default_rng(seed)
    cdf_lo, pmf = stats.nbinom.cdf(y - 1, k, p), stats.nbinom.pmf(y, k, p)
    pit = cdf_lo + rng.random(len(y)) * pmf
    ks = stats.kstest(pit, "uniform")
    return {"pearson_medio": round(float(z.mean()), 4), "pearson_erro_padrao": round(float(se), 4),
            "pearson_ic95": [round(float(z.mean() - 1.96 * se), 4), round(float(z.mean() + 1.96 * se), 4)],
            "pit_ks_estatistica": round(float(ks.statistic), 4), "pit_ks_pvalor": round(float(ks.pvalue), 4)}


def _harmonic_fit(total: pd.Series, weeks: pd.DatetimeIndex) -> dict:
    t = np.arange(len(weeks)) / 52.18
    woy = weeks.isocalendar().week.to_numpy().astype(float)
    X = np.column_stack([np.ones(len(t)), t, np.cos(2 * np.pi * woy / 52.18), np.sin(2 * np.pi * woy / 52.18)])
    beta, *_ = np.linalg.lstsq(X, np.log(np.maximum(total.to_numpy(), 1.0)), rcond=None)
    amp = float(np.hypot(beta[2], beta[3]))
    peak = float((np.degrees(np.arctan2(beta[3], beta[2])) / 360 * 52.18) % 52.18)
    return {"tendencia_anual_log": round(float(beta[1]), 4), "amplitude": round(amp, 4), "semana_pico": round(peak, 1)}


def demand_diagnostics(ds: MockDataset) -> dict:
    tr = ds.truth
    out: dict = {}
    # --- classificação de Syntetos-Boylan (ADI / CV²) sobre a demanda verdadeira
    def sb(g):
        nz = g[g > 0]
        adi = len(g) / max(len(nz), 1)
        cv2 = float((nz.std(ddof=0) / nz.mean()) ** 2) if len(nz) > 1 else 0.0
        return pd.Series({"adi": adi, "cv2": cv2, "zero_share": float((g == 0).mean()), "mean": g.mean(),
                          "disp_index": g.var(ddof=1) / max(g.mean(), 1e-9)})
    per = tr.groupby(KEYS)["true_demand"].apply(lambda g: sb(g)).unstack()
    per["classe"] = np.select([(per.adi < 1.32) & (per.cv2 < 0.49), (per.adi < 1.32), (per.cv2 < 0.49)],
                              ["suave", "errática", "intermitente"], "irregular (lumpy)")
    out["classificacao_syntetos_boylan"] = per["classe"].value_counts().to_dict()
    out["fracao_de_zeros_mediana"] = round(float(per["zero_share"].median()), 4)
    out["demanda_media_semanal_quantis"] = {q: round(float(per["mean"].quantile(q)), 2) for q in (0.1, 0.5, 0.9)}
    out["indice_de_dispersao_mediano"] = round(float(per["disp_index"].median()), 2)
    # --- recuperação de parâmetros (agregado por classe terapêutica) vs especificação
    weeks = pd.DatetimeIndex(sorted(tr["week"].unique()))
    spec = ds.config
    from .dgp import CLASS_PARAMS
    rec = {}
    for c, g in tr.groupby("therapeutic_class"):
        total = g.groupby("week")["true_demand"].sum().reindex(weeks)
        fit = _harmonic_fit(total, weeks)
        acf52 = float(pd.Series(np.log(np.maximum(total.to_numpy(), 1))).autocorr(52)) if len(total) > 60 else None
        rec[c] = {**fit, "especificado": {"amplitude": CLASS_PARAMS[c]["amp"], "semana_pico": CLASS_PARAMS[c]["peak"],
                                          "tendencia_anual": CLASS_PARAMS[c]["trend"]},
                  "autocorrelacao_lag52": None if acf52 is None else round(acf52, 3)}
    out["recuperacao_de_parametros_por_classe"] = rec
    # --- fator comum entre unidades: correlação média dos RESÍDUOS suavizados (tendência+sazonalidade removidas por série).
    # (correlação de diferenças semanais seria dominada pelo ruído de contagem e subestimaria o fator comum)
    corr, corr_true = {}, {}
    t = np.arange(len(weeks)) / 52.18
    woy = weeks.isocalendar().week.to_numpy().astype(float)
    X = np.column_stack([np.ones(len(t)), t, np.cos(2 * np.pi * woy / 52.18), np.sin(2 * np.pi * woy / 52.18)])
    for col, dest in (("true_demand", corr), ("true_mean", corr_true)):
        for c, g in tr.groupby("therapeutic_class"):
            pv = g.pivot_table(index="week", columns=KEYS, values=col).reindex(weeks)
            Y = np.log1p(pv.to_numpy())
            ok = ~np.isnan(Y).any(axis=0)
            Y = Y[:, ok]
            res = Y - X @ np.linalg.lstsq(X, Y, rcond=None)[0]
            sm = pd.DataFrame(res).rolling(4, min_periods=4).mean().dropna().to_numpy()
            cm = np.corrcoef(sm.T)
            dest[c] = round(float(np.nanmean(cm[np.triu_indices_from(cm, 1)])), 3)
    out["correlacao_media_entre_series_residuos_suavizados"] = {"observado": corr, "lambda_verdadeiro": corr_true}
    # --- censura por ruptura
    out["censura"] = {"semanas_serie_com_ruptura": round(float((tr["unmet"] > 0).mean()), 4),
                      "vies_da_demanda_observada": round(float(tr["observed"].sum() / tr["true_demand"].sum() - 1), 4)}
    return out


def ledger_rates(ds: MockDataset) -> dict:
    m = ds.raw.movements
    q = m.groupby("type")["quantity"].sum()
    entry = float(q.get("Entry", 0))
    r = ds.raw
    snap = r.stocks[r.stocks["removed_at"].isna()].merge(r.batches, on="batch_id")
    as_of = pd.Timestamp(ds.meta["as_of"])
    days = (snap["expiration_date"] - as_of).dt.days
    return {"movimentos_por_tipo": m["type"].value_counts().to_dict(),
            "perda_por_vencimento_sobre_recebido": round(float(q.get("Loss", 0)) / entry, 5),
            "snapshot_unidades": int(snap["quantity"].sum()), "snapshot_linhas": int(len(snap)),
            "snapshot_vence_em_30d": int(snap.loc[days <= 30, "quantity"].sum()),
            "snapshot_vence_em_60d": int(snap.loc[days <= 60, "quantity"].sum()),
            "snapshot_vence_em_90d": int(snap.loc[days <= 90, "quantity"].sum())}


def validate_dataset(ds: MockDataset) -> dict:
    checks = ledger_checks(ds)
    return {"integridade_do_livro_razao": checks, "todas_as_checagens_passaram": all(checks.values()),
            "gerador_vs_especificacao": sampler_checks(ds.truth), "demanda": demand_diagnostics(ds),
            "livro_razao": ledger_rates(ds)}


# ------------------------------------------------------------------------------------ 4) oráculo
def oracle_benchmark(predictions: pd.DataFrame, truth: pd.DataFrame) -> dict:
    """Mesmas linhas do backtest: erro do modelo vs erro do ORÁCULO (que conhece λ verdadeiro).
    O oráculo é o melhor desempenho possível; a diferença é a margem que ainda existe."""
    t = truth.rename(columns={"week": "target_week"})[KEYS + ["target_week", "true_mean", "true_demand"]]
    p = predictions.merge(t, on=KEYS + ["target_week"], how="left")
    w = lambda yhat: float(np.abs(p["y"] - yhat).sum() / p["y"].sum())  # noqa: E731
    res = {"linhas": int(len(p)), "wape_oraculo_vs_observado": round(w(p["true_mean"]), 4)}
    for m in ("lgbm", "tsb", "ma8", "naive"):
        res[f"wape_{m}"] = round(w(p[m]), 4)
    res["margem_lgbm_sobre_oraculo"] = round(res["wape_lgbm"] - res["wape_oraculo_vs_observado"], 4)
    res["margem_melhor_baseline_sobre_oraculo"] = round(min(res["wape_tsb"], res["wape_ma8"]) - res["wape_oraculo_vs_observado"], 4)
    return res
