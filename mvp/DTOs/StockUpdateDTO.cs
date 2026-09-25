namespace mvp.DTOs
{
    public class StockUpdateDTO
    {
        public Guid? PharmacyId { get; set; }
        public Guid? HealthUnitId { get; set; }
        public string MedicineName { get; set; } = null!;
        public float Dosage { get; set; }
        public int Quantity { get; set; }
    }
}
