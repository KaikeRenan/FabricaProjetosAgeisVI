namespace mvp.Exceptions
{
    public class MlServiceUnavailableException : UpstreamUnavailableException
    {
        public MlServiceUnavailableException(string message) : base(message)
        {
        }
    }
}
