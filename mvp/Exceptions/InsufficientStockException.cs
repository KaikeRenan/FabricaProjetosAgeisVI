namespace mvp.Exceptions
{
    public class InsufficientStockException : ConflictException
    {
        public InsufficientStockException() : base("Estoque de origem insuficiente para executar o remanejamento")
        {
        }
    }
}
