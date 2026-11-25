namespace mvp.ValueObjects
{
    public class CPF
    {
        public string Value { get; private set; }

        public CPF(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("CPF inválido");

            var digits = new string(value.Where(char.IsDigit).ToArray());

            if (digits.Length != 11)
                throw new ArgumentException("CPF deve conter 11 dígitos");

            if (digits.Distinct().Count() == 1)
                throw new ArgumentException("CPF inválido");

            if (!IsValid(digits))
                throw new ArgumentException("CPF inválido");

            Value = digits;
        }

        private static bool IsValid(string digits)
            => CalculateDigit(digits, 9) == digits[9] - '0'
               && CalculateDigit(digits, 10) == digits[10] - '0';

        private static int CalculateDigit(string digits, int length)
        {
            var sum = 0;

            for (var i = 0; i < length; i++)
                sum += (digits[i] - '0') * (length + 1 - i);

            var rest = sum % 11;

            return rest < 2 ? 0 : 11 - rest;
        }

        public override string ToString() => Value;
    }
}
