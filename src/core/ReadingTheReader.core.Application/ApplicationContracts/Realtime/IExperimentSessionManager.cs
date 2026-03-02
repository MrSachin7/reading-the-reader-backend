using System.Text.Json;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public interface IExperimentSessionManager
{
    Task<bool> StartSessionAsync(CancellationToken ct = default);

    Task<bool> StopSessionAsync(CancellationToken ct = default);

    void UpdateGazeSample(GazeData gazeData);

    ExperimentSessionSnapshot GetCurrentSnapshot();

    Task HandleInboundMessageAsync(string connectionId, string messageType, JsonElement payload, CancellationToken ct = default);
}
