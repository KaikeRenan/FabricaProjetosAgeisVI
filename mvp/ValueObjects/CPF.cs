namespace mvp.ValueObjects
public class CPF
{
	public string value { get; set;  }

    public CPF(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException("CPF Invalido")

		if(value.length < 11)
			throw new ArgumentException("CPF deve ter 11 digitos")


        bool todosRepetidos = value.All(c => c == value[0]);

		if(todosRepetidos == true)
			throw new ArgumentException("CPF não pode ser somente um mesmo numero")

	}
}

