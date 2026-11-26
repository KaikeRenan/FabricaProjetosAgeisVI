namespace mvp.Exceptions
{
    public class PharmacyNotFoundException : NotFoundException
    {
        public PharmacyNotFoundException() : base("Farmácia não encontrada")
        {
        }
    }
}
