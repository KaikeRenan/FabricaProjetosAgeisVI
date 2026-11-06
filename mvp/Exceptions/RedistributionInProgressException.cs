namespace mvp.Exceptions
{
    public class RedistributionInProgressException : ConflictException
    {
        public RedistributionInProgressException() : base("Já existe uma execução de remanejamento em andamento")
        {
        }
    }
}
