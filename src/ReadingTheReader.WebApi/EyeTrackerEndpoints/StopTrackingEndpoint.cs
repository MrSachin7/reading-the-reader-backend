using FastEndpoints;
using ReadingTheReader.core.Application.ApplicationContracts.Realtime;

namespace ReadingTheReader.WebApi.EyeTrackerEndpoints;

public class StopTrackingEndpoint : EndpointWithoutRequest
{
    private readonly IEyeTrackerPublisher _eyeTrackerPublisher;

    public StopTrackingEndpoint(IEyeTrackerPublisher eyeTrackerPublisher)
    {
        _eyeTrackerPublisher = eyeTrackerPublisher;
    }

    public override void Configure()
    {
        Post("/eyetrackers/stop");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await _eyeTrackerPublisher.StopTrackingAsync(ct);
        await Send.OkAsync(cancellation: ct);
    }
}
