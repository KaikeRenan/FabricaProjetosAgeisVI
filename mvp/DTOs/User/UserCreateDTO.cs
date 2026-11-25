using mvp.Enums;
using System.Text.Json.Serialization;

namespace mvp.DTOs.User
{
    public class UserCreateDTO
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? CPF { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole? Role { get; set; }

    }
}
