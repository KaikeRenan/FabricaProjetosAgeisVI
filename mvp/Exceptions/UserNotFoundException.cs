namespace mvp.Exceptions
{
    public class UserNotFoundException : NotFoundException
    {
        public UserNotFoundException() : base("Usuário não encontrado")
        {
        }
    }
}
