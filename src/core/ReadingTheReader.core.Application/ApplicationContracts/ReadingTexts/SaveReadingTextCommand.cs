namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public sealed class SaveReadingTextCommand
{
    public string Title { get; init; } = string.Empty;

    public string Markdown { get; init; } = string.Empty;
}
