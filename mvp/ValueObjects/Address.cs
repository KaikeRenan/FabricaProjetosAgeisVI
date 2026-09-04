namespace mvp.ValueObjects
{
    public class Address
    {
        public string Street { get; private set; }
        public string Number { get; private set; }
        public string Neighborhood { get; private set; }
        public string ZipCode { get; private set; }
        public string Zone { get; private set; }

        protected Address()
        {
            Street = null!;
            Number = null!;
            Neighborhood = null!;
            ZipCode = null!;
            Zone = null!;
        }

        public Address(string street, string number, string neighborhood, string zipCode, string zone)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Rua é obrigatória");

            if (string.IsNullOrWhiteSpace(zipCode))
                throw new ArgumentException("CEP é obrigatório");

            Street = street.Trim();
            Number = number?.Trim() ?? string.Empty;
            Neighborhood = neighborhood?.Trim() ?? string.Empty;
            ZipCode = zipCode.Trim();
            Zone = zone?.Trim() ?? string.Empty;
        }
    }
}
