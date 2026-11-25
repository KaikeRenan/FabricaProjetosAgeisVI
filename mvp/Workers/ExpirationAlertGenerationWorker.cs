using Microsoft.Extensions.Options;
using mvp.Config;
using mvp.Interfaces.IServices;

namespace mvp.Workers
{
    public class ExpirationAlertGenerationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<ExpirationAlertSettings> _settings;
        private readonly ILogger<ExpirationAlertGenerationWorker> _logger;

        public ExpirationAlertGenerationWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<ExpirationAlertSettings> settings,
            ILogger<ExpirationAlertGenerationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = _settings.Value;

            if (!settings.Enabled)
            {
                _logger.LogInformation("Geração automática de alertas desabilitada.");
                return;
            }

            if (settings.RunOnStartup)
                await TryRunAsync(settings, stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(0.01, settings.IntervalHours)));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await TryRunAsync(settings, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // desligamento normal
            }
        }

        private async Task TryRunAsync(ExpirationAlertSettings settings, CancellationToken ct)
        {
            try
            {
                // o repositório/service são Scoped; BackgroundService é Singleton
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IExpirationAlertService>();

                var result = await service.GenerateAsync(settings.DaysAhead, settings.AutoResolve, ct);

                _logger.LogInformation(
                    "Alertas de vencimento: {Scanned} lotes avaliados, {Created} criados, {Resolved} resolvidos.",
                    result.Scanned, result.Created, result.Resolved);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // sem isto, uma falha transitória (DB fora, migration pendente) derruba o host:
                // BackgroundServiceExceptionBehavior.StopHost é o padrão no .NET 6+
                _logger.LogError(ex, "Falha ao gerar alertas de vencimento.");
            }
        }
    }
}