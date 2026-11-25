namespace mvp.Exceptions
{
    public class ExpirationAlertNotFoundException : NotFoundException
    {
        public ExpirationAlertNotFoundException() : base("Alerta de Validade não encontrado")
        {
        }
    }
}
