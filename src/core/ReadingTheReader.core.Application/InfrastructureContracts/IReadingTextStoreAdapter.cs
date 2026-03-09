using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

namespace ReadingTheReader.core.Application.InfrastructureContracts;

public interface IReadingTextStoreAdapter
{
    ValueTask<ReadingTextSummary> SaveAsync(SaveReadingTextCommand command, CancellationToken ct = default);

    ValueTask<IReadOnlyCollection<ReadingTextSummary>> ListAsync(CancellationToken ct = default);

    ValueTask<ReadingTextDetail?> GetByIdAsync(string id, CancellationToken ct = default);
}
