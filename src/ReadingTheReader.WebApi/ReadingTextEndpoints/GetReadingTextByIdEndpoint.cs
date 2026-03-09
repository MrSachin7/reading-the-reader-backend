using FastEndpoints;
using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;

namespace ReadingTheReader.WebApi.ReadingTextEndpoints;

public sealed class GetReadingTextByIdEndpoint : EndpointWithoutRequest<ReadingTextDetail>
{
    private readonly IReadingTextService _readingTextService;

    public GetReadingTextByIdEndpoint(IReadingTextService readingTextService)
    {
        _readingTextService = readingTextService;
    }

    public override void Configure()
    {
        Get("/reading-texts/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id");

        try
        {
            var item = await _readingTextService.GetByIdAsync(id ?? string.Empty, ct);
            await Send.OkAsync(item, ct);
        }
        catch (ReadingTextValidationException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsJsonAsync(new { message = ex.Message }, ct);
        }
        catch (ReadingTextNotFoundException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new { message = ex.Message }, ct);
        }
        catch (IOException ex)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(new { message = "Failed to load reading text.", detail = ex.Message }, ct);
        }
    }
}
