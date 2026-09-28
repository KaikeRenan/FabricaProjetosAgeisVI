namespace mvp.DTOs.Medicine
{
    public class MedicineCreateDTO
    {
        public string Name { get; set; } = null!;
        public string ActiveIngredient { get; set; } = null!;
        public string Dosage { get; set; } = null!;
        public string Unit { get; set; } = null!;
    }
}
