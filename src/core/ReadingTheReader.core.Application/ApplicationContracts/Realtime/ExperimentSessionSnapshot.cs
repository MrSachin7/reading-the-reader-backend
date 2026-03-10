using ReadingTheReader.core.Domain;

namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public sealed record ExperimentSetupSnapshot(
    bool EyeTrackerSetupCompleted,
    bool ParticipantSetupCompleted,
    bool CalibrationCompleted,
    int CurrentStepIndex
)
{
    public ExperimentSetupSnapshot Copy()
    {
        return new ExperimentSetupSnapshot(
            EyeTrackerSetupCompleted,
            ParticipantSetupCompleted,
            CalibrationCompleted,
            CurrentStepIndex);
    }
}

public sealed record ExperimentSessionSnapshot(
    Guid? SessionId,
    bool IsActive,
    long StartedAtUnixMs,
    long? StoppedAtUnixMs,
    Participant? Participant,
    EyeTrackerDevice? EyeTrackerDevice,
    CalibrationSessionSnapshot Calibration,
    ExperimentSetupSnapshot Setup,
    long ReceivedGazeSamples,
    GazeData? LatestGazeSample,
    int ConnectedClients
)
{
    public ExperimentSessionSnapshot Copy()
    {
        return new ExperimentSessionSnapshot(
            SessionId,
            IsActive,
            StartedAtUnixMs,
            StoppedAtUnixMs,
            Participant?.Copy(),
            EyeTrackerDevice?.Copy(),
            Calibration is null ? CalibrationSessionSnapshots.CreateIdle() : CopyCalibration(Calibration),
            Setup is null ? new ExperimentSetupSnapshot(false, false, false, 0) : Setup.Copy(),
            ReceivedGazeSamples,
            LatestGazeSample?.Copy(),
            ConnectedClients);
    }

    private static CalibrationSessionSnapshot CopyCalibration(CalibrationSessionSnapshot source)
    {
        return new CalibrationSessionSnapshot(
            source.SessionId,
            source.Status,
            source.Pattern,
            source.StartedAtUnixMs,
            source.UpdatedAtUnixMs,
            source.CompletedAtUnixMs,
            CopyCalibrationPoints(source.Points),
            source.Result is null ? null : CopyCalibrationRunResult(source.Result),
            source.Notes is null ? [] : [.. source.Notes]);
    }

    private static IReadOnlyList<CalibrationPointState> CopyCalibrationPoints(IReadOnlyList<CalibrationPointState>? points)
    {
        if (points is null || points.Count == 0)
        {
            return [];
        }

        var copies = new CalibrationPointState[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            copies[i] = CopyCalibrationPointState(points[i]);
        }

        return copies;
    }

    private static CalibrationPointState CopyCalibrationPointState(CalibrationPointState source)
    {
        return new CalibrationPointState(
            source.PointId,
            source.Label,
            source.X,
            source.Y,
            source.Status,
            source.Attempts,
            source.CollectedAtUnixMs,
            source.HardwareStatus,
            source.Notes is null ? [] : [.. source.Notes]);
    }

    private static CalibrationRunResult CopyCalibrationRunResult(CalibrationRunResult source)
    {
        return new CalibrationRunResult(
            source.Status,
            source.Applied,
            source.CalibrationPointCount,
            source.Notes is null ? [] : [.. source.Notes]);
    }
}
