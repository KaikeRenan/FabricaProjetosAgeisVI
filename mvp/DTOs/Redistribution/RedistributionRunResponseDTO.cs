using mvp.Enums;

namespace mvp.DTOs.Redistribution
{
    public class RedistributionRunResponseDTO
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateOnly AsOf { get; set; }
        public int HorizonWeeks { get; set; }
        public string ModelVersion { get; set; } = null!;

        /// <summary>"lgbm" (modelo de ML promovido) ou "baseline" (estatístico; poucos dados ou modelo não promovido).</summary>
        public string ServingTier { get; set; } = null!;
        public RedistributionRunStatus Status { get; set; }
        public List<string> Warnings { get; set; } = new();
        public double UnitsAtRisk { get; set; }
        public double UnitsRescued { get; set; }
        public double ResidualWasteUnits { get; set; }
        public int AlreadyExpiredBatches { get; set; }
        public int NoForecastPairs { get; set; }
        public List<RedistributionSuggestionResponseDTO> Suggestions { get; set; } = new();
    }
}
