namespace mvp.Entities
{
    public class ExpirationAlert : BaseEntity
    {
        public Guid? BatchId { get; private set; }
        public DateTime AlertDate { get; private set; }
        public bool Resolved { get; private set; }

        protected ExpirationAlert()
        {
        }

        public ExpirationAlert(Guid? batchId, DateTime alertDate)
        {
            this.BatchId = batchId;
            this.AlertDate = alertDate;
            this.Resolved = false;
        }

        public void Update(Guid? batchId, DateTime alertDate)
        {
            this.BatchId = batchId;
            this.AlertDate = alertDate;

            UpdateTimestamps();
        }

        public void Resolve()
        {
            this.Resolved = true;
            UpdateTimestamps();
        }
    }
}
