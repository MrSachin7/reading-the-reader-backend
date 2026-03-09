namespace ReadingTheReader.Realtime.Persistence;

public sealed class ReadingTextStorageOptions
{
    public const string SectionName = "ReadingTextStorage";

    public string DirectoryPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "reading-texts");
}
