namespace mvp.DTOs
{
    public class MedicineCreateDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ActiveIngredient { get; private set; } = null!;
        public string Dosage { get; private set; } = null!;
        public string Unit { get; private set; } = null!;
    }
}


