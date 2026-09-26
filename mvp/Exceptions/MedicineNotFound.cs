namespace mvp.Exceptions
{
    public class MedicineNotFoundException : BaseException
    {
        public MedicineNotFoundException() : base("Medicamento não encontrado")
        {
        }
    }
}
