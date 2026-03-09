using System.Text.Json;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public interface IExperimentSessionManager
{
    ValueTask SetCurrentParticipantAsync(Participant participant, CancellationToken ct = default);
    
    ValueTask SetCurrentEyeTrackerAsync(EyeTrackerDevice eyeTrackerDevice, CancellationToken ct = default);

    Task<bool> StartSessionAsync(CancellationToken ct = default);

    Task<bool> StopSessionAsync(CancellationToken ct = default);

    void UpdateGazeSample(GazeData gazeData);

    ExperimentSessionSnapshot GetCurrentSnapshot();

    Task HandleInboundMessageAsync(string connectionId, string messageType, JsonElement payload, CancellationToken ct = default);

    Task HandleClientDisconnectedAsync(string connectionId, CancellationToken ct = default);
}
