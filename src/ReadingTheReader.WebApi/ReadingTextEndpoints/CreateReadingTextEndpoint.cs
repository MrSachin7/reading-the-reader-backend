using FastEndpoints;
using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;
using ReadingTheReader.WebApi.Contracts.ReadingTexts;

namespace ReadingTheReader.WebApi.ReadingTextEndpoints;

public sealed class CreateReadingTextEndpoint : Endpoint<CreateReadingTextRequest, ReadingTextSummary>
{
    private readonly IReadingTextService _readingTextService;

    public CreateReadingTextEndpoint(IReadingTextService readingTextService)
    {
        _readingTextService = readingTextService;
    }

    public override void Configure()
    {
        Post("/reading-texts");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateReadingTextRequest req, CancellationToken ct)
    {
        try
        {
            var savedText = await _readingTextService.SaveAsync(new SaveReadingTextCommand
            {
                Title = req.Title,
                Markdown = req.Markdown
            }, ct);

            await Send.CreatedAtAsync<GetReadingTextByIdEndpoint>(
                new { id = savedText.Id },
                savedText,
                cancellation: ct);
        }
        catch (ReadingTextValidationException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { message = ex.Message }, ct);
        }
        catch (IOException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { message = "Failed to save reading text.", detail = ex.Message }, ct);
        }
    }
}
