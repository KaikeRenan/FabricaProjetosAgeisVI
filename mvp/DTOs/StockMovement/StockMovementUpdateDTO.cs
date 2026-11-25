using mvp.Enums;

namespace mvp.DTOs.StockMovement
{
    public class StockMovementUpdateDTO
    {
        public Guid StockId { get; set; }
        public Guid BatchId { get; set; }
        public Guid MedicineId { get; set; }
        public StockMovementType Type { get; set; }
        public int Quantity { get; set; }
        public DateTime? OccurredAt { get; set; }
        public Guid? TransferId { get; set; }
    }
}
