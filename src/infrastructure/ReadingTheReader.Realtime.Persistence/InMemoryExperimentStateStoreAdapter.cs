using ReadingTheReader.core.Application.ApplicationContracts.Realtime;
using ReadingTheReader.core.Application.InfrastructureContracts;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.Realtime.Persistence;

public sealed class InMemoryExperimentStateStoreAdapter : IExperimentStateStoreAdapter
{
    private readonly object _gate = new();
    private ExperimentSessionSnapshot? _latest;

    public ValueTask SaveSnapshotAsync(ExperimentSessionSnapshot snapshot, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _latest = Clone(snapshot);
            Console.WriteLine($"Saving snapshot to InMemory : {_latest?.EyeTrackerDevice?.SerialNumber}, {_latest?.Participant?.ToString()}");
        }

        return ValueTask.CompletedTask;
    }
        
    public ValueTask<ExperimentSessionSnapshot?> LoadLatestSnapshotAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            return ValueTask.FromResult(_latest is null ? null : Clone(_latest));
        }
    }

    private static ExperimentSessionSnapshot Clone(ExperimentSessionSnapshot source)
    {
        return new ExperimentSessionSnapshot(
            source.SessionId,
            source.IsActive,
            source.StartedAtUnixMs,
            source.StoppedAtUnixMs,
            source.Participant is null ? null : CloneParticipant(source.Participant),
            source.EyeTrackerDevice is null ? null : CloneEyeTrackerDevice(source.EyeTrackerDevice),
            source.ReceivedGazeSamples,
            source.LatestGazeSample is null ? null : CloneGaze(source.LatestGazeSample),
            source.ConnectedClients
        );
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
