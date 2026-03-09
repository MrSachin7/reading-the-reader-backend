using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;
using ReadingTheReader.Realtime.Persistence;

namespace ReadingTheReader.Realtime.Persistence.Tests;

public sealed class FileReadingTextStoreAdapterTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "reading-text-store-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_WritesMarkdownAndMetadata_AndReturnsSummary()
    {
        var sut = new FileReadingTextStoreAdapter(_tempDirectory);

        var result = await sut.SaveAsync(new SaveReadingTextCommand
        {
            Title = "My Custom Text",
            Markdown = "# Hello"
        });

        Assert.NotEmpty(result.Id);
        Assert.Equal("My Custom Text", result.Title);
        Assert.Matches("^my-custom-text-[a-f0-9]{8}\\.md$", result.FileName);
        Assert.True(result.CreatedAtUnixMs > 0);
        Assert.True(File.Exists(Path.Combine(_tempDirectory, result.FileName)));
        Assert.True(File.Exists(Path.Combine(_tempDirectory, Path.ChangeExtension(result.FileName, ".json"))));
    }

    [Fact]
    public async Task SaveAsync_UsesUniqueFileNames_ForDuplicateTitles()
    {
        var sut = new FileReadingTextStoreAdapter(_tempDirectory);

        var first = await sut.SaveAsync(new SaveReadingTextCommand
        {
            Title = "Duplicate Title",
            Markdown = "First"
        });

        var second = await sut.SaveAsync(new SaveReadingTextCommand
        {
            Title = "Duplicate Title",
            Markdown = "Second"
        });

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.FileName, second.FileName);
    }

    [Fact]
    public async Task ListAndGetByIdAsync_ReturnSavedReadingTexts()
    {
        var sut = new FileReadingTextStoreAdapter(_tempDirectory);

        var first = await sut.SaveAsync(new SaveReadingTextCommand
        {
            Title = "First Text",
            Markdown = "Alpha"
        });

        await Task.Delay(10);

        var second = await sut.SaveAsync(new SaveReadingTextCommand
        {
            Title = "Second/Text",
            Markdown = "Bravo"
        });

        var list = await sut.ListAsync();
        var detail = await sut.GetByIdAsync(second.Id);

        Assert.Equal(2, list.Count);
        Assert.Equal(second.Id, list.First().Id);
        Assert.NotNull(detail);
        Assert.Equal("Second/Text", detail.Title);
        Assert.Equal("Bravo", detail.Markdown);
        Assert.Matches("^second-text-[a-f0-9]{8}\\.md$", detail.FileName);
        Assert.Contains(list, item => item.Id == first.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
