using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public sealed record ExperimentSessionSnapshot(
    Guid? SessionId,
    bool IsActive,
    long StartedAtUnixMs,
    long? StoppedAtUnixMs,
    Participant? Participant,
    EyeTrackerDevice? EyeTrackerDevice,
    long ReceivedGazeSamples,
    GazeData? LatestGazeSample,
    int ConnectedClients
);
