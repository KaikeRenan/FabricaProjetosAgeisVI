using mvp.DTOs.Login;
using mvp.Interfaces.IRepositories;
using mvp.Interfaces.Services;

namespace mvp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly TokenService _tokenService;

        public AuthService(
            IUserRepository userRepository,
            TokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user is null)
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

            if (!user.Password.Verify(request.Password))
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

            var token = _tokenService.Generate(user);

            return new LoginResponseDTO(token);
        }
    }
}
