namespace mvp.Entities
{
    public class Batch : BaseEntity
    {
        public Guid? MedicineId { get; private set; }
        public string BatchNumber { get; private set; }
        public DateTime FabricationDate { get; private set; }
        public DateTime ExpirationDate { get; private set; }

        protected Batch()
        {
            BatchNumber = null!;
        }

        public Batch(Guid? medicineId, string batchNumber, DateTime fabricationDate, DateTime expirationDate)
        {
            this.MedicineId = medicineId;
            this.BatchNumber = batchNumber;
            this.FabricationDate = fabricationDate;
            this.ExpirationDate = expirationDate;
        }

        public void Update(Guid? medicineId, string batchNumber, DateTime fabricationDate, DateTime expirationDate)
        {
            this.MedicineId = medicineId;
            this.BatchNumber = batchNumber;
            this.FabricationDate = fabricationDate;
            this.ExpirationDate = expirationDate;

            UpdateTimestamps();
        }
    }
}
