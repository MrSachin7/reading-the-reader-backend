#if WINDOWS
using Tobii.Research;
#endif
using ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.TobiiEyetracker;

public class TobiiEyeTrackerManager : IEyeTrackerManager
{
    public event EventHandler<GazeData>? GazeDataReceived;

#if WINDOWS
    private IEyeTracker? _activeTracker;

    public Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers()
    {
        var found = EyeTrackingOperations.FindAllEyeTrackers();
        var devices = found.Select(t => new EyeTrackerDevice
        {
            Name = t.DeviceName,
            SerialNumber = t.SerialNumber,
            Model = t.Model
        }).ToList();
        return Task.FromResult(devices);
    }

    public Task StartEyeTracking()
    {
        var found = EyeTrackingOperations.FindAllEyeTrackers();
        if (found.Count == 0)
            throw new InvalidOperationException("No Tobii eye trackers found.");

        _activeTracker = found[0];

        // Apply license if present
        var licenseFilePath = Path.Combine(AppContext.BaseDirectory, "license_key_IS404-100106341184");
        if (!File.Exists(licenseFilePath))
            licenseFilePath = "C:\\Users\\nepal\\OneDrive\\Desktop\\reading-the-reader-backend\\license_key_IS404-100106341184";

        if (File.Exists(licenseFilePath))
        {
            try
            {
                Console.WriteLine("🔑 License file found, applying Tobii license");
                var licenseBytes = File.ReadAllBytes(licenseFilePath);
                Console.WriteLine("🔑 License file read successfully, applying license to Tobii eye tracker");
                var licenseKey = new LicenseKey(licenseBytes);
                var licenseCollection = new LicenseCollection([licenseKey]);
                _activeTracker.TryApplyLicenses(licenseCollection, out var result);
                bool valid = result[0].ValidationResult == LicenseValidationResult.Ok;
                Console.WriteLine(valid
                    ? "✅ Tobii license applied successfully"
                    : "⚠️  Tobii license validation failed, continuing without license");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not apply license: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("ℹ️  No license file found, continuing without license");
        }

        _activeTracker.GazeDataReceived += OnTobiiGazeDataReceived;
        Console.WriteLine($"✅ Tobii eye tracking started on '{_activeTracker.Address}'");
        return Task.CompletedTask;
    }

    public void StopEyeTracking()
    {
        if (_activeTracker is null) return;
        _activeTracker.GazeDataReceived -= OnTobiiGazeDataReceived;
        _activeTracker = null;
        Console.WriteLine("🛑 Tobii eye tracking stopped");
    }

    private void OnTobiiGazeDataReceived(object? sender, GazeDataEventArgs e)
    {
        Console.WriteLine(
            $"Gaze: ts={e.DeviceTimeStamp} " +
            $"L=({e.LeftEye.GazePoint.PositionOnDisplayArea.X:F3},{e.LeftEye.GazePoint.PositionOnDisplayArea.Y:F3}) " +
            $"R=({e.RightEye.GazePoint.PositionOnDisplayArea.X:F3},{e.RightEye.GazePoint.PositionOnDisplayArea.Y:F3}) " +
            $"LValid={e.LeftEye.GazePoint.Validity} RValid={e.RightEye.GazePoint.Validity}");

        GazeDataReceived?.Invoke(this, new GazeData
        {
            DeviceTimeStamp  = e.DeviceTimeStamp,
            LeftEyeX         = e.LeftEye.GazePoint.PositionOnDisplayArea.X,
            LeftEyeY         = e.LeftEye.GazePoint.PositionOnDisplayArea.Y,
            LeftEyeValidity  = e.LeftEye.GazePoint.Validity.ToString(),
            RightEyeX        = e.RightEye.GazePoint.PositionOnDisplayArea.X,
            RightEyeY        = e.RightEye.GazePoint.PositionOnDisplayArea.Y,
            RightEyeValidity = e.RightEye.GazePoint.Validity.ToString()
        });
    }

#else
    // Mock implementation — Tobii SDK is Windows-only, not available on macOS

    public Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers()
    {
        return Task.FromResult(new List<EyeTrackerDevice>
        {
            new EyeTrackerDevice
            {
                Name = "Mock Tobii Eye Tracker",
                SerialNumber = "MOCK-001",
                Model = "Tobii Pro X3-120"
            }
        });
    }

    public Task StartEyeTracking()
    {
        Console.WriteLine("⚠️  Tobii SDK not available on this platform — running mock eye tracker");
        Console.WriteLine("✅ Mock eye tracking started");
        return Task.CompletedTask;
    }

    public void StopEyeTracking()
    {
        Console.WriteLine("🛑 Mock eye tracking stopped");
    }
#endif
}