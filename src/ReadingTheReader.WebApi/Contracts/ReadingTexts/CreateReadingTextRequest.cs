namespace ReadingTheReader.WebApi.Contracts.ReadingTexts;

public sealed class CreateReadingTextRequest
{
    public string Title { get; set; } = string.Empty;

    public string Markdown { get; set; } = string.Empty;
}
