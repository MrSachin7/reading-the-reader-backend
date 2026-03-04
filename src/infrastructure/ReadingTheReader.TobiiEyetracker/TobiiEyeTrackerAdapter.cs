#if WINDOWS
using Tobii.Research;
#endif
using ReadingTheReader.core.Application.InfrastructureContracts;
using ReadingTheReader.core.Domain;

namespace ReadingTheReader.TobiiEyetracker;

public class TobiiEyeTrackerAdapter : IEyeTrackerAdapter
{
    public event EventHandler<GazeData>? GazeDataReceived;

#if WINDOWS
    private IEyeTracker? _selectedTracker;
    private bool _isTracking;

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

    public Task SelectEyeTracker(string serialNumber, byte[] licenseFileBytes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("A serial number is required.", nameof(serialNumber));

        if (licenseFileBytes.Length == 0)
            throw new ArgumentException("A non-empty license file is required.", nameof(licenseFileBytes));

        var found = EyeTrackingOperations.FindAllEyeTrackers();
        var selected = found.FirstOrDefault(t =>
            t.SerialNumber.Equals(serialNumber, StringComparison.OrdinalIgnoreCase));

        if (selected is null)
            throw new ArgumentException($"No eye tracker found with serial number '{serialNumber}'.", nameof(serialNumber));

        try
        {
            var licenseKey = new LicenseKey(licenseFileBytes);
            var licenseCollection = new LicenseCollection([licenseKey]);
            selected.TryApplyLicenses(licenseCollection, out var result);

            var hasInvalidLicense = result is not null &&
                                    result.Any(r => r.ValidationResult != LicenseValidationResult.Ok);

            if (hasInvalidLicense)
                throw new ArgumentException("The provided license is not valid for the selected eye tracker.");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The provided license is not valid for the selected eye tracker.", ex);
        }

        _selectedTracker = selected;
        return Task.CompletedTask;
    }

    public Task StartEyeTracking()
    {
        if (_selectedTracker is null)
            throw new InvalidOperationException("No eye tracker selected. Select an eye tracker before starting.");

        if (_isTracking)
            return Task.CompletedTask;

        _selectedTracker.GazeDataReceived += OnTobiiGazeDataReceived;
        _isTracking = true;
        Console.WriteLine($"Tobii eye tracking started on '{_selectedTracker.Address}'");
        return Task.CompletedTask;
    }

    public void StopEyeTracking()
    {
        if (_selectedTracker is null || !_isTracking)
            return;

        _selectedTracker.GazeDataReceived -= OnTobiiGazeDataReceived;
        _isTracking = false;
        Console.WriteLine("Tobii eye tracking stopped");
    }

    private void OnTobiiGazeDataReceived(object? sender, GazeDataEventArgs e)
    {
        GazeDataReceived?.Invoke(this, new GazeData
        {
            DeviceTimeStamp = e.DeviceTimeStamp,
            LeftEyeX = e.LeftEye.GazePoint.PositionOnDisplayArea.X,
            LeftEyeY = e.LeftEye.GazePoint.PositionOnDisplayArea.Y,
            LeftEyeValidity = e.LeftEye.GazePoint.Validity.ToString(),
            RightEyeX = e.RightEye.GazePoint.PositionOnDisplayArea.X,
            RightEyeY = e.RightEye.GazePoint.PositionOnDisplayArea.Y,
            RightEyeValidity = e.RightEye.GazePoint.Validity.ToString()
        });
    }
#else
    private bool _isTrackerSelected;

    public Task<List<EyeTrackerDevice>> GetAllConnectedEyeTrackers()
    {
        return Task.FromResult(new List<EyeTrackerDevice>
        {
            new()
            {
                Name = "Mock Tobii Eye Tracker",
                SerialNumber = "MOCK-001",
                Model = "Tobii Pro X3-120"
            }
        });
    }

    public Task SelectEyeTracker(string serialNumber, byte[] licenseFileBytes, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("A serial number is required.", nameof(serialNumber));

        if (licenseFileBytes.Length == 0)
            throw new ArgumentException("A non-empty license file is required.", nameof(licenseFileBytes));

        _isTrackerSelected = true;
        return Task.CompletedTask;
    }

    public Task StartEyeTracking()
    {
        if (!_isTrackerSelected)
            throw new InvalidOperationException("No eye tracker selected. Select an eye tracker before starting.");

        Console.WriteLine("Tobii SDK not available on this platform. Running mock eye tracker.");
        Console.WriteLine("Mock eye tracking started");
        return Task.CompletedTask;
    }

    public void StopEyeTracking()
    {
        Console.WriteLine("Mock eye tracking stopped");
    }
#endif
}
