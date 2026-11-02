namespace mvp.Config
{
    public class JwtSettings
    {
        public string PrivateKey { get; set; } = null!;
        public int ExpirationHours { get; set; }

        [Obsolete("Nome com erro de digitação; use ExpirationHours (chave 'Jwt:ExpirationHours').")]
        public int ExperationHours { get; set; }
    }
}
