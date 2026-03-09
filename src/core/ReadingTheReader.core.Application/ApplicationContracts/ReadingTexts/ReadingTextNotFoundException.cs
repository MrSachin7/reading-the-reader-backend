namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public sealed class ReadingTextNotFoundException : Exception
{
    public ReadingTextNotFoundException(string id) : base($"Reading text '{id}' was not found.")
    {
    }
}
