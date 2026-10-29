namespace mvp.DTOs.User
{
    public class UserCreateDTO
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Cpf { get; set; } = null!;
    }
}
