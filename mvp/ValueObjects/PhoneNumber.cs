namespace mvp.ValueObjects
{
    public class PhoneNumber
    {
        public string Value { get; private set; }

        public PhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Telefone inválido");

            this.Value = value.Trim();
        }
    }
}
