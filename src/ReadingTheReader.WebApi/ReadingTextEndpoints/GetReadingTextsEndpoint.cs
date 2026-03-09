using FastEndpoints;
using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

namespace ReadingTheReader.WebApi.ReadingTextEndpoints;

public sealed class GetReadingTextsEndpoint : EndpointWithoutRequest<IReadOnlyCollection<ReadingTextSummary>>
{
    private readonly IReadingTextService _readingTextService;

    public GetReadingTextsEndpoint(IReadingTextService readingTextService)
    {
        _readingTextService = readingTextService;
    }

    public override void Configure()
    {
        Get("/reading-texts");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var items = await _readingTextService.ListAsync(ct);
            await Send.OkAsync(items, ct);
        }
        catch (IOException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { message = "Failed to load reading texts.", detail = ex.Message }, ct);
        }
    }
}
