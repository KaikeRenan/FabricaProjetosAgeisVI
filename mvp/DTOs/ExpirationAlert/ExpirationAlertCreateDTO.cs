namespace mvp.DTOs.ExpirationAlert
{
    public class ExpirationAlertCreateDTO
    {
        public Guid MedicineId { get; set; }
        public Guid BatchId { get; set; }
        public DateTime AlertDate { get; set; }
    }
}
