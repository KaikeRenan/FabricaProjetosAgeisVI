namespace mvp.Entities
{
    public class StockMovement : BaseEntity
    {
        public Guid? StockId { get; private set; }
        public string Type { get; private set; }
        public int Quantity { get; private set; }

        protected StockMovement()
        {
            Type = null!;
        }

        public StockMovement(Guid? stockId, string type, int quantity)
        {
            this.StockId = stockId;
            this.Type = type;
            this.Quantity = quantity;
        }

        public void Update(Guid? stockId, string type, int quantity)
        {
            this.StockId = stockId;
            this.Type = type;
            this.Quantity = quantity;

            UpdateTimestamps();
        }
    }
}
