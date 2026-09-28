namespace mvp.Entities
{
    public class ExpirationAlert : BaseEntity
    {
        public Guid MedicineId { get; set; }
        public Guid BatchId { get; private set; }
        public DateTime AlertDate { get; private set; }
        public bool Resolved { get; private set; }

        protected ExpirationAlert()
        {
        }

        public ExpirationAlert(Guid medicineId, Guid batchId, DateTime alertDate)
        {
            this.MedicineId = medicineId;
            this.BatchId = batchId;
            this.AlertDate = alertDate;
            this.Resolved = false;
        }

        public void Update(Guid medicineId, Guid batchId, DateTime alertDate, bool resolved)
        {
            this.MedicineId = medicineId;
            this.BatchId = batchId;
            this.AlertDate = alertDate;
            this.Resolved = resolved;

            UpdateTimestamps();
        }
    }
}
