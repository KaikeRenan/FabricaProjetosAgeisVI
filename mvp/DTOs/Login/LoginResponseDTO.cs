namespace mvp.DTOs.Login
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = null!;

        public LoginResponseDTO(string token)
        {
            Token = token;
        }
    }
}
