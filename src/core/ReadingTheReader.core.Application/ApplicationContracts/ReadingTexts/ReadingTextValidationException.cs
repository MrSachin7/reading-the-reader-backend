namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public sealed class ReadingTextValidationException : Exception
{
    public ReadingTextValidationException(string message) : base(message)
    {
    }
}
