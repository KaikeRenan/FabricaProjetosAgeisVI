using mvp.Enums;
using System.Data;
using System.Text.Json.Serialization;

namespace mvp.DTOs.Login
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = null!;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }

        public LoginResponseDTO(string token, UserRole role)
        {
            Token = token;
            Role = role;
        }
    }
}
