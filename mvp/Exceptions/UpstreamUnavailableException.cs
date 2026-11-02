namespace mvp.Exceptions
{
    /// <summary>Base das exceções por falha de um serviço dependente; traduzida em HTTP 503.</summary>
    public abstract class UpstreamUnavailableException : BaseException
    {
        protected UpstreamUnavailableException(string message) : base(message) { }
    }
}
