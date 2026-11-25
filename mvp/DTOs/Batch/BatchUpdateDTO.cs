namespace mvp.DTOs.Batch
{
    public class BatchUpdateDTO
    {
        public Guid MedicineId { get; set; }
        public string BatchNumber { get; set; } = null!;
        public DateTime FabricationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
