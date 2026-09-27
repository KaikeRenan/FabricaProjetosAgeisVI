namespace mvp.DTOs.Batch
{
    public class BatchUpdateDTO
    {
        public Guid MedicineId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime FabricationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
