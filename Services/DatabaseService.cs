using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SpeedoMeter.Services;

public sealed class DatabaseService
{
    private readonly string _dbPath;
    private long _sessionDownloaded;
    private long _sessionUploaded;
    private readonly object _lock = new();

    public DatabaseService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetPulse");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "usage.db");
        InitializeDatabase();
    }

    private string ConnectionString => $"Data Source={_dbPath}";

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS daily_usage (
                date TEXT PRIMARY KEY,
                bytes_downloaded INTEGER NOT NULL DEFAULT 0,
                bytes_uploaded INTEGER NOT NULL DEFAULT 0
            )";
        cmd.ExecuteNonQuery();
    }

    public void AccumulateBytes(long downloadedBytes, long uploadedBytes)
    {
        lock (_lock)
        {
            _sessionDownloaded += downloadedBytes;
            _sessionUploaded += uploadedBytes;
        }
    }

    public void FlushToDatabase()
    {
        long dl, ul;
        lock (_lock)
        {
            dl = _sessionDownloaded;
            ul = _sessionUploaded;
            _sessionDownloaded = 0;
            _sessionUploaded = 0;
        }

        if (dl == 0 && ul == 0) return;

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO daily_usage (date, bytes_downloaded, bytes_uploaded)
            VALUES (@date, @dl, @ul)
            ON CONFLICT(date) DO UPDATE SET
                bytes_downloaded = bytes_downloaded + @dl,
                bytes_uploaded = bytes_uploaded + @ul";
        cmd.Parameters.AddWithValue("@date", today);
        cmd.Parameters.AddWithValue("@dl", dl);
        cmd.Parameters.AddWithValue("@ul", ul);
        cmd.ExecuteNonQuery();
    }

    public (long Downloaded, long Uploaded) GetTodayUsage()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        long pendingDl, pendingUl;
        lock (_lock)
        {
            pendingDl = _sessionDownloaded;
            pendingUl = _sessionUploaded;
        }

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT bytes_downloaded, bytes_uploaded FROM daily_usage WHERE date = @date";
        cmd.Parameters.AddWithValue("@date", today);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt64(0) + pendingDl, reader.GetInt64(1) + pendingUl);
        }
        return (pendingDl, pendingUl);
    }

    public List<UsageRecord> GetUsageHistory(int days = 30)
    {
        var records = new List<UsageRecord>();

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT date, bytes_downloaded, bytes_uploaded
            FROM daily_usage
            ORDER BY date DESC
            LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", days);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new UsageRecord
            {
                Date = reader.GetString(0),
                BytesDownloaded = reader.GetInt64(1),
                BytesUploaded = reader.GetInt64(2)
            });
        }
        return records;
    }

    public void ClearAllRecords()
    {
        lock (_lock)
        {
            _sessionDownloaded = 0;
            _sessionUploaded = 0;
        }

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM daily_usage";
        cmd.ExecuteNonQuery();
    }
}

public class UsageRecord
{
    public string Date { get; set; } = "";
    public long BytesDownloaded { get; set; }
    public long BytesUploaded { get; set; }

    public string Downloaded => FormatBytes(BytesDownloaded);
    public string Uploaded => FormatBytes(BytesUploaded);

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
            >= 1024L * 1024 => $"{bytes / (1024.0 * 1024):F2} MB",
            >= 1024L => $"{bytes / 1024.0:F2} KB",
            _ => $"{bytes} B"
        };
    }
}
