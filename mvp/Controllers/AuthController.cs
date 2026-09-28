using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.Login;
using mvp.Interfaces.Services;
using mvp.Services;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequestDTO request)
        {
            var response = _authService.LoginAsync(request);

            return Ok(response);
        }
    }
}
