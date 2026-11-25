using mvp.Enums;
using System.Text.Json.Serialization;

namespace mvp.DTOs.User
{
    public class UserResponseDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string? CPF { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }
    }
}
