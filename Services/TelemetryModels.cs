using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeedoMeter.Services;

public sealed record AdapterTelemetry(
    string Id,
    string Name,
    string Type,
    long DownloadSpeed,
    long UploadSpeed,
    string Description)
{
    public long TotalSpeed => DownloadSpeed + UploadSpeed;
}

public sealed record TelemetrySnapshot(
    long DownloadSpeed,
    long UploadSpeed,
    IReadOnlyList<AdapterTelemetry> Adapters)
{
    public static TelemetrySnapshot Empty { get; } = new(0, 0, Array.Empty<AdapterTelemetry>());

    public AdapterTelemetry? TopAdapter => Adapters
        .OrderByDescending(adapter => adapter.TotalSpeed)
        .FirstOrDefault();
}

public sealed class AdapterCatalogRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Type) ? Name : $"{Name} ({Type})";
}

public sealed class AdapterDailyUsageRecord
{
    public string AdapterId { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public string AdapterType { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long BytesUploaded { get; set; }

    public string Downloaded => SpeedFormatter.FormatSize(BytesDownloaded);
    public string Uploaded => SpeedFormatter.FormatSize(BytesUploaded);
}