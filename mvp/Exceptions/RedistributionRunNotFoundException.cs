namespace mvp.Exceptions
{
    public class RedistributionRunNotFoundException : NotFoundException
    {
        public RedistributionRunNotFoundException() : base("Execução de remanejamento não encontrada")
        {
        }
    }
}
