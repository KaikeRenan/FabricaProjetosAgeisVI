namespace mvp.Exceptions
{
    public class StockMovementNotFoundException : Exception
    {
        public StockMovementNotFoundException() : base("Movimentação no estoque não encontrado")
        {
        }
    }
}
