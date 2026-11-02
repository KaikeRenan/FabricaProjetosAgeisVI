using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using mvp.Clients;
using mvp.DTOs.Redistribution;
using mvp.Entities;
using mvp.Enums;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Middleware;
using mvp.Services;

class FakeRepo : IRedistributionRepository
{
    public List<(RedistributionRun Run, List<RedistributionSuggestion> S, bool Supersede)> Added = new();
    public DecisionOutcome Next = DecisionOutcome.Applied;
    public int DelayMs = 0;
    public RedistributionSuggestion? Stored;

    public async Task AddRunAsync(RedistributionRun run, IReadOnlyCollection<RedistributionSuggestion> s, bool supersede)
    { if (DelayMs > 0) await Task.Delay(DelayMs); Added.Add((run, s.ToList(), supersede)); }
    public Task<RedistributionRun?> GetRunAsync(Guid id) => Task.FromResult(Added.Select(a => a.Run).FirstOrDefault(r => r.Id == id));
    public Task<List<RedistributionSuggestion>> GetSuggestionsAsync(Guid? runId, RedistributionSuggestionStatus? st, int take)
        => Task.FromResult(Added.SelectMany(a => a.S).Where(x => !runId.HasValue || x.RunId == runId).Take(take).ToList());
    public Task<RedistributionSuggestion?> GetSuggestionAsync(Guid id) => Task.FromResult(Stored);
    public Task<DecisionOutcome> TryRejectAsync(Guid id, string by, string reason) => Task.FromResult(Next);
    public Task<DecisionOutcome> AcceptAndTransferAsync(Guid id, string by) => Task.FromResult(Next);
}

static class P
{
    static int fails = 0, passes = 0;
    static void Check(bool ok, string name) { if (ok) passes++; else { fails++; Console.WriteLine("  FALHOU: " + name); } }

    static MlServiceClient MakeClient(string url, string key)
    {
        var http = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Add("X-Api-Key", key);
        return new MlServiceClient(http, NullLogger<MlServiceClient>.Instance);
    }

    static async Task<T?> Throws<T>(Func<Task> f) where T : Exception
    { try { await f(); return null; } catch (T e) { return e; } }

