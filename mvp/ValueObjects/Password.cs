namespace mvp.ValueObjects
{
    public class Password
    {
        public string Value { get; private set; }

        public Password(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("Senha inválida");

            if (value.Length < 8)
                throw new ArgumentException("Senha deve possuir pelo menos 8 caracteres");

            this.Value = value.Trim();
        }
    }
}
