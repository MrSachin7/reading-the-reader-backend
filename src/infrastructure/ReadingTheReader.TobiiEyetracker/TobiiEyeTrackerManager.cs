using ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;
using ReadingTheReader.core.Domain;
using Tobii.Research;

namespace ReadingTheReader.TobiiEyetracker;

public class TobiiEyeTrackerManager : IEyeTrackerManager{
    public async Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers() {
       var trackers =await EyeTrackingOperations.FindAllEyeTrackersAsync();
       return trackers.Select(tracker => new EyeTrackerDevice() {
           Name = tracker.DeviceName,
           SerialNumber = tracker.SerialNumber,
           Model = tracker.Model

       }).ToList();
    }
}