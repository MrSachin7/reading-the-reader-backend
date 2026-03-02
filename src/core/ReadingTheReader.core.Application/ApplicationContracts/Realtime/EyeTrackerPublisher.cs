namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public sealed class EyeTrackerPublisher : IEyeTrackerPublisher
{
    private readonly IExperimentSessionManager _sessionManager;

    public EyeTrackerPublisher(IExperimentSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public Task StartTrackingAsync(CancellationToken ct = default)
    {
        return _sessionManager.StartSessionAsync(ct);
    }

    public Task StopTrackingAsync(CancellationToken ct = default)
    {
        return _sessionManager.StopSessionAsync(ct);
    }
}
