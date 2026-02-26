using ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;
using ReadingTheReader.core.Domain;

#if WINDOWS
using Tobii.Research;
#endif

namespace ReadingTheReader.TobiiEyetracker;

public class TobiiEyeTrackerManager : IEyeTrackerManager
{
#if WINDOWS
    private IEyeTracker? _currentTracker;
    private bool _isTracking = false;

    public async Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers()
    {
        var trackers = await EyeTrackingOperations.FindAllEyeTrackersAsync();
        return trackers.Select(tracker => new EyeTrackerDevice()
        {
            Name = tracker.DeviceName,
            SerialNumber = tracker.SerialNumber,
            Model = tracker.Model
        }).ToList();
    }

    public async Task StartEyeTracking()
    {
        Console.WriteLine("=== Tobii Eye Tracker Manager - Starting Eye Tracking ===");

        try
        {
            // Find all eye trackers
            var trackers = await EyeTrackingOperations.FindAllEyeTrackersAsync();

            if (trackers.Count == 0)
            {
                Console.WriteLine("❌ No Eye Trackers found!");
                return;
            }

            _currentTracker = trackers[0];
            Console.WriteLine($"✅ Found {trackers.Count} tracker(s)");
            Console.WriteLine($"📱 Using Tobii Tracker: '{_currentTracker.Address}'");
            Console.WriteLine($"   - Device Name: {_currentTracker.DeviceName}");
            Console.WriteLine($"   - Serial Number: {_currentTracker.SerialNumber}");
            Console.WriteLine($"   - Model: {_currentTracker.Model}");

            // Apply license
            await ApplyLicense(_currentTracker);

            // Subscribe to gaze data events
            _currentTracker.GazeDataReceived += OnGazeDataReceived;

            // Start gaze data collection
            Console.WriteLine("🎯 Starting gaze data collection...");
            _isTracking = true;

            Console.WriteLine("📊 Eye tracking is now active. Gaze data will be logged below:");
            Console.WriteLine("" + new string('=', 80));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error starting eye tracking: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task ApplyLicense(IEyeTracker tracker)
    {
        try
        {
            // Look for license file in the root directory
            var rootPath = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(rootPath, "liscence")) && Directory.GetParent(rootPath) != null)
            {
                rootPath = Directory.GetParent(rootPath)!.FullName;
            }

            var licenseFilePath = Path.Combine(rootPath, "liscence");

            Console.WriteLine($"🔑 Looking for license file at: {licenseFilePath}");

            if (!File.Exists(licenseFilePath))
            {
                Console.WriteLine("⚠️  License file 'liscence' not found - continuing without license");
                return;
            }

            var licenseBytes = File.ReadAllBytes(licenseFilePath);
            Console.WriteLine($"✅ Successfully read license file ({licenseBytes.Length} bytes)");

            // Apply the license using Tobii Research API
            LicenseKey licenseKey = new LicenseKey(licenseBytes);
            LicenseCollection licenseCollection = new LicenseCollection(new[] { licenseKey });

            tracker.TryApplyLicenses(licenseCollection, out var results);

            if (results.Length > 0)
            {
                bool isValid = results[0].ValidationResult == LicenseValidationResult.Ok;
                if (isValid)
                {
                    Console.WriteLine("✅ License successfully applied and validated");
                }
                else
                {
                    Console.WriteLine($"❌ License application failed: {results[0].ValidationResult}");
                }
            }
            else
            {
                Console.WriteLine("⚠️  No license validation results returned");
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("⚠️  License file 'liscence' not found - continuing without license");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error applying license: {ex.Message}");
            Console.WriteLine("⚠️  Continuing without license...");
        }
    }

    private void OnGazeDataReceived(object? sender, GazeDataEventArgs e)
    {
        if (!_isTracking) return;

        var gazeData = e.GazeData;

        Console.WriteLine($"👁️  GAZE DATA [{DateTime.Now:HH:mm:ss.fff}]");
        Console.WriteLine($"   ⏱️  Device Timestamp: {gazeData.DeviceTimeStamp}");
        Console.WriteLine($"   🔄 System Timestamp: {gazeData.SystemTimeStamp}");

        // Left Eye Data
        Console.WriteLine($"   👁️  LEFT EYE:");
        Console.WriteLine($"      📍 Gaze Point: ({gazeData.LeftEye.GazePoint.PositionOnDisplayArea.X:F4}, {gazeData.LeftEye.GazePoint.PositionOnDisplayArea.Y:F4})");
        Console.WriteLine($"      ✅ Gaze Validity: {gazeData.LeftEye.GazePoint.Validity}");
        Console.WriteLine($"      👤 Pupil Diameter: {gazeData.LeftEye.Pupil.PupilDiameter:F2} mm");
        Console.WriteLine($"      ✅ Pupil Validity: {gazeData.LeftEye.Pupil.Validity}");
        Console.WriteLine($"      📐 Gaze Origin: ({gazeData.LeftEye.GazeOrigin.PositionInUserCoordinates.X:F2}, {gazeData.LeftEye.GazeOrigin.PositionInUserCoordinates.Y:F2}, {gazeData.LeftEye.GazeOrigin.PositionInUserCoordinates.Z:F2})");
        Console.WriteLine($"      ✅ Origin Validity: {gazeData.LeftEye.GazeOrigin.Validity}");

        // Right Eye Data
        Console.WriteLine($"   👁️  RIGHT EYE:");
        Console.WriteLine($"      📍 Gaze Point: ({gazeData.RightEye.GazePoint.PositionOnDisplayArea.X:F4}, {gazeData.RightEye.GazePoint.PositionOnDisplayArea.Y:F4})");
        Console.WriteLine($"      ✅ Gaze Validity: {gazeData.RightEye.GazePoint.Validity}");
        Console.WriteLine($"      👤 Pupil Diameter: {gazeData.RightEye.Pupil.PupilDiameter:F2} mm");
        Console.WriteLine($"      ✅ Pupil Validity: {gazeData.RightEye.Pupil.Validity}");
        Console.WriteLine($"      📐 Gaze Origin: ({gazeData.RightEye.GazeOrigin.PositionInUserCoordinates.X:F2}, {gazeData.RightEye.GazeOrigin.PositionInUserCoordinates.Y:F2}, {gazeData.RightEye.GazeOrigin.PositionInUserCoordinates.Z:F2})");
        Console.WriteLine($"      ✅ Origin Validity: {gazeData.RightEye.GazeOrigin.Validity}");

        Console.WriteLine("" + new string('-', 80));
    }

    public void StopEyeTracking()
    {
        if (_currentTracker != null && _isTracking)
        {
            Console.WriteLine("🛑 Stopping eye tracking...");
            _currentTracker.GazeDataReceived -= OnGazeDataReceived;
            _isTracking = false;
            Console.WriteLine("✅ Eye tracking stopped successfully");
        }
    }

#else
    // Mock implementation for non-Windows platforms
    public async Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers()
    {
        await Task.Delay(100); // Simulate async operation
        return new List<EyeTrackerDevice>
        {
            new EyeTrackerDevice
            {
                Name = "Mock Tobii Eye Tracker",
                SerialNumber = "MOCK-001",
                Model = "Tobii Pro X3-120"
            }
        };
    }

    public async Task StartEyeTracking()
    {
        Console.WriteLine("=== Mock Eye Tracker Manager - Starting Eye Tracking ===");
        Console.WriteLine("⚠️  Running on non-Windows platform - using mock implementation");
        await Task.Delay(100);
        Console.WriteLine("✅ Mock eye tracking started successfully");
    }

    public void StopEyeTracking()
    {
        Console.WriteLine("🛑 Mock eye tracking stopped");
    }
#endif
}