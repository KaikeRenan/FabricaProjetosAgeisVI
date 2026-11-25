namespace mvp.Exceptions
{
    public class BatchNotFoundException : NotFoundException
    {
        public BatchNotFoundException() : base("Lote não encontrado")
        {
        }
    }
}
