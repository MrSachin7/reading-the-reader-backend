using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public sealed class ReadingTextService : IReadingTextService
{
    private readonly IReadingTextStoreAdapter _readingTextStoreAdapter;

    public ReadingTextService(IReadingTextStoreAdapter readingTextStoreAdapter)
    {
        _readingTextStoreAdapter = readingTextStoreAdapter;
    }

    public async ValueTask<ReadingTextSummary> SaveAsync(SaveReadingTextCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ReadingTextValidationException("title is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Markdown))
        {
            throw new ReadingTextValidationException("markdown is required.");
        }

        return await _readingTextStoreAdapter.SaveAsync(command, ct);
    }

    public ValueTask<IReadOnlyCollection<ReadingTextSummary>> ListAsync(CancellationToken ct = default)
    {
        return _readingTextStoreAdapter.ListAsync(ct);
    }

    public async ValueTask<ReadingTextDetail> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ReadingTextValidationException("id is required.");
        }

        var readingText = await _readingTextStoreAdapter.GetByIdAsync(id, ct);
        if (readingText is null)
        {
            throw new ReadingTextNotFoundException(id);
        }

        return readingText;
    }
}
