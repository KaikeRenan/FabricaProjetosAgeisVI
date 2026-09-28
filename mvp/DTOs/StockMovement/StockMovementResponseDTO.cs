using mvp.Enums;

namespace mvp.DTOs.StockMovement
{
    public class StockMovementResponseDTO
    {
        public Guid Id { get; set; }
        public Guid StockId { get; set; }
        public Guid BatchId { get; set; }
        public Guid MedicineId { get; set; }
        public StockMovementType Type { get; set; }
        public int Quantity { get; set; }
    }
}
