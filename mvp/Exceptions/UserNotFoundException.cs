namespace mvp.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException() : base("Usuário não encontrado")
        {
        }
    }
}
