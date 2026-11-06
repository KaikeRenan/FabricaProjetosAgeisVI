using mvp.Clients;
using mvp.Clients.Contracts;
using mvp.DTOs.Redistribution;
using mvp.Entities;
using mvp.Enums;
using mvp.Exceptions;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.IServices;

namespace mvp.Services
{
    public class RedistributionService : IRedistributionService
    {
        private const int MaxPageSize = 500;

        // Uma execução por vez neste processo: evita duplo clique/estouro de chamadas ao serviço de ML.
        // (Com várias instâncias da API seria preciso um lock distribuído, ex.: pg_advisory_lock.)
        private static readonly SemaphoreSlim RunGate = new(1, 1);

        private readonly IMlServiceClient _mlClient;
        private readonly IRedistributionRepository _repository;

        public RedistributionService(IMlServiceClient mlClient, IRedistributionRepository repository)
        {
            _mlClient = mlClient;
            _repository = repository;
        }

        public async Task<RedistributionRunResponseDTO> GenerateRunAsync(RedistributionRunRequestDTO dto, string? triggeredBy, CancellationToken cancellationToken)
        {
            if (!await RunGate.WaitAsync(0, cancellationToken))
                throw new RedistributionInProgressException();

            try
            {
                RedistributionRun run;
                List<RedistributionSuggestion> suggestions;

                try
                {
                    var response = await _mlClient.GetRecommendationsAsync(ToMlRequest(dto), cancellationToken);
                    (run, suggestions) = Build(response, triggeredBy);
                }
                catch (UpstreamUnavailableException ex)
                {
                    // Falhas também ficam registradas (observabilidade: quando e por que o ML ficou indisponível).
                    await _repository.AddRunAsync(
                        RedistributionRun.CreateFailed(ex.Message, triggeredBy),
                        Array.Empty<RedistributionSuggestion>(),
                        supersedePending: false);
                    throw;
                }

                await _repository.AddRunAsync(run, suggestions, supersedePending: true);

                return ToRunResponse(run, suggestions);
            }
            finally
            {
                RunGate.Release();
            }
        }

        public async Task<RedistributionRunResponseDTO> GetRunAsync(Guid id)
        {
            var run = await _repository.GetRunAsync(id);

            if (run == null)
                throw new RedistributionRunNotFoundException();

            var suggestions = await _repository.GetSuggestionsAsync(id, null, MaxPageSize);

            return ToRunResponse(run, suggestions);
        }

        public async Task<List<RedistributionSuggestionResponseDTO>> GetSuggestionsAsync(Guid? runId, RedistributionSuggestionStatus? status, int take)
        {
            var size = Math.Clamp(take, 1, MaxPageSize);
            var suggestions = await _repository.GetSuggestionsAsync(runId, status, size);

            return suggestions.Select(ToSuggestionResponse).ToList();
        }

        public async Task<RedistributionSuggestionResponseDTO> AcceptAsync(Guid id, string decidedBy)
        {
            var outcome = await _repository.AcceptAndTransferAsync(id, decidedBy);

            return await AfterDecisionAsync(id, outcome);
        }

        public async Task<RedistributionSuggestionResponseDTO> RejectAsync(Guid id, RejectSuggestionDTO dto, string decidedBy)
        {
            var outcome = await _repository.TryRejectAsync(id, decidedBy, dto.Reason.Trim());

            return await AfterDecisionAsync(id, outcome);
        }

        public async Task<MlModelInfoResponseDTO> GetModelInfoAsync(CancellationToken cancellationToken)
        {
            var info = await _mlClient.GetModelInfoAsync(cancellationToken);

            return new MlModelInfoResponseDTO
            {
                ModelVersion = info.ModelVersion,
                ServingTier = info.ServingTier,
                TrainedAt = info.TrainedAt,
                LastTrainWeek = info.LastTrainWeek,
                SeriesCount = info.NSeries,
                WeeksOfHistory = info.NWeeks,
                CoverageQ90 = info.CoverageQ90,
                AvailableVersions = info.AvailableVersions
            };
        }

        private async Task<RedistributionSuggestionResponseDTO> AfterDecisionAsync(Guid id, DecisionOutcome outcome)
        {
            switch (outcome)
            {
                case DecisionOutcome.NotFound:
                    throw new RedistributionSuggestionNotFoundException();
                case DecisionOutcome.InvalidState:
                    throw new InvalidSuggestionStateException();
                case DecisionOutcome.InsufficientStock:
                    throw new InsufficientStockException();
            }

            var suggestion = await _repository.GetSuggestionAsync(id);

            if (suggestion == null)
                throw new RedistributionSuggestionNotFoundException();

            return ToSuggestionResponse(suggestion);
        }

