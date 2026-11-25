namespace mvp.Exceptions
{
    public class StockMovementNotFoundException : NotFoundException
    {
        public StockMovementNotFoundException() : base("Movimentação no estoque não encontrado")
        {
        }
    }
}
