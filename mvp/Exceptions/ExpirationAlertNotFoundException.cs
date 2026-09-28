namespace mvp.Exceptions
{
    public class ExpirationAlertNotFoundException : BaseException
    {
        public ExpirationAlertNotFoundException() : base("Alerta de Validade não encontrado")
        {
        }
    }
}