        private static MlRecommendationRequest ToMlRequest(RedistributionRunRequestDTO dto)
        {
            return new MlRecommendationRequest
            {
                AsOf = dto.AsOf,
                LeadTimeDays = dto.LeadTimeDays,
                MinShelfLifeDays = dto.MinShelfLifeDays,
                MinTransferQty = dto.MinTransferQty,
                DonorDemandZ = dto.DonorDemandZ,
                ReceiverDemandZ = dto.ReceiverDemandZ,
                MaxDistanceKm = dto.MaxDistanceKm
            };
        }

        private static (RedistributionRun Run, List<RedistributionSuggestion> Suggestions) Build(MlRecommendationResponse response, string? triggeredBy)
        {
            var warnings = response.Run.Warnings.Count > 0 ? string.Join("\n", response.Run.Warnings) : null;

            var run = RedistributionRun.CreateSucceeded(
                response.Run.AsOf,
                response.Run.HorizonWeeks,
                response.Run.ModelVersion,
                response.Run.ServingTier,
                response.Run.DataFingerprint,
                warnings,
                triggeredBy,
                response.Summary.UnitsAtRisk,
                response.Summary.UnitsRescued,
                response.ResidualWaste.Sum(r => r.UnavoidableWaste),
                response.AlreadyExpired.Select(e => e.BatchId).Distinct().Count(),
                response.NoForecast.Count);

            var suggestions = response.Recommendations
                .Select(r => new RedistributionSuggestion(
                    run.Id,
                    ParseGuid(r.MedicineId),
                    ParseGuid(r.BatchId),
                    ParseGuid(r.FromLocationId),
                    ParseKind(r.FromLocationKind),
                    ParseGuid(r.ToLocationId),
                    ParseKind(r.ToLocationKind),
                    r.Quantity,
                    r.ExpirationDate,
                    r.DaysToExpiry,
                    r.ProjectedWasteAtSource,
                    r.DistanceKm,
                    r.ProximityCost))
                .ToList();

            return (run, suggestions);
        }

        // Resposta fora do contrato é tratada como falha do serviço dependente (503), nunca como erro 500 do nosso lado.
        private static Guid ParseGuid(string value)
        {
            if (!Guid.TryParse(value, out var id))
                throw new MlServiceUnavailableException("Resposta do serviço de ML com identificador inválido");

            return id;
        }

        private static LocationKind ParseKind(string value)
        {
            return value switch
            {
                "pharmacy" => LocationKind.Pharmacy,
                "health_unit" => LocationKind.HealthUnit,
                _ => throw new MlServiceUnavailableException("Resposta do serviço de ML com tipo de local inválido")
            };
        }

        private static RedistributionRunResponseDTO ToRunResponse(RedistributionRun run, IEnumerable<RedistributionSuggestion> suggestions)
        {
            return new RedistributionRunResponseDTO
            {
                Id = run.Id,
                CreatedAt = run.CreatedAt,
                AsOf = run.AsOf,
                HorizonWeeks = run.HorizonWeeks,
                ModelVersion = run.ModelVersion,
                ServingTier = run.ServingTier,
                Status = run.Status,
                Warnings = string.IsNullOrEmpty(run.Warnings)
                    ? new List<string>()
                    : run.Warnings.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                UnitsAtRisk = run.UnitsAtRisk,
                UnitsRescued = run.UnitsRescued,
                ResidualWasteUnits = run.ResidualWasteUnits,
                AlreadyExpiredBatches = run.AlreadyExpiredBatches,
                NoForecastPairs = run.NoForecastPairs,
                Suggestions = suggestions.Select(ToSuggestionResponse).ToList()
            };
        }

        private static RedistributionSuggestionResponseDTO ToSuggestionResponse(RedistributionSuggestion s)
        {
            return new RedistributionSuggestionResponseDTO
            {
                Id = s.Id,
                RunId = s.RunId,
                MedicineId = s.MedicineId,
                BatchId = s.BatchId,
                FromLocationId = s.FromLocationId,
                FromLocationKind = s.FromLocationKind,
                ToLocationId = s.ToLocationId,
                ToLocationKind = s.ToLocationKind,
                Quantity = s.Quantity,
                ExpirationDate = s.ExpirationDate,
                DaysToExpiry = s.DaysToExpiry,
                ProjectedWasteAtSource = s.ProjectedWasteAtSource,
                DistanceKm = s.DistanceKm,
                ProximityCost = s.ProximityCost,
                Status = s.Status,
                DecidedAt = s.DecidedAt,
                DecidedBy = s.DecidedBy,
                RejectionReason = s.RejectionReason
            };
        }
    }
}
