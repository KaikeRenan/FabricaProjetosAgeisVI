namespace mvp.Exceptions
{
    public class EmailAlreadyRegisteredException : ConflictException
    {
        public EmailAlreadyRegisteredException() : base("E-mail já cadastrado.") { }
    }
}
