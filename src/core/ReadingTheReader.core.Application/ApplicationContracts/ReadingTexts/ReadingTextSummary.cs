namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public sealed class ReadingTextSummary
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public long CreatedAtUnixMs { get; init; }
}
