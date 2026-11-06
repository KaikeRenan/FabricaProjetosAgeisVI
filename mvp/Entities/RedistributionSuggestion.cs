using mvp.Enums;

namespace mvp.Entities
{
    /// <summary>
    /// Sugestão de remanejamento: mover Quantity unidades do lote BatchId do local de origem para o de destino.
    /// A decisão (Status/DecidedBy/RejectionReason) é gravada de forma atômica pelo repositório e é o
    /// RÓTULO usado para medir se o modelo está sendo útil.
    /// </summary>
    public class RedistributionSuggestion : BaseEntity
    {
        public Guid RunId { get; private set; }
        public Guid MedicineId { get; private set; }
        public Guid BatchId { get; private set; }
        public Guid FromLocationId { get; private set; }
        public LocationKind FromLocationKind { get; private set; }
        public Guid ToLocationId { get; private set; }
        public LocationKind ToLocationKind { get; private set; }
        public int Quantity { get; private set; }
        public DateTime ExpirationDate { get; private set; }
        public int DaysToExpiry { get; private set; }
        public double ProjectedWasteAtSource { get; private set; }
        public double? DistanceKm { get; private set; }
        public double ProximityCost { get; private set; }
        public RedistributionSuggestionStatus Status { get; private set; }
        public DateTime? DecidedAt { get; private set; }
        public string? DecidedBy { get; private set; }
        public string? RejectionReason { get; private set; }

        protected RedistributionSuggestion()
        {
        }

        public RedistributionSuggestion(
            Guid runId,
            Guid medicineId,
            Guid batchId,
            Guid fromLocationId,
            LocationKind fromLocationKind,
            Guid toLocationId,
            LocationKind toLocationKind,
            int quantity,
            DateTime expirationDate,
            int daysToExpiry,
            double projectedWasteAtSource,
            double? distanceKm,
            double proximityCost)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantidade deve ser positiva");

            this.RunId = runId;
            this.MedicineId = medicineId;
            this.BatchId = batchId;
            this.FromLocationId = fromLocationId;
            this.FromLocationKind = fromLocationKind;
            this.ToLocationId = toLocationId;
            this.ToLocationKind = toLocationKind;
            this.Quantity = quantity;
            this.ExpirationDate = DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc);
            this.DaysToExpiry = daysToExpiry;
            this.ProjectedWasteAtSource = projectedWasteAtSource;
            this.DistanceKm = distanceKm;
            this.ProximityCost = proximityCost;
            this.Status = RedistributionSuggestionStatus.Pending;
        }
    }
}
