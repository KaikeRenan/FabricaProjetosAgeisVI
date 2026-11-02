namespace mvp.Exceptions
{
    public class InvalidSuggestionStateException : ConflictException
    {
        public InvalidSuggestionStateException() : base("A sugestão não está pendente (já decidida ou substituída)")
        {
        }
    }
}
