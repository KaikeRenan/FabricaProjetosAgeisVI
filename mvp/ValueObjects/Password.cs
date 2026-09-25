namespace mvp.ValueObjects
{
    public class Password
    {
        public string Value { get; private set; }

        protected Password()
        {
            Value = null!;
        }

        public Password(string value)
        {
            Validate(value);

            Value = BCrypt.Net.BCrypt.HashPassword(value);
        }

        private Password(string value, bool isHash)
        {
            Value = value;
        }

        public static Password FromHash(string hash)
        {
            return new Password(hash, true);
        }

        public bool Verify(string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, Value);
        }

        private static void Validate(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value), "Senha inválida");

            if (value.Length < 8)
                throw new ArgumentException("Senha deve possuir pelo menos 8 caracteres");
        }
    }
}
