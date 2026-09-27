namespace mvp.DTOs.Batch
{
    public class BatchResponseDTO
    {
        public Guid Id { get; set; }
        public Guid MedicineId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime FabricationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
