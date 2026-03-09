namespace ReadingTheReader.Realtime.Persistence;

internal sealed class StoredReadingTextMetadata
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public long CreatedAtUnixMs { get; init; }
}
