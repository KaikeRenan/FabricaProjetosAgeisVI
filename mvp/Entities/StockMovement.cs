using mvp.Enums;

namespace mvp.Entities
{
    public class StockMovement : BaseEntity
    {
        public Guid StockId { get; private set; }
        public Guid BatchId { get; private set; }
        public Guid MedicineId { get; private set; }
        public StockMovementType Type { get; private set; }
        public int Quantity { get; private set; }

        protected StockMovement()
        {
        }

        public StockMovement(Guid stockId, Guid batchId, Guid medicineId, StockMovementType type, int quantity)
        {
            this.StockId = stockId;
            this.BatchId = batchId;
            this.MedicineId = medicineId;
            this.Type = type;
            this.Quantity = quantity;
        }

        public void Update(Guid stockId, Guid batchId, Guid medicineId, StockMovementType type, int quantity)
        {
            this.StockId = stockId;
            this.BatchId = batchId;
            this.MedicineId = medicineId;
            this.Type = type;
            this.Quantity = quantity;

            UpdateTimestamps();
        }
    }
}
