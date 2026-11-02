namespace mvp.DTOs.Redistribution
{
    public class MlModelInfoResponseDTO
    {
        public string ModelVersion { get; set; } = null!;
        public string ServingTier { get; set; } = null!;
        public DateTimeOffset TrainedAt { get; set; }
        public DateOnly LastTrainWeek { get; set; }
        public int SeriesCount { get; set; }
        public int WeeksOfHistory { get; set; }
        public double? CoverageQ90 { get; set; }
        public List<string> AvailableVersions { get; set; } = new();
    }
}
