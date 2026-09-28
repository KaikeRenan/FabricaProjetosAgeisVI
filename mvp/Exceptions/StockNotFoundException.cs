namespace mvp.Exceptions
{
    public class StockNotFoundException : BaseException
    {
        public StockNotFoundException() : base("Estoque não encontrado")
        {
        }
    }
}
