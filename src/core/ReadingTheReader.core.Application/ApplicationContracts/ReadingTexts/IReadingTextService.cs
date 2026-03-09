namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

public interface IReadingTextService
{
    ValueTask<ReadingTextSummary> SaveAsync(SaveReadingTextCommand command, CancellationToken ct = default);

    ValueTask<IReadOnlyCollection<ReadingTextSummary>> ListAsync(CancellationToken ct = default);

    ValueTask<ReadingTextDetail> GetByIdAsync(string id, CancellationToken ct = default);
}
