namespace mvp.ValueObjects
{
    public class Email
    {
        public string Value { get; private set; }

        public Email(string value) 
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Email inválido");

            if (!value.Contains("@"))
                throw new ArgumentException("Email com formato inválido");

            this.Value = value.Trim();
        }
    }
}
