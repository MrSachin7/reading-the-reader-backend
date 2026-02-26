using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;

public interface IEyeTrackerManager {

    Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers();

}
