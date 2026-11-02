namespace mvp.Exceptions
{
    /// <summary>Base das exceções que o DomainExceptionHandler traduz em HTTP 404.</summary>
    public abstract class NotFoundException : BaseException
    {
        protected NotFoundException(string message) : base(message) { }
    }
}
