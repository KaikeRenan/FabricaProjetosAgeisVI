namespace mvp.Exceptions
{
    public class HealthPostNotFoundException : Exception
    {
        public HealthPostNotFoundException() : base("Posto de saúde não encontrado")
        {
        }
    }
}
