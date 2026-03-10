namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public static class CalibrationPatterns
{
    public const string ScreenBasedFivePoint = "screen-based-five-point";
}

public sealed record CalibrationPointDefinition(
    string PointId,
    string Label,
    float X,
    float Y);

public sealed record CalibrationPointState(
    string PointId,
    string Label,
    float X,
    float Y,
    string Status,
    int Attempts,
    long? CollectedAtUnixMs,
    string? HardwareStatus,
    IReadOnlyList<string> Notes);

public sealed record CalibrationCollectionResult(
    string Status,
    bool Succeeded,
    int Attempts,
    IReadOnlyList<string> Notes);

public sealed record CalibrationComputeResult(
    string Status,
    bool Applied,
    int CalibrationPointCount,
    IReadOnlyList<string> Notes);

public sealed record CalibrationRunResult(
    string Status,
    bool Applied,
    int CalibrationPointCount,
    IReadOnlyList<string> Notes);

public sealed record CalibrationSessionSnapshot(
    Guid? SessionId,
    string Status,
    string Pattern,
    long? StartedAtUnixMs,
    long? UpdatedAtUnixMs,
    long? CompletedAtUnixMs,
    IReadOnlyList<CalibrationPointState> Points,
    CalibrationRunResult? Result,
    IReadOnlyList<string> Notes);

public static class CalibrationSessionSnapshots
{
    public static CalibrationSessionSnapshot CreateIdle()
    {
        return new CalibrationSessionSnapshot(
            null,
            "idle",
            CalibrationPatterns.ScreenBasedFivePoint,
            null,
            null,
            null,
            [],
            null,
            []);
    }

    public static bool IsApplied(CalibrationSessionSnapshot? snapshot)
    {
        return snapshot is not null &&
               string.Equals(snapshot.Status, "completed", StringComparison.OrdinalIgnoreCase) &&
               snapshot.Result?.Applied == true;
    }
}
