namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public sealed class CalibrationOptions
{
    public const string SectionName = "Calibration";

    public int PresetPointCount { get; set; } = 9;

    public string GetPatternName()
    {
        return PresetPointCount switch
        {
            9 => CalibrationPatterns.ScreenBasedNinePoint,
            13 => CalibrationPatterns.ScreenBasedThirteenPoint,
            16 => CalibrationPatterns.ScreenBasedSixteenPoint,
            _ => throw new InvalidOperationException(
                $"Unsupported calibration preset '{PresetPointCount}'. Supported values are 9, 13, and 16.")
        };
    }

    public IReadOnlyList<CalibrationPointDefinition> GetPointDefinitions()
    {
        return PresetPointCount switch
        {
            9 => CalibrationPresets.NinePoint,
            13 => CalibrationPresets.ThirteenPoint,
            16 => CalibrationPresets.SixteenPoint,
            _ => throw new InvalidOperationException(
                $"Unsupported calibration preset '{PresetPointCount}'. Supported values are 9, 13, and 16.")
        };
    }
}

public static class CalibrationPresets
{
    // These points are biased toward the centered reading column used by ReaderShell
    // rather than the full screen margins. That gives the tracker more samples where
    // gaze is expected during reading.
    public static readonly IReadOnlyList<CalibrationPointDefinition> NinePoint =
    [
        new("center", "Center", 0.5f, 0.5f),
        new("top-left", "Top left", 0.27f, 0.22f),
        new("top-center", "Top center", 0.5f, 0.22f),
        new("top-right", "Top right", 0.73f, 0.22f),
        new("right-center", "Right center", 0.73f, 0.5f),
        new("bottom-right", "Bottom right", 0.73f, 0.78f),
        new("bottom-center", "Bottom center", 0.5f, 0.78f),
        new("bottom-left", "Bottom left", 0.27f, 0.78f),
        new("left-center", "Left center", 0.27f, 0.5f),
    ];

    public static readonly IReadOnlyList<CalibrationPointDefinition> ThirteenPoint =
    [
        ..NinePoint,
        new("upper-inner-left", "Upper inner left", 0.38f, 0.36f),
        new("upper-inner-right", "Upper inner right", 0.62f, 0.36f),
        new("lower-inner-right", "Lower inner right", 0.62f, 0.64f),
        new("lower-inner-left", "Lower inner left", 0.38f, 0.64f),
    ];

    public static readonly IReadOnlyList<CalibrationPointDefinition> SixteenPoint =
    [
        new("grid-r1-c1", "Row 1 column 1", 0.27f, 0.22f),
        new("grid-r1-c2", "Row 1 column 2", 0.42f, 0.22f),
        new("grid-r1-c3", "Row 1 column 3", 0.58f, 0.22f),
        new("grid-r1-c4", "Row 1 column 4", 0.73f, 0.22f),
        new("grid-r2-c1", "Row 2 column 1", 0.27f, 0.41f),
        new("grid-r2-c2", "Row 2 column 2", 0.42f, 0.41f),
        new("grid-r2-c3", "Row 2 column 3", 0.58f, 0.41f),
        new("grid-r2-c4", "Row 2 column 4", 0.73f, 0.41f),
        new("grid-r3-c1", "Row 3 column 1", 0.27f, 0.59f),
        new("grid-r3-c2", "Row 3 column 2", 0.42f, 0.59f),
        new("grid-r3-c3", "Row 3 column 3", 0.58f, 0.59f),
        new("grid-r3-c4", "Row 3 column 4", 0.73f, 0.59f),
        new("grid-r4-c1", "Row 4 column 1", 0.27f, 0.78f),
        new("grid-r4-c2", "Row 4 column 2", 0.42f, 0.78f),
        new("grid-r4-c3", "Row 4 column 3", 0.58f, 0.78f),
        new("grid-r4-c4", "Row 4 column 4", 0.73f, 0.78f),
    ];
}
