namespace mvp.Exceptions
{
    public class CPFAlreadyRegisteredException : ConflictException
    {
        public CPFAlreadyRegisteredException() : base("CPF já cadastrado.") { }
    }
}
