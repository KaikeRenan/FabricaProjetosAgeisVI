namespace mvp.Exceptions
{
    public class StockMovementNotFoundException : BaseException
    {
        public StockMovementNotFoundException() : base("Movimentação no estoque não encontrado")
        {
        }
    }
}
