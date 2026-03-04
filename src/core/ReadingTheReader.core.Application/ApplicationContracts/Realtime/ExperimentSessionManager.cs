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

    private int _isSubscribed;
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
            var participantCopy = CloneParticipant(participant);
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
            var eyeTrackerCopy = CloneEyeTrackerDevice(eyeTrackerDevice);
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

            if (Interlocked.Exchange(ref _isSubscribed, 1) == 0)
            {
                _eyeTrackerAdapter.GazeDataReceived += OnGazeDataReceived;
            }

            try
            {
                await _eyeTrackerAdapter.StartEyeTracking();
            }
            catch
            {
                if (Interlocked.Exchange(ref _isSubscribed, 0) == 1)
                {
                    _eyeTrackerAdapter.GazeDataReceived -= OnGazeDataReceived;
                }

                throw;
            }

            Interlocked.Exchange(ref _receivedGazeSamples, 0);
            Volatile.Write(ref _latestGazeSample, null);

            var startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Volatile.Write(ref _session, ExperimentSession.StartNew(startedAt, current.Participant, current.EyeTrackerDevice));

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

            _eyeTrackerAdapter.StopEyeTracking();

            if (Interlocked.Exchange(ref _isSubscribed, 0) == 1)
            {
                _eyeTrackerAdapter.GazeDataReceived -= OnGazeDataReceived;
            }

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
            session.Participant is null ? null : CloneParticipant(session.Participant),
            session.EyeTrackerDevice is null ? null : CloneEyeTrackerDevice(session.EyeTrackerDevice),
            Interlocked.Read(ref _receivedGazeSamples),
            latest is null ? null : CloneGaze(latest),
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

    private void OnGazeDataReceived(object? sender, GazeData gazeData)
    {
        var session = Volatile.Read(ref _session);
        if (!session.IsActive)
        {
            return;
        }

        UpdateGazeSample(gazeData);
        var sendTask = _clientBroadcasterAdapter.BroadcastAsync(MessageTypes.GazeSample, gazeData);
        if (!sendTask.IsCompletedSuccessfully)
        {
            _ = IgnoreFailuresAsync(sendTask.AsTask());
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

    private static GazeData CloneGaze(GazeData source)
    {
        return new GazeData
        {
            DeviceTimeStamp = source.DeviceTimeStamp,
            LeftEyeX = source.LeftEyeX,
            LeftEyeY = source.LeftEyeY,
            LeftEyeValidity = source.LeftEyeValidity,
            RightEyeX = source.RightEyeX,
            RightEyeY = source.RightEyeY,
            RightEyeValidity = source.RightEyeValidity
        };
    }

    private static Participant CloneParticipant(Participant source)
    {
        return new Participant
        {
            Name = source.Name,
            Age = source.Age,
            Sex = source.Sex,
            ExistingEyeCondition = source.ExistingEyeCondition,
            ReadingProficiency = source.ReadingProficiency
        };
    }

    private static EyeTrackerDevice CloneEyeTrackerDevice(EyeTrackerDevice source)
    {
        return new EyeTrackerDevice
        {
            Name = source.Name,
            Model = source.Model,
            SerialNumber = source.SerialNumber,
            HasSavedLicence = source.HasSavedLicence
        };
    }
}
