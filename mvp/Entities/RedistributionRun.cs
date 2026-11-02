using mvp.Enums;

namespace mvp.Entities
{
    /// <summary>
    /// Uma execução do serviço de ML. Guarda COM QUE modelo/dados as sugestões foram geradas
    /// (rastreabilidade e auditoria) e o resultado agregado.
    /// </summary>
    public class RedistributionRun : BaseEntity
    {
        public DateOnly AsOf { get; private set; }
        public int HorizonWeeks { get; private set; }
        public string ModelVersion { get; private set; }
        public string ServingTier { get; private set; }
        public string DataFingerprint { get; private set; }
        public RedistributionRunStatus Status { get; private set; }
        public string? Warnings { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? TriggeredBy { get; private set; }
        public double UnitsAtRisk { get; private set; }
        public double UnitsRescued { get; private set; }
        public double ResidualWasteUnits { get; private set; }
        public int AlreadyExpiredBatches { get; private set; }
        public int NoForecastPairs { get; private set; }

        protected RedistributionRun()
        {
            ModelVersion = null!;
            ServingTier = null!;
            DataFingerprint = null!;
        }

        public static RedistributionRun CreateSucceeded(
            DateOnly asOf,
            int horizonWeeks,
            string modelVersion,
            string servingTier,
            string dataFingerprint,
            string? warnings,
            string? triggeredBy,
            double unitsAtRisk,
            double unitsRescued,
            double residualWasteUnits,
            int alreadyExpiredBatches,
            int noForecastPairs)
        {
            return new RedistributionRun
            {
                AsOf = asOf,
                HorizonWeeks = horizonWeeks,
                ModelVersion = modelVersion,
                ServingTier = servingTier,
                DataFingerprint = dataFingerprint,
                Status = RedistributionRunStatus.Succeeded,
                Warnings = warnings,
                TriggeredBy = triggeredBy,
                UnitsAtRisk = unitsAtRisk,
                UnitsRescued = unitsRescued,
                ResidualWasteUnits = residualWasteUnits,
                AlreadyExpiredBatches = alreadyExpiredBatches,
                NoForecastPairs = noForecastPairs
            };
        }

        public static RedistributionRun CreateFailed(string errorMessage, string? triggeredBy)
        {
            return new RedistributionRun
            {
                AsOf = DateOnly.FromDateTime(DateTime.UtcNow),
                ModelVersion = "unknown",
                ServingTier = "unknown",
                DataFingerprint = "unknown",
                Status = RedistributionRunStatus.Failed,
                ErrorMessage = errorMessage,
                TriggeredBy = triggeredBy
            };
        }
    }
}
