namespace mvp.Exceptions
{
    public class MedicineNotFoundException : Exception
    {
        public MedicineNotFoundException() : base("Medicamento não encontrado")
        {
        }
    }
}
