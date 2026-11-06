using System.Text.Json.Serialization;

namespace mvp.Clients.Contracts
{
    // Espelho do contrato v1 do serviço de ML (docs/contracts/ml-service.schemas.v1.json).
    // Nomes JSON explícitos: não dependemos de política de nomenclatura para siglas como "n_transfers".

    public class MlRecommendationRequest
    {
        [JsonPropertyName("as_of")] public DateOnly? AsOf { get; set; }
        [JsonPropertyName("lead_time_days")] public int? LeadTimeDays { get; set; }
        [JsonPropertyName("min_shelf_life_days")] public int? MinShelfLifeDays { get; set; }
        [JsonPropertyName("min_transfer_qty")] public int? MinTransferQty { get; set; }
        [JsonPropertyName("donor_demand_z")] public double? DonorDemandZ { get; set; }
        [JsonPropertyName("receiver_demand_z")] public double? ReceiverDemandZ { get; set; }
        [JsonPropertyName("max_distance_km")] public double? MaxDistanceKm { get; set; }
    }

    public class MlRunInfo
    {
        [JsonPropertyName("model_version")] public string ModelVersion { get; set; } = string.Empty;
        [JsonPropertyName("serving_tier")] public string ServingTier { get; set; } = string.Empty;
        [JsonPropertyName("trained_at")] public DateTimeOffset TrainedAt { get; set; }
        [JsonPropertyName("data_fingerprint")] public string DataFingerprint { get; set; } = string.Empty;
        [JsonPropertyName("as_of")] public DateOnly AsOf { get; set; }
        [JsonPropertyName("horizon_weeks")] public int HorizonWeeks { get; set; }
        [JsonPropertyName("data_age_days")] public int DataAgeDays { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
        [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = new();
    }

    public class MlSummary
    {
        [JsonPropertyName("units_at_risk")] public double UnitsAtRisk { get; set; }
        [JsonPropertyName("units_rescued")] public double UnitsRescued { get; set; }
        [JsonPropertyName("rescue_rate")] public double? RescueRate { get; set; }
        [JsonPropertyName("n_transfers")] public int NTransfers { get; set; }
        [JsonPropertyName("unmet_demand_before")] public double UnmetDemandBefore { get; set; }
    }

    public class MlSuggestion
    {
        [JsonPropertyName("medicine_id")] public string MedicineId { get; set; } = string.Empty;
        [JsonPropertyName("batch_id")] public string BatchId { get; set; } = string.Empty;
        [JsonPropertyName("batch_number")] public string BatchNumber { get; set; } = string.Empty;
        [JsonPropertyName("from_location_id")] public string FromLocationId { get; set; } = string.Empty;
        [JsonPropertyName("from_location_kind")] public string FromLocationKind { get; set; } = string.Empty;
        [JsonPropertyName("to_location_id")] public string ToLocationId { get; set; } = string.Empty;
        [JsonPropertyName("to_location_kind")] public string ToLocationKind { get; set; } = string.Empty;
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("expiration_date")] public DateTime ExpirationDate { get; set; }
        [JsonPropertyName("days_to_expiry")] public int DaysToExpiry { get; set; }
        [JsonPropertyName("shelf_life_at_arrival")] public int ShelfLifeAtArrival { get; set; }
        [JsonPropertyName("distance_km")] public double? DistanceKm { get; set; }
        [JsonPropertyName("proximity_cost")] public double ProximityCost { get; set; }
        [JsonPropertyName("projected_waste_at_source")] public double ProjectedWasteAtSource { get; set; }
    }

    public class MlResidualWaste
    {
        [JsonPropertyName("unavoidable_waste")] public double UnavoidableWaste { get; set; }
    }

    public class MlStockItem
    {
        [JsonPropertyName("batch_id")] public string BatchId { get; set; } = string.Empty;
    }

    public class MlRecommendationResponse
    {
        [JsonPropertyName("run")] public MlRunInfo Run { get; set; } = new();
        [JsonPropertyName("summary")] public MlSummary Summary { get; set; } = new();
        [JsonPropertyName("recommendations")] public List<MlSuggestion> Recommendations { get; set; } = new();
        [JsonPropertyName("residual_waste")] public List<MlResidualWaste> ResidualWaste { get; set; } = new();
        [JsonPropertyName("already_expired")] public List<MlStockItem> AlreadyExpired { get; set; } = new();
        [JsonPropertyName("no_forecast")] public List<MlStockItem> NoForecast { get; set; } = new();
    }

    public class MlModelInfo
    {
        [JsonPropertyName("model_version")] public string ModelVersion { get; set; } = string.Empty;
        [JsonPropertyName("serving_tier")] public string ServingTier { get; set; } = string.Empty;
        [JsonPropertyName("trained_at")] public DateTimeOffset TrainedAt { get; set; }
        [JsonPropertyName("last_train_week")] public DateOnly LastTrainWeek { get; set; }
        [JsonPropertyName("n_series")] public int NSeries { get; set; }
        [JsonPropertyName("n_weeks")] public int NWeeks { get; set; }
        [JsonPropertyName("coverage_q90")] public double? CoverageQ90 { get; set; }
        [JsonPropertyName("available_versions")] public List<string> AvailableVersions { get; set; } = new();
    }
}
