namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public sealed class ReadingTextDetail : ReadingTextSummary
{
    public string Markdown { get; init; } = string.Empty;
}
