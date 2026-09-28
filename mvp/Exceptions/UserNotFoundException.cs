namespace mvp.Exceptions
{
    public class UserNotFoundException : BaseException
    {
        public UserNotFoundException() : base("Usuário não encontrado")
        {
        }
    }
}
