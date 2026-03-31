using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SpeedoMeter.Services;

public static class ExportService
{
    public static string ExportDailyUsageCsv(List<UsageRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Downloaded (bytes),Uploaded (bytes),Downloaded,Uploaded");
        foreach (var r in records)
            sb.AppendLine($"{r.Date},{r.BytesDownloaded},{r.BytesUploaded},{r.Downloaded},{r.Uploaded}");
        return sb.ToString();
    }

    public static string ExportDailyUsageJson(List<UsageRecord> records)
    {
        var data = records.Select(r => new
        {
            r.Date,
            r.BytesDownloaded,
            r.BytesUploaded,
            DownloadedFormatted = r.Downloaded,
            UploadedFormatted = r.Uploaded
        });
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string ExportAdapterUsageCsv(List<AdapterDailyUsageRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Adapter ID,Adapter Name,Type,Downloaded (bytes),Uploaded (bytes),Downloaded,Uploaded");
        foreach (var r in records)
            sb.AppendLine($"{Escape(r.AdapterId)},{Escape(r.AdapterName)},{Escape(r.AdapterType)},{r.BytesDownloaded},{r.BytesUploaded},{r.Downloaded},{r.Uploaded}");
        return sb.ToString();
    }

    public static string ExportAdapterUsageJson(List<AdapterDailyUsageRecord> records)
    {
        var data = records.Select(r => new
        {
            r.AdapterId,
            r.AdapterName,
            r.AdapterType,
            r.BytesDownloaded,
            r.BytesUploaded,
            DownloadedFormatted = r.Downloaded,
            UploadedFormatted = r.Uploaded
        });
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    public static void SaveToFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
