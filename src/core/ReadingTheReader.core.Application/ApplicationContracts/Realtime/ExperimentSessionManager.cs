using System.Collections.Concurrent;
using System.Text.Json;
using ReadingTheReader.core.Application.InfrastructureContracts;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public sealed class ExperimentSessionManager : IExperimentSessionManager
{
    private readonly IEyeTrackerAdapter _eyeTrackerAdapter;
    private readonly IClientBroadcasterAdapter _clientBroadcasterAdapter;
    private readonly IExperimentStateStoreAdapter _experimentStateStoreAdapter;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly ConcurrentDictionary<string, byte> _gazeSubscribers = new();

    private int _isSubscribedToHardware;
    private int _isHardwareTracking;
    private long _receivedGazeSamples;
    private GazeData? _latestGazeSample;
    private ExperimentSession _session = ExperimentSession.Inactive;

    public ExperimentSessionManager(
        IEyeTrackerAdapter eyeTrackerAdapter,
        IClientBroadcasterAdapter clientBroadcasterAdapter,
        IExperimentStateStoreAdapter experimentStateStoreAdapter)
    {
        _eyeTrackerAdapter = eyeTrackerAdapter;
        _clientBroadcasterAdapter = clientBroadcasterAdapter;
        _experimentStateStoreAdapter = experimentStateStoreAdapter;
    }

    public async ValueTask SetCurrentParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            var current = Volatile.Read(ref _session);
            var participantCopy = participant.Copy();
            Volatile.Write(ref _session, current with { Participant = participantCopy });

            var snapshot = GetCurrentSnapshot();
            await _experimentStateStoreAdapter.SaveSnapshotAsync(snapshot, ct);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }
    
    public async ValueTask SetCurrentEyeTrackerAsync(EyeTrackerDevice eyeTrackerDevice, CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            var current = Volatile.Read(ref _session);
            var eyeTrackerCopy = eyeTrackerDevice.Copy();
            Volatile.Write(ref _session, current with { EyeTrackerDevice = eyeTrackerCopy });

            var snapshot = GetCurrentSnapshot();
            await _experimentStateStoreAdapter.SaveSnapshotAsync(snapshot, ct);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<bool> StartSessionAsync(CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            var current = Volatile.Read(ref _session);
            if (current.IsActive)
            {
                return false;
            }

            Interlocked.Exchange(ref _receivedGazeSamples, 0);
            Volatile.Write(ref _latestGazeSample, null);

            var startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Volatile.Write(ref _session, ExperimentSession.StartNew(startedAt, current.Participant, current.EyeTrackerDevice));
            await EnsureGazeStreamingStateAsync(ct);

            var snapshot = GetCurrentSnapshot();
            await _experimentStateStoreAdapter.SaveSnapshotAsync(snapshot, ct);
            await _clientBroadcasterAdapter.BroadcastAsync(MessageTypes.ExperimentStarted, snapshot, ct);
            return true;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<bool> StopSessionAsync(CancellationToken ct = default)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            var current = Volatile.Read(ref _session);
            if (!current.IsActive)
            {
                return false;
            }

            Volatile.Write(ref _session, current.Stop(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            await EnsureGazeStreamingStateAsync(ct);

            var snapshot = GetCurrentSnapshot();
            await _experimentStateStoreAdapter.SaveSnapshotAsync(snapshot, ct);
            await _clientBroadcasterAdapter.BroadcastAsync(MessageTypes.ExperimentStopped, snapshot, ct);
            return true;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public void UpdateGazeSample(GazeData gazeData)
    {
        Interlocked.Increment(ref _receivedGazeSamples);
        Volatile.Write(ref _latestGazeSample, gazeData);
    }

    public ExperimentSessionSnapshot GetCurrentSnapshot()
    {
        var session = Volatile.Read(ref _session);
        var latest = Volatile.Read(ref _latestGazeSample);

        return new ExperimentSessionSnapshot(
            session.Id,
            session.IsActive,
            session.StartedAtUnixMs,
            session.StoppedAtUnixMs,
            session.Participant?.Copy(),
            session.EyeTrackerDevice?.Copy(),
            Interlocked.Read(ref _receivedGazeSamples),
            latest?.Copy(),
            _clientBroadcasterAdapter.ConnectedClients
        );
    }

    public async Task HandleInboundMessageAsync(string connectionId, string messageType, JsonElement payload, CancellationToken ct = default)
    {
        switch (messageType)
        {
            case MessageTypes.Ping:
                await _clientBroadcasterAdapter.SendToClientAsync(connectionId, MessageTypes.Pong, new
                {
                    serverTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }, ct);
                break;

            case MessageTypes.StartExperiment:
                await StartSessionAsync(ct);
                break;

            case MessageTypes.StopExperiment:
                await StopSessionAsync(ct);
                break;

            case MessageTypes.SubscribeGazeData:
                await SubscribeGazeDataAsync(connectionId, ct);
                break;

            case MessageTypes.UnsubscribeGazeData:
                await UnsubscribeGazeDataAsync(connectionId, ct);
                break;

            case MessageTypes.GetExperimentState:
                await _clientBroadcasterAdapter.SendToClientAsync(connectionId, MessageTypes.ExperimentState, GetCurrentSnapshot(), ct);
                break;

            case MessageTypes.ResearcherCommand:
                if (payload.ValueKind == JsonValueKind.Object &&
                    payload.TryGetProperty("command", out var command) &&
                    command.ValueKind == JsonValueKind.String)
                {
                    var commandValue = command.GetString();
                    if (string.Equals(commandValue, MessageTypes.StartExperiment, StringComparison.OrdinalIgnoreCase))
                    {
                        await StartSessionAsync(ct);
                        return;
                    }

                    if (string.Equals(commandValue, MessageTypes.StopExperiment, StringComparison.OrdinalIgnoreCase))
                    {
                        await StopSessionAsync(ct);
                        return;
                    }
                }

                await _clientBroadcasterAdapter.SendToClientAsync(connectionId, MessageTypes.Error, new
                {
                    message = "Unsupported researcher command"
                }, ct);
                break;

            default:
                await _clientBroadcasterAdapter.SendToClientAsync(connectionId, MessageTypes.Error, new
                {
                    message = $"Unsupported message type '{messageType}'"
                }, ct);
                break;
        }
    }

    public Task HandleClientDisconnectedAsync(string connectionId, CancellationToken ct = default)
    {
        return UnsubscribeGazeDataAsync(connectionId, ct);
    }

    private void OnGazeDataReceived(object? sender, GazeData gazeData)
    {
        if (_gazeSubscribers.IsEmpty)
        {
            return;
        }

        UpdateGazeSample(gazeData);
        var subscribers = _gazeSubscribers.Keys.ToArray();
        var sendTask = BroadcastGazeSampleAsync(subscribers, gazeData);
        if (!sendTask.IsCompletedSuccessfully)
        {
            _ = IgnoreFailuresAsync(sendTask.AsTask());
        }
    }

    private async Task SubscribeGazeDataAsync(string connectionId, CancellationToken ct)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            _gazeSubscribers[connectionId] = 0;

            try
            {
                await EnsureGazeStreamingStateAsync(ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"SubscribeGazeData failed. ConnectionId={connectionId}, Reason=EyeTrackerNotReady, Error={ex.Message}");
                await _clientBroadcasterAdapter.SendToClientAsync(connectionId, MessageTypes.Error, new
                {
                    message = $"Cannot stream gaze data because the eye tracker is not ready or the licence has not been applied: {ex.Message}"
                }, ct);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task UnsubscribeGazeDataAsync(string connectionId, CancellationToken ct)
    {
        await _lifecycleGate.WaitAsync(ct);
        try
        {
            _gazeSubscribers.TryRemove(connectionId, out _);
            await EnsureGazeStreamingStateAsync(ct);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task EnsureGazeStreamingStateAsync(CancellationToken ct)
    {
        var session = Volatile.Read(ref _session);
        var shouldStream = !_gazeSubscribers.IsEmpty;

        if (shouldStream)
        {
            if (Interlocked.Exchange(ref _isSubscribedToHardware, 1) == 0)
            {
                _eyeTrackerAdapter.GazeDataReceived += OnGazeDataReceived;
            }

            if (Interlocked.Exchange(ref _isHardwareTracking, 1) == 0)
            {
                try
                {
                    await _eyeTrackerAdapter.StartEyeTracking();
                    Console.WriteLine(
                        $"Gaze streaming started. SessionId={session.Id}, Subscribers={_gazeSubscribers.Count}");
                }
                catch
                {
                    Interlocked.Exchange(ref _isHardwareTracking, 0);
                    if (Interlocked.Exchange(ref _isSubscribedToHardware, 0) == 1)
                    {
                        _eyeTrackerAdapter.GazeDataReceived -= OnGazeDataReceived;
                    }

                    throw;
                }
            }

            return;
        }

        if (Interlocked.Exchange(ref _isHardwareTracking, 0) == 1)
        {
            _eyeTrackerAdapter.StopEyeTracking();
            Console.WriteLine(
                $"Gaze streaming stopped. SessionId={session.Id}, Subscribers={_gazeSubscribers.Count}, SessionActive={session.IsActive}");
        }

        if (Interlocked.Exchange(ref _isSubscribedToHardware, 0) == 1)
        {
            _eyeTrackerAdapter.GazeDataReceived -= OnGazeDataReceived;
        }
    }

    private async ValueTask BroadcastGazeSampleAsync(string[] subscribers, GazeData gazeData)
    {
        foreach (var connectionId in subscribers)
        {
            await _clientBroadcasterAdapter.SendToClientAsync(connectionId, MessageTypes.GazeSample, gazeData);
        }
    }

    private static async Task IgnoreFailuresAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // Keep gaze ingestion non-blocking.
        }
    }

}
