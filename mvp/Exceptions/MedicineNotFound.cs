namespace mvp.Exceptions
{
    public class MedicineNotFoundException : NotFoundException
    {
        public MedicineNotFoundException() : base("Medicamento não encontrado")
        {
        }
    }
}
