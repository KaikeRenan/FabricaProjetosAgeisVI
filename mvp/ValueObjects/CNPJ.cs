namespace mvp.ValueObjects
{
    public class CNPJ
    {
        public string Value { get; private set; }

        public CNPJ(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("CNPJ inválido");

            this.Value = value.Trim();
        }
    }
}
