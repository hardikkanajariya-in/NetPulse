namespace SpeedoMeter.Services;

public static class SpeedFormatter
{
    private const long KB = 1024;
    private const long MB = 1024 * 1024;
    private const long GB = 1024 * 1024 * 1024;

    public static string Format(long bytesPerSecond)
    {
        return FormatValue(bytesPerSecond, "/s");
    }

    public static string FormatSize(long bytes)
    {
        return FormatValue(bytes, string.Empty);
    }

    private static string FormatValue(long bytes, string suffix)
    {
        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F2} GB{suffix}",
            >= MB => $"{bytes / (double)MB:F2} MB{suffix}",
            >= KB => $"{bytes / (double)KB:F2} KB{suffix}",
            _ => $"{bytes} B{suffix}"
        };
    }
}
