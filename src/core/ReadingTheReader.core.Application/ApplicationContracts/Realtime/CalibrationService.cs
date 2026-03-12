using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public sealed class CalibrationService : ICalibrationService
{
    private static readonly IReadOnlyList<CalibrationPointDefinition> DefaultPoints =
    [
        new("center", "Center", 0.5f, 0.5f),
        new("top-left", "Top left", 0.1f, 0.1f),
        new("bottom-left", "Bottom left", 0.1f, 0.9f),
        new("top-right", "Top right", 0.9f, 0.1f),
        new("bottom-right", "Bottom right", 0.9f, 0.9f),
    ];

    private readonly IEyeTrackerAdapter _eyeTrackerAdapter;
    private readonly IClientBroadcasterAdapter _clientBroadcasterAdapter;
    private readonly IExperimentSessionManager _experimentSessionManager;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CalibrationSessionSnapshot _snapshot = CalibrationSessionSnapshots.CreateIdle();

    public CalibrationService(
        IEyeTrackerAdapter eyeTrackerAdapter,
        IClientBroadcasterAdapter clientBroadcasterAdapter,
        IExperimentSessionManager experimentSessionManager)
    {
        _eyeTrackerAdapter = eyeTrackerAdapter;
        _clientBroadcasterAdapter = clientBroadcasterAdapter;
        _experimentSessionManager = experimentSessionManager;
    }

    public CalibrationSessionSnapshot GetCurrentSnapshot()
    {
        return _snapshot;
    }

    public async Task<CalibrationSessionSnapshot> StartCalibrationAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (string.Equals(_snapshot.Status, "running", StringComparison.OrdinalIgnoreCase))
            {
                await _eyeTrackerAdapter.CancelCalibrationAsync(ct);
            }

            await _eyeTrackerAdapter.BeginCalibrationAsync(ct);

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _snapshot = new CalibrationSessionSnapshot(
                Guid.NewGuid(),
                "running",
                CalibrationPatterns.ScreenBasedFivePoint,
                now,
                now,
                null,
                DefaultPoints
                    .Select(point => new CalibrationPointState(
                        point.PointId,
                        point.Label,
                        point.X,
                        point.Y,
                        "pending",
                        0,
                        null,
                        null,
                        []))
                    .ToArray(),
                null,
                ["Calibration mode entered on the selected eye tracker."]);

            await BroadcastSnapshotAsync(ct);
            return _snapshot;
        }
        catch (Exception ex)
        {
            _snapshot = CreateFailedSnapshot(ex.Message);
            await BroadcastSnapshotAsync(ct);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CalibrationSessionSnapshot> CollectPointAsync(string pointId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pointId))
        {
            throw new ArgumentException("pointId is required.", nameof(pointId));
        }

        await _gate.WaitAsync(ct);
        try
        {
            EnsureRunningSession();

            var pointIndex = FindPointIndex(pointId);
            var point = _snapshot.Points[pointIndex];

            _snapshot = _snapshot with
            {
                UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Points = ReplacePoint(pointIndex, point with
                {
                    Status = "collecting",
                    HardwareStatus = null,
                    Notes = ["Collecting calibration data from the eye tracker."]
                })
            };
            await BroadcastSnapshotAsync(ct);

            var result = await _eyeTrackerAdapter.CollectCalibrationDataAsync(point.X, point.Y, ct);
            var collectedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nextPoint = point with
            {
                Status = result.Succeeded ? "collected" : "failed",
                Attempts = result.Attempts,
                CollectedAtUnixMs = collectedAtUnixMs,
                HardwareStatus = result.Status,
                Notes = result.Notes
            };

            _snapshot = _snapshot with
            {
                Status = result.Succeeded ? "running" : "failed",
                UpdatedAtUnixMs = collectedAtUnixMs,
                CompletedAtUnixMs = result.Succeeded ? null : collectedAtUnixMs,
                Points = ReplacePoint(pointIndex, nextPoint),
                Notes = result.Succeeded
                    ? [$"Collected data for {point.Label.ToLowerInvariant()}."]
                    : [$"Collection failed for {point.Label.ToLowerInvariant()}. Restart calibration and try again."]
            };

            if (!result.Succeeded)
            {
                await _eyeTrackerAdapter.CancelCalibrationAsync(ct);
            }

            await BroadcastSnapshotAsync(ct);
            return _snapshot;
        }
        catch (Exception ex)
        {
            await SafeCancelCalibrationAsync(ct);
            _snapshot = CreateFailedSnapshot(ex.Message, _snapshot.Points);
            await BroadcastSnapshotAsync(ct);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CalibrationSessionSnapshot> FinishCalibrationAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            EnsureRunningSession();

            if (_snapshot.Points.Any(point => !string.Equals(point.Status, "collected", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("All calibration points must be collected before calibration can be applied.");
            }

            var result = await _eyeTrackerAdapter.ComputeAndApplyCalibrationAsync(ct);
            var completedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _snapshot = _snapshot with
            {
                Status = result.Applied ? "completed" : "failed",
                UpdatedAtUnixMs = completedAtUnixMs,
                CompletedAtUnixMs = completedAtUnixMs,
                Result = new CalibrationRunResult(
                    result.Status,
                    result.Applied,
                    result.CalibrationPointCount,
                    result.Notes),
                Notes = result.Applied
                    ? ["The eye tracker calibration was computed and applied successfully."]
                    : ["The eye tracker rejected the collected data. Restart calibration and try again."]
            };

            await _eyeTrackerAdapter.CancelCalibrationAsync(ct);
            await BroadcastSnapshotAsync(ct);
            return _snapshot;
        }
        catch (Exception ex)
        {
            await SafeCancelCalibrationAsync(ct);
            _snapshot = CreateFailedSnapshot(ex.Message, _snapshot.Points);
            await BroadcastSnapshotAsync(ct);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CalibrationSessionSnapshot> CancelCalibrationAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await _eyeTrackerAdapter.CancelCalibrationAsync(ct);

            if (!string.Equals(_snapshot.Status, "running", StringComparison.OrdinalIgnoreCase))
            {
                return _snapshot;
            }

            var completedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _snapshot = _snapshot with
            {
                Status = "cancelled",
                UpdatedAtUnixMs = completedAtUnixMs,
                CompletedAtUnixMs = completedAtUnixMs,
                Notes = ["Calibration was cancelled before completion."]
            };

            await BroadcastSnapshotAsync(ct);
            return _snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static CalibrationSessionSnapshot CreateFailedSnapshot(
        string message,
        IReadOnlyList<CalibrationPointState>? points = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new CalibrationSessionSnapshot(
            Guid.NewGuid(),
            "failed",
            CalibrationPatterns.ScreenBasedFivePoint,
            now,
            now,
            now,
            points ?? [],
            null,
            [message]);
    }

    private void EnsureRunningSession()
    {
        if (!string.Equals(_snapshot.Status, "running", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No active calibration session. Start calibration first.");
        }
    }

    private int FindPointIndex(string pointId)
    {
        var index = _snapshot.Points
            .ToList()
            .FindIndex(point => point.PointId.Equals(pointId, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            throw new ArgumentException($"Unknown calibration point '{pointId}'.", nameof(pointId));
        }

        return index;
    }

    private CalibrationPointState[] ReplacePoint(int pointIndex, CalibrationPointState point)
    {
        var nextPoints = _snapshot.Points.ToArray();
        nextPoints[pointIndex] = point;
        return nextPoints;
    }

    private async Task BroadcastSnapshotAsync(CancellationToken ct)
    {
        await _experimentSessionManager.SetCalibrationStateAsync(_snapshot, ct);
        await _clientBroadcasterAdapter.BroadcastAsync(MessageTypes.CalibrationStateChanged, _snapshot, ct);
    }

    private async Task SafeCancelCalibrationAsync(CancellationToken ct)
    {
        try
        {
            await _eyeTrackerAdapter.CancelCalibrationAsync(ct);
        }
        catch
        {
            // Best effort cleanup for calibration mode.
        }
    }
}
