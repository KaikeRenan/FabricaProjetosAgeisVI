namespace mvp.Exceptions
{
    public class StockNotFoundException : Exception
    {
        public StockNotFoundException() : base("Estoque não encontrado")
        {
        }
    }
}
