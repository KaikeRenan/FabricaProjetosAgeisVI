namespace mvp.Exceptions
{
    /// <summary>Base das exceções que o DomainExceptionHandler traduz em HTTP 409.</summary>
    public abstract class ConflictException : BaseException
    {
        protected ConflictException(string message) : base(message) { }
    }
}
