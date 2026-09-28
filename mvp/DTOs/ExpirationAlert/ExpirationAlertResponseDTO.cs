namespace mvp.DTOs.ExpirationAlert
{
    public class ExpirationAlertResponseDTO
    {
        public Guid Id { get; set; }
        public Guid MedicineId { get; set; }
        public Guid BatchId { get; set; }
        public DateTime AlertDate { get; set; }
        public bool Resolved { get; set; }
    }
}
