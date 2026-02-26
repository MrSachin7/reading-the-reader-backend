using FastEndpoints;
using ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;

namespace ReadingTheReader.WebApi.EyeTrackerEndpoints;

public class StartTrackingEndpoint : EndpointWithoutRequest {
    private readonly IEyeTrackerManager _eyeTrackerManager;
    public StartTrackingEndpoint(IEyeTrackerManager eyeTrackerManager) {
        _eyeTrackerManager = eyeTrackerManager;
    }


    public override void Configure() {
        Post("/eyetrackers/start");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) {
         await _eyeTrackerManager.StartEyeTracking();
        await Send.OkAsync(cancellation:ct);
    }
}