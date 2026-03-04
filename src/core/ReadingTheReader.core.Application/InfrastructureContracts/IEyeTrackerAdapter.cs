using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.InfrastructureContracts;

public interface IEyeTrackerAdapter
{
    event EventHandler<GazeData> GazeDataReceived;

    Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers();
    
    Task SelectEyeTracker(string serialNumber, byte[] licenseFileBytes, CancellationToken ct = default);

    Task StartEyeTracking();

    void StopEyeTracking();
}
