namespace mvp.Entities
{
    public class Stock : BaseEntity
    {
        public Guid? PharmacyId { get; private set; }
        public Guid? HealthPostId { get; private set; }
        public string MedicineName { get; private set; }
        public float Dosage { get; private set; }
        public int Quantity { get; private set; }

        protected Stock()
        {
            MedicineName = null!;
        }

        public Stock(Guid? pharmacyId, Guid? healthPostId, string medicineName, float dosage, int quantity)
        {
            this.PharmacyId = pharmacyId;
            this.HealthPostId = healthPostId;
            this.MedicineName = medicineName;
            this.Dosage = dosage;
            this.Quantity = quantity;
        }

        public void Update(Guid? pharmacyId, Guid? healthPostId, string medicineName, float dosage, int quantity)
        {
            this.PharmacyId = pharmacyId;
            this.HealthPostId = healthPostId;
            this.MedicineName = medicineName;
            this.Dosage = dosage;
            this.Quantity = quantity;
            UpdateTimestamps();
        }
    }
}