    static async Task<int> Main()
    {
        // ML_URL / ML_KEY: padrão = `redistml serve-demo` (porta 8765, chave de 32 "k").
        var url = Environment.GetEnvironmentVariable("ML_URL") ?? "http://127.0.0.1:8765/";
        var key = Environment.GetEnvironmentVariable("ML_KEY") ?? new string('k', 32);
        var good = MakeClient(url, key);
        var ct = CancellationToken.None;

        // ---- 1) Contrato real Python -> C# --------------------------------------------------
        var info = await good.GetModelInfoAsync(ct);
        Check(info.ModelVersion.Length > 8 && (info.ServingTier == "lgbm" || info.ServingTier == "baseline"), "model info básico");
        Check(info.NWeeks > 0 && info.NSeries > 0 && info.AvailableVersions.Count >= 1, "model info numérico");
        Check(info.TrainedAt.Year >= 2026 && info.LastTrainWeek.Year >= 2026, "datas do model info (DateTimeOffset/DateOnly)");

        var rec = await good.GetRecommendationsAsync(new mvp.Clients.Contracts.MlRecommendationRequest { LeadTimeDays = 2 }, ct);
        Check(rec.Recommendations.Count > 0, "há recomendações");
        Check(rec.Run.AsOf.Year >= 2026 && rec.Run.HorizonWeeks > 0 && rec.Run.ModelVersion == info.ModelVersion, "run info");
        Check(rec.Summary.NTransfers == rec.Recommendations.Count, "n_transfers == nº de recomendações");
        Check(rec.Recommendations.All(r => Guid.TryParse(r.MedicineId, out _) && Guid.TryParse(r.BatchId, out _)
              && Guid.TryParse(r.FromLocationId, out _) && Guid.TryParse(r.ToLocationId, out _)), "ids são GUIDs");
        Check(rec.Recommendations.All(r => (r.FromLocationKind is "pharmacy" or "health_unit") && (r.ToLocationKind is "pharmacy" or "health_unit")), "kinds válidos");
        Check(rec.Recommendations.All(r => r.Quantity > 0 && r.FromLocationId != r.ToLocationId && r.ShelfLifeAtArrival >= 7), "invariantes de negócio");
        Check(rec.Recommendations.All(r => r.ExpirationDate.Year >= 2026), "expiration_date desserializa");
        Check(rec.Summary.UnitsRescued <= rec.Summary.UnitsAtRisk + 1e-6, "rescued <= at_risk");
        Console.WriteLine($"  contrato OK: tier={rec.Run.ServingTier} recs={rec.Recommendations.Count} warnings={rec.Run.Warnings.Count}");

        // ---- 2) Falhas viram MlServiceUnavailableException (HTTP 503) ------------------------
        var wrongKey = MakeClient(url, "x" + key);
        Check(await Throws<MlServiceUnavailableException>(() => wrongKey.GetModelInfoAsync(ct)) != null, "API key errada -> 503");
        var down = MakeClient("http://127.0.0.1:9/", key);
        Check(await Throws<MlServiceUnavailableException>(() => down.GetModelInfoAsync(ct)) != null, "serviço fora do ar -> 503");
        var unconfigured = new MlServiceClient(new HttpClient(), NullLogger<MlServiceClient>.Instance);
        var ex = await Throws<MlServiceUnavailableException>(() => unconfigured.GetModelInfoAsync(ct));
        Check(ex != null && ex.Message.Contains("não configurado"), "sem configuração -> 503 explicativo");

        // ---- 3) Serviço de aplicação com repositório falso -------------------------------------
        var repo = new FakeRepo();
        var svc = new RedistributionService(good, repo);
        var run = await svc.GenerateRunAsync(new RedistributionRunRequestDTO(), "ana@exemplo.com", ct);
        Check(run.Suggestions.Count == rec.Recommendations.Count && run.Status == RedistributionRunStatus.Succeeded, "serviço persiste as sugestões do ML");
        Check(repo.Added.Count == 1 && repo.Added[0].Supersede, "execução bem-sucedida substitui pendentes anteriores");
        Check(repo.Added[0].S.All(s => s.ExpirationDate.Kind == DateTimeKind.Utc && s.Status == RedistributionSuggestionStatus.Pending), "datas UTC (exigência do Npgsql) e status Pending");
        Check(run.ServingTier == rec.Run.ServingTier && run.ModelVersion == rec.Run.ModelVersion, "rastreabilidade modelo/tier");
        Check(repo.Added[0].Run.TriggeredBy == "ana@exemplo.com", "registra quem disparou");

        var repoFail = new FakeRepo();
        var svcFail = new RedistributionService(down, repoFail);
        Check(await Throws<MlServiceUnavailableException>(() => svcFail.GenerateRunAsync(new(), "x", ct)) != null, "falha do ML propaga 503");
        Check(repoFail.Added.Count == 1 && repoFail.Added[0].Run.Status == RedistributionRunStatus.Failed && !repoFail.Added[0].Supersede
              && repoFail.Added[0].S.Count == 0, "falha fica registrada e NÃO substitui pendentes");

        var slow = new FakeRepo { DelayMs = 800 };
        var svcSlow = new RedistributionService(good, slow);
        var t1 = svcSlow.GenerateRunAsync(new(), "a", ct);
        await Task.Delay(50);
        Check(await Throws<RedistributionInProgressException>(() => svcSlow.GenerateRunAsync(new(), "b", ct)) != null, "execução concorrente -> 409");
        await t1;

        var r2 = new FakeRepo();
        var s2 = new RedistributionService(good, r2);
        foreach (var (outcome, expected) in new (DecisionOutcome, Type)[] {
            (DecisionOutcome.NotFound, typeof(RedistributionSuggestionNotFoundException)),
            (DecisionOutcome.InvalidState, typeof(InvalidSuggestionStateException)),
            (DecisionOutcome.InsufficientStock, typeof(InsufficientStockException)) })
        {
            r2.Next = outcome;
            var e1 = await Throws<BaseException>(() => s2.AcceptAsync(Guid.NewGuid(), "u"));
            var e2 = await Throws<BaseException>(() => s2.RejectAsync(Guid.NewGuid(), new RejectSuggestionDTO { Reason = "motivo" }, "u"));
            Check(e1?.GetType() == expected && e2?.GetType() == expected, $"outcome {outcome} -> {expected.Name}");
        }
        r2.Next = DecisionOutcome.Applied;
        r2.Stored = run.Suggestions.Count > 0 ? repo.Added[0].S[0] : null;
        var accepted = await s2.AcceptAsync(Guid.NewGuid(), "u");
        Check(accepted.Quantity > 0, "aceite devolve a sugestão");

        // ---- 4) Tradução de exceções em HTTP ---------------------------------------------------
        var handler = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);
        async Task<(bool, int)> Handle(Exception e)
        { var ctx = new DefaultHttpContext(); ctx.Response.Body = new MemoryStream(); var h = await handler.TryHandleAsync(ctx, e, ct); return (h, ctx.Response.StatusCode); }
        Check(await Handle(new RedistributionSuggestionNotFoundException()) == (true, 404), "NotFound -> 404");
        Check(await Handle(new InvalidSuggestionStateException()) == (true, 409), "Conflict -> 409");
        Check(await Handle(new InsufficientStockException()) == (true, 409), "InsufficientStock -> 409");
        Check(await Handle(new MlServiceUnavailableException("x")) == (true, 503), "Upstream -> 503");
        Check(await Handle(new UnauthorizedAccessException("x")) == (true, 401), "Unauthorized -> 401");
        Check((await Handle(new InvalidOperationException("x"))).Item1 == false, "desconhecida segue fluxo padrão (500)");

        Console.WriteLine($"\n{passes} verificações OK, {fails} falhas");
        return fails == 0 ? 0 : 1;
    }
}
