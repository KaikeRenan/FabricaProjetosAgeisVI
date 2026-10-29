namespace mvp.DTOs.User
{
    public class UserResponseDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Cpf { get; set; } = null!;
    }
}
