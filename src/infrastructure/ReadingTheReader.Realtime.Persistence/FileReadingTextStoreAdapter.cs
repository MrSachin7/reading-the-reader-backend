using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;
using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.Realtime.Persistence;

public sealed class FileReadingTextStoreAdapter : IReadingTextStoreAdapter
{
    private static readonly Regex InvalidFileNameCharactersRegex = new(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Compiled);

    private readonly string _directoryPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public FileReadingTextStoreAdapter(string directoryPath)
    {
        _directoryPath = directoryPath;
    }

    public async ValueTask<ReadingTextSummary> SaveAsync(SaveReadingTextCommand command, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_directoryPath);

        var id = Guid.NewGuid().ToString("N");
        var createdAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fileName = BuildUniqueFileName(command.Title, id);
        var markdownPath = Path.Combine(_directoryPath, fileName);
        var metadataPath = GetMetadataPath(markdownPath);

        var metadata = new StoredReadingTextMetadata
        {
            Id = id,
            Title = command.Title.Trim(),
            FileName = fileName,
            CreatedAtUnixMs = createdAtUnixMs
        };

        await File.WriteAllTextAsync(markdownPath, command.Markdown, Encoding.UTF8, ct);
        await WriteMetadataAsync(metadataPath, metadata, ct);

        return new ReadingTextSummary
        {
            Id = metadata.Id,
            Title = metadata.Title,
            FileName = metadata.FileName,
            CreatedAtUnixMs = metadata.CreatedAtUnixMs
        };
    }

    public async ValueTask<IReadOnlyCollection<ReadingTextSummary>> ListAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_directoryPath))
        {
            return Array.Empty<ReadingTextSummary>();
        }

        var metadataFiles = Directory.GetFiles(_directoryPath, "*.json", SearchOption.TopDirectoryOnly);
        var items = new List<ReadingTextSummary>(metadataFiles.Length);

        foreach (var metadataFile in metadataFiles)
        {
            var metadata = await ReadMetadataAsync(metadataFile, ct);
            if (metadata is null)
            {
                continue;
            }

            var markdownPath = Path.Combine(_directoryPath, metadata.FileName);
            if (!File.Exists(markdownPath))
            {
                continue;
            }

            items.Add(new ReadingTextSummary
            {
                Id = metadata.Id,
                Title = metadata.Title,
                FileName = metadata.FileName,
                CreatedAtUnixMs = metadata.CreatedAtUnixMs
            });
        }

        return items
            .OrderByDescending(item => item.CreatedAtUnixMs)
            .ToArray();
    }

    public async ValueTask<ReadingTextDetail?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!Directory.Exists(_directoryPath))
        {
            return null;
        }

        var metadataFiles = Directory.GetFiles(_directoryPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var metadataFile in metadataFiles)
        {
            var metadata = await ReadMetadataAsync(metadataFile, ct);
            if (metadata is null || !string.Equals(metadata.Id, id, StringComparison.Ordinal))
            {
                continue;
            }

            var markdownPath = Path.Combine(_directoryPath, metadata.FileName);
            if (!File.Exists(markdownPath))
            {
                return null;
            }

            return new ReadingTextDetail
            {
                Id = metadata.Id,
                Title = metadata.Title,
                FileName = metadata.FileName,
                CreatedAtUnixMs = metadata.CreatedAtUnixMs,
                Markdown = await File.ReadAllTextAsync(markdownPath, ct)
            };
        }

        return null;
    }

    private string BuildUniqueFileName(string title, string id)
    {
        var slug = SanitizeFileNameSegment(title);
        return $"{slug}-{id[..8]}.md";
    }

    internal static string SanitizeFileNameSegment(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        normalized = InvalidFileNameCharactersRegex.Replace(normalized, "-");
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, "-{2,}", "-");
        normalized = normalized.Trim('-', '.');

        return string.IsNullOrWhiteSpace(normalized) ? "reading-text" : normalized;
    }

    private async ValueTask WriteMetadataAsync(string metadataPath, StoredReadingTextMetadata metadata, CancellationToken ct)
    {
        var tempPath = $"{metadataPath}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, metadata, _jsonOptions, ct);
        }

        File.Move(tempPath, metadataPath, overwrite: true);
    }

    private async ValueTask<StoredReadingTextMetadata?> ReadMetadataAsync(string metadataPath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(metadataPath);
        return await JsonSerializer.DeserializeAsync<StoredReadingTextMetadata>(stream, _jsonOptions, ct);
    }

    private static string GetMetadataPath(string markdownPath)
    {
        return Path.ChangeExtension(markdownPath, ".json");
    }
}
