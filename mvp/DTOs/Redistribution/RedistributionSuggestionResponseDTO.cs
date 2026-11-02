using mvp.Enums;

namespace mvp.DTOs.Redistribution
{
    public class RedistributionSuggestionResponseDTO
    {
        public Guid Id { get; set; }
        public Guid RunId { get; set; }
        public Guid MedicineId { get; set; }
        public Guid BatchId { get; set; }
        public Guid FromLocationId { get; set; }
        public LocationKind FromLocationKind { get; set; }
        public Guid ToLocationId { get; set; }
        public LocationKind ToLocationKind { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int DaysToExpiry { get; set; }
        public double ProjectedWasteAtSource { get; set; }
        public double? DistanceKm { get; set; }
        public double ProximityCost { get; set; }
        public RedistributionSuggestionStatus Status { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string? DecidedBy { get; set; }
        public string? RejectionReason { get; set; }
    }
}
