namespace mvp.Entities
{
    public class Medicine : BaseEntity
    {
        public string Name { get; private set; }
        public string ActiveIngredient { get; private set; }
        public string Dosage { get; private set; }
        public string Unit { get; private set; }

        protected Medicine()
        {
            Name = null!;
            ActiveIngredient = null!;
            Dosage = null!;
            Unit = null!;
        }

        public Medicine(string name, string activeIngredient, string dosage, string unit)
        {
            this.Name = name;
            this.ActiveIngredient = activeIngredient;
            this.Dosage = dosage;
            this.Unit = unit;
        }

        public void Update(string name, string activeIngredient, string dosage, string unit)
        {
            this.Name = name;
            this.ActiveIngredient = activeIngredient;
            this.Dosage = dosage;
            this.Unit = unit;

            UpdateTimestamps();
        }
    }
}
