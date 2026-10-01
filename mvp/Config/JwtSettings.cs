namespace mvp.Config
{
    public class JwtSettings
    {
        public string PrivateKey { get; set; } = null!;
        public int ExpirationHours { get; set; }
    }
}
