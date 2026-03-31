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

    public IReadOnlyList<ProcessNetworkInfo> Processes { get; init; } = Array.Empty<ProcessNetworkInfo>();

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

public sealed class ProcessNetworkInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public string ProcessPath { get; set; } = string.Empty;
    public int ConnectionCount { get; set; }
}

public sealed class AlertRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public long ThresholdBytes { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class AlertHistoryEntry
{
    public string RuleId { get; set; } = string.Empty;
    public string TriggeredUtc { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string TriggeredLocal => DateTime.TryParse(TriggeredUtc, out var dt)
        ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        : TriggeredUtc;
}