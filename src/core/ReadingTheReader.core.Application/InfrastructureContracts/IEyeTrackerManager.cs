using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.InfrastructureContracts;

public interface IEyeTrackerManager
{
    event EventHandler<GazeData> GazeDataReceived;

    Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers();

    Task StartEyeTracking();

    void StopEyeTracking();
}
