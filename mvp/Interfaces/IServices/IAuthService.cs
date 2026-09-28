using mvp.DTOs.Login;

namespace mvp.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);
    }
}
