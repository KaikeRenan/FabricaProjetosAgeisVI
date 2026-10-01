namespace mvp.ValueObjects
{
    public class Email
    {
        public string Value { get; private set; }

        protected Email()
        {
            Value = null!;
        }

        public Email(string value) 
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email inválido");

            value = value.Trim().ToLowerInvariant();

            if (!value.Contains("@"))
                throw new ArgumentException("Email com formato inválido");

            Value = value;
        }
    }
}
