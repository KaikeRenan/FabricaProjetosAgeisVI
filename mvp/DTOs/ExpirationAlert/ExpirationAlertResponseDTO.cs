namespace mvp.DTOs.ExpirationAlert
{
    public class ExpirationAlertResponseDTO
    {
        public Guid Id { get; set; }
        public Guid MedicineId { get; set; }
        public string MedicineName { get; set; } = null!;
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = null!;
        public int Quantity { get; set; }                     
        public DateTime ExpirationDate { get; set; }          
        public int DaysToExpiry { get; set; }
        public DateTime AlertDate { get; set; }
        public bool Resolved { get; set; }
    }
}
