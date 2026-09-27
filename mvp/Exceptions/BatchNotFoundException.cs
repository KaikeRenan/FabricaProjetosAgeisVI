namespace mvp.Exceptions
{
    public class BatchNotFoundException : BaseException
    {
        public BatchNotFoundException() : base("Lote não encontrado")
        {
        }
    }
}
