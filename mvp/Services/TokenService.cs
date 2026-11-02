using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using mvp.Config;
using mvp.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace mvp.Services
{
    public class TokenService
    {
        private readonly JwtSettings _jwtSettings;

        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string Generate(User user)
        {
            // Create Instance of JwtSecurityTokenHandler
            var handler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_jwtSettings.PrivateKey);

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = GenerateClaims(user),

                SigningCredentials = credentials,

                Expires = DateTime.UtcNow.AddHours(ExpirationHours)
            };

            // Generate Token
            var token = handler.CreateToken(tokenDescriptor);

            // Generate Token String
            return handler.WriteToken(token);
        }

        // Aceita a chave antiga (com erro de digitação) para não quebrar appsettings já existentes; padrão: 8h.
#pragma warning disable CS0618
        private int ExpirationHours =>
            _jwtSettings.ExpirationHours > 0 ? _jwtSettings.ExpirationHours
            : _jwtSettings.ExperationHours > 0 ? _jwtSettings.ExperationHours
            : 8;
#pragma warning restore CS0618

        private static ClaimsIdentity GenerateClaims(User user)
        {
            var ci = new ClaimsIdentity();

            ci.AddClaim(new Claim(ClaimTypes.Name, user.Email.Value));

            //foreach (var role in user.Roles)
            //    ci.AddClaim(new Claim(Claim.Role, role));

            return ci;
        }
    }
}
