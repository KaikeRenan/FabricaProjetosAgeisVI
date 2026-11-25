namespace mvp.Exceptions
{
    public class StockNotFoundException : NotFoundException
    {
        public StockNotFoundException() : base("Estoque não encontrado")
        {
        }
    }
}
