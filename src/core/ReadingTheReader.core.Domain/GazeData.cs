namespace ReadingTheReader.core.Domain;

public class GazeData
{
    public long DeviceTimeStamp { get; set; }

    public float LeftEyeX { get; set; }
    public float LeftEyeY { get; set; }
    public string LeftEyeValidity { get; set; } = string.Empty;

    public float RightEyeX { get; set; }
    public float RightEyeY { get; set; }
    public string RightEyeValidity { get; set; } = string.Empty;
}

