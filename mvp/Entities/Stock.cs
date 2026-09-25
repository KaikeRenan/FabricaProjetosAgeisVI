namespace mvp.Entities
{
    public class Stock : BaseEntity
    {
        public Guid? PharmacyId { get; private set; }
        public Guid? HealthUnitId { get; private set; }
        public Guid? BatchId { get; private set; }
        public int Quantity { get; private set; }

        protected Stock()
        {
        }

        public Stock(Guid? pharmacyId, Guid? healthUnitId, Guid? batchId, int quantity)
        {
            this.PharmacyId = pharmacyId;
            this.HealthUnitId = healthUnitId;
            this.BatchId = batchId;
            this.Quantity = quantity;
        }

        public void Update(Guid? pharmacyId, Guid? healthUnitId, Guid? batchId, int quantity)
        {
            this.PharmacyId = pharmacyId;
            this.HealthUnitId = healthUnitId;
            this.BatchId = batchId;
            this.Quantity = quantity;
            UpdateTimestamps();
        }
    }
}
