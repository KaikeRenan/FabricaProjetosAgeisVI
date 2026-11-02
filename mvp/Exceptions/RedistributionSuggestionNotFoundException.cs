namespace mvp.Exceptions
{
    public class RedistributionSuggestionNotFoundException : NotFoundException
    {
        public RedistributionSuggestionNotFoundException() : base("Sugestão de remanejamento não encontrada")
        {
        }
    }
}
