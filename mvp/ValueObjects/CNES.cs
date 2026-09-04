namespace mvp.ValueObjects
{
    public class CNES
    {
        public string Value { get; private set; }

        public CNES(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("CNES inválido");

            this.Value = value.Trim();
        }
    }
}
