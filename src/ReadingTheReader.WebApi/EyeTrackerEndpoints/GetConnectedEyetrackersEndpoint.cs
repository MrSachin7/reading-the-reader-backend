using FastEndpoints;
using ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.WebApi.EyeTrackerEndpoints;

public class GetConnectedEyetrackersEndpoint: EndpointWithoutRequest<List<EyeTrackerDevice>> {
    private readonly IEyeTrackerManager _eyeTrackerManager;

    public GetConnectedEyetrackersEndpoint(IEyeTrackerManager eyeTrackerManager) {
        _eyeTrackerManager = eyeTrackerManager;
    }

    public override void Configure() {
        Get("/eyetrackers");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) {
        var trackers = await _eyeTrackerManager.GetAllConnectedEyeTrackers();
        await Send.OkAsync(trackers, ct);
    }
}

