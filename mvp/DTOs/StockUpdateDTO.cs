namespace mvp.DTOs
{
    public class StockUpdateDTO
    {
        public Guid? PharmacyId { get; set; }
        public Guid? HealthUnitId { get; set; }
        public Guid? BatchId { get; set; }
        public int Quantity { get; set; }
    }
}
