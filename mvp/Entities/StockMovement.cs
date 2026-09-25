using mvp.Enums;

namespace mvp.Entities
{
    public class StockMovement : BaseEntity
    {
        public Guid? StockId { get; private set; }
        public StockMovementType Type { get; private set; }
        public int Quantity { get; private set; }

        protected StockMovement()
        {
        }

        public StockMovement(Guid? stockId, StockMovementType type, int quantity)
        {
            this.StockId = stockId;
            this.Type = type;
            this.Quantity = quantity;
        }

        public void Update(Guid? stockId, StockMovementType type, int quantity)
        {
            this.StockId = stockId;
            this.Type = type;
            this.Quantity = quantity;

            UpdateTimestamps();
        }
    }
}
