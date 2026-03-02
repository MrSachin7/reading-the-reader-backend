namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public interface IEyeTrackerPublisher
{
    Task StartTrackingAsync(CancellationToken ct = default);

    Task StopTrackingAsync(CancellationToken ct = default);
}
