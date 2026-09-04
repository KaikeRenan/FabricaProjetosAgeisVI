namespace mvp.ValueObjects
{
    public class Name
    {
        public string Value { get; private set; }

        public Name(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nome inválido");

            this.Value = value.Trim();
        }
    }
}
