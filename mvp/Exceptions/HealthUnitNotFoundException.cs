namespace mvp.Exceptions
{
    public class HealthUnitNotFoundException : BaseException
    {
        public HealthUnitNotFoundException() : base("Unidade de Saúde não encontrado")
        {
        }
    }
}
