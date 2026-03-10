using System.Text.RegularExpressions;
using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.core.Application.ApplicationContracts.ReadingMaterialSetups;

public sealed class ReadingMaterialSetupService : IReadingMaterialSetupService
{
    internal const int MinFontSizePx = 12;
    internal const int MaxFontSizePx = 48;
    internal const int MinLineWidthPx = 320;
    internal const int MaxLineWidthPx = 1600;
    internal const double MinLineHeight = 1.0;
    internal const double MaxLineHeight = 3.0;
    internal const double MinLetterSpacingEm = -0.05;
    internal const double MaxLetterSpacingEm = 0.2;

    private static readonly Regex ValidFontFamilyRegex = new(@"^[A-Za-z0-9][A-Za-z0-9 _-]{0,63}$", RegexOptions.Compiled);
    private readonly IReadingMaterialSetupStoreAdapter _readingMaterialSetupStoreAdapter;

    public ReadingMaterialSetupService(IReadingMaterialSetupStoreAdapter readingMaterialSetupStoreAdapter)
    {
        _readingMaterialSetupStoreAdapter = readingMaterialSetupStoreAdapter;
    }

    public async ValueTask<ReadingMaterialSetup> SaveAsync(SaveReadingMaterialSetupCommand command, CancellationToken ct = default)
    {
        Validate(command.Title, command.Markdown, command.FontFamily, command.FontSizePx, command.LineWidthPx, command.LineHeight, command.LetterSpacingEm);
        return await _readingMaterialSetupStoreAdapter.SaveAsync(command, ct);
    }

    public ValueTask<IReadOnlyCollection<ReadingMaterialSetup>> ListAsync(CancellationToken ct = default) => _readingMaterialSetupStoreAdapter.ListAsync(ct);

    public async ValueTask<ReadingMaterialSetup> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ReadingMaterialSetupValidationException("id is required.");
        }

        var item = await _readingMaterialSetupStoreAdapter.GetByIdAsync(id, ct);
        if (item is null)
        {
            throw new ReadingMaterialSetupNotFoundException(id);
        }

        return item;
    }

    public async ValueTask<ReadingMaterialSetup> UpdateAsync(UpdateReadingMaterialSetupCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            throw new ReadingMaterialSetupValidationException("id is required.");
        }

        Validate(command.Title, command.Markdown, command.FontFamily, command.FontSizePx, command.LineWidthPx, command.LineHeight, command.LetterSpacingEm);

        var updated = await _readingMaterialSetupStoreAdapter.UpdateAsync(command, ct);
        if (updated is null)
        {
            throw new ReadingMaterialSetupNotFoundException(command.Id);
        }

        return updated;
    }

    private static void Validate(string title, string markdown, string fontFamily, int fontSizePx, int lineWidthPx, double lineHeight, double letterSpacingEm)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ReadingMaterialSetupValidationException("title is required.");
        }

        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new ReadingMaterialSetupValidationException("markdown is required.");
        }

        if (string.IsNullOrWhiteSpace(fontFamily) || !ValidFontFamilyRegex.IsMatch(fontFamily.Trim()))
        {
            throw new ReadingMaterialSetupValidationException("fontFamily is invalid.");
        }

        if (fontSizePx < MinFontSizePx || fontSizePx > MaxFontSizePx)
        {
            throw new ReadingMaterialSetupValidationException($"fontSizePx must be between {MinFontSizePx} and {MaxFontSizePx}.");
        }

        if (lineWidthPx < MinLineWidthPx || lineWidthPx > MaxLineWidthPx)
        {
            throw new ReadingMaterialSetupValidationException($"lineWidthPx must be between {MinLineWidthPx} and {MaxLineWidthPx}.");
        }

        if (lineHeight < MinLineHeight || lineHeight > MaxLineHeight)
        {
            throw new ReadingMaterialSetupValidationException($"lineHeight must be between {MinLineHeight} and {MaxLineHeight}.");
        }

        if (letterSpacingEm < MinLetterSpacingEm || letterSpacingEm > MaxLetterSpacingEm)
        {
            throw new ReadingMaterialSetupValidationException($"letterSpacingEm must be between {MinLetterSpacingEm} and {MaxLetterSpacingEm}.");
        }
    }
}
