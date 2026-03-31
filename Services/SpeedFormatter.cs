namespace SpeedoMeter.Services;

public static class SpeedFormatter
{
    private const long KB = 1024;
    private const long MB = 1024 * 1024;
    private const long GB = 1024 * 1024 * 1024;

    public static string Format(long bytesPerSecond)
    {
        return bytesPerSecond switch
        {
            >= GB => $"{bytesPerSecond / (double)GB:F2} GB/s",
            >= MB => $"{bytesPerSecond / (double)MB:F2} MB/s",
            >= KB => $"{bytesPerSecond / (double)KB:F2} KB/s",
            _ => $"{bytesPerSecond} B/s"
        };
    }
}
