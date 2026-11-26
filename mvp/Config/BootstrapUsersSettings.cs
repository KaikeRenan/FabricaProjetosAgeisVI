using mvp.Enums;

namespace mvp.Config
{
    public class BootstrapUsersSettings
    {
        public bool Enabled { get; set; }
        public List<BootstrapUser> Users { get; set; } = new();
    }

    public class BootstrapUser
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? CPF { get; set; }
        public UserRole Role { get; set; } = UserRole.Operator;
    }
}
