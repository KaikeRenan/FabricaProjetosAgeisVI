namespace mvp.Exceptions
{
    public class HealthUnitNotFoundException : NotFoundException
    {
        public HealthUnitNotFoundException() : base("Unidade de Saúde não encontrado")
        {
        }
    }
}
