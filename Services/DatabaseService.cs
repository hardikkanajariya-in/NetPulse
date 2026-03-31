using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SpeedoMeter.Services;

public sealed class DatabaseService
{
    private readonly string _dbPath;
    private long _sessionDownloaded;
    private long _sessionUploaded;
    private readonly Dictionary<string, PendingAdapterUsage> _pendingAdapterUsage = new(StringComparer.OrdinalIgnoreCase);
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

        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL;";
            pragma.ExecuteNonQuery();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS daily_usage (
                date TEXT PRIMARY KEY,
                bytes_downloaded INTEGER NOT NULL DEFAULT 0,
                bytes_uploaded INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS adapters (
                adapter_id TEXT PRIMARY KEY,
                adapter_name TEXT NOT NULL,
                adapter_type TEXT NOT NULL,
                last_seen_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS adapter_daily_usage (
                date TEXT NOT NULL,
                adapter_id TEXT NOT NULL,
                adapter_name TEXT NOT NULL,
                adapter_type TEXT NOT NULL,
                bytes_downloaded INTEGER NOT NULL DEFAULT 0,
                bytes_uploaded INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (date, adapter_id)
            );

            CREATE TABLE IF NOT EXISTS app_settings (
                setting_key TEXT PRIMARY KEY,
                setting_value TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_adapter_daily_usage_date ON adapter_daily_usage(date DESC);
            CREATE INDEX IF NOT EXISTS idx_adapters_last_seen ON adapters(last_seen_utc DESC);";
        cmd.ExecuteNonQuery();
    }

    public void AccumulateBytes(long downloadedBytes, long uploadedBytes)
    {
        AccumulateTelemetry(new TelemetrySnapshot(downloadedBytes, uploadedBytes, Array.Empty<AdapterTelemetry>()));
    }

    public void AccumulateTelemetry(TelemetrySnapshot snapshot)
    {
        lock (_lock)
        {
            _sessionDownloaded += snapshot.DownloadSpeed;
            _sessionUploaded += snapshot.UploadSpeed;

            foreach (var adapter in snapshot.Adapters)
            {
                if (!_pendingAdapterUsage.TryGetValue(adapter.Id, out var pending))
                {
                    pending = new PendingAdapterUsage
                    {
                        AdapterId = adapter.Id,
                        AdapterName = adapter.Name,
                        AdapterType = adapter.Type
                    };
                    _pendingAdapterUsage[adapter.Id] = pending;
                }

                pending.AdapterName = adapter.Name;
                pending.AdapterType = adapter.Type;
                pending.BytesDownloaded += adapter.DownloadSpeed;
                pending.BytesUploaded += adapter.UploadSpeed;
            }
        }
    }

    public void FlushToDatabase()
    {
        long downloadedBytes;
        long uploadedBytes;
        List<PendingAdapterUsage> pendingAdapters;

        lock (_lock)
        {
            downloadedBytes = _sessionDownloaded;
            uploadedBytes = _sessionUploaded;
            _sessionDownloaded = 0;
            _sessionUploaded = 0;
            pendingAdapters = _pendingAdapterUsage.Values
                .Select(usage => usage.Clone())
                .ToList();
            _pendingAdapterUsage.Clear();
        }

        if (downloadedBytes == 0 && uploadedBytes == 0 && pendingAdapters.Count == 0)
        {
            return;
        }

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string nowUtc = DateTime.UtcNow.ToString("O");

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();

        if (downloadedBytes != 0 || uploadedBytes != 0)
        {
            using var dailyUsageCommand = conn.CreateCommand();
            dailyUsageCommand.Transaction = transaction;
            dailyUsageCommand.CommandText = @"
                INSERT INTO daily_usage (date, bytes_downloaded, bytes_uploaded)
                VALUES (@date, @downloadedBytes, @uploadedBytes)
                ON CONFLICT(date) DO UPDATE SET
                    bytes_downloaded = bytes_downloaded + @downloadedBytes,
                    bytes_uploaded = bytes_uploaded + @uploadedBytes";
            dailyUsageCommand.Parameters.AddWithValue("@date", today);
            dailyUsageCommand.Parameters.AddWithValue("@downloadedBytes", downloadedBytes);
            dailyUsageCommand.Parameters.AddWithValue("@uploadedBytes", uploadedBytes);
            dailyUsageCommand.ExecuteNonQuery();
        }

        foreach (var adapter in pendingAdapters)
        {
            using var adapterCommand = conn.CreateCommand();
            adapterCommand.Transaction = transaction;
            adapterCommand.CommandText = @"
                INSERT INTO adapters (adapter_id, adapter_name, adapter_type, last_seen_utc)
                VALUES (@adapterId, @adapterName, @adapterType, @lastSeenUtc)
                ON CONFLICT(adapter_id) DO UPDATE SET
                    adapter_name = excluded.adapter_name,
                    adapter_type = excluded.adapter_type,
                    last_seen_utc = excluded.last_seen_utc;";
            adapterCommand.Parameters.AddWithValue("@adapterId", adapter.AdapterId);
            adapterCommand.Parameters.AddWithValue("@adapterName", adapter.AdapterName);
            adapterCommand.Parameters.AddWithValue("@adapterType", adapter.AdapterType);
            adapterCommand.Parameters.AddWithValue("@lastSeenUtc", nowUtc);
            adapterCommand.ExecuteNonQuery();

            if (adapter.BytesDownloaded == 0 && adapter.BytesUploaded == 0)
            {
                continue;
            }

            using var adapterUsageCommand = conn.CreateCommand();
            adapterUsageCommand.Transaction = transaction;
            adapterUsageCommand.CommandText = @"
                INSERT INTO adapter_daily_usage (date, adapter_id, adapter_name, adapter_type, bytes_downloaded, bytes_uploaded)
                VALUES (@date, @adapterId, @adapterName, @adapterType, @downloadedBytes, @uploadedBytes)
                ON CONFLICT(date, adapter_id) DO UPDATE SET
                    adapter_name = excluded.adapter_name,
                    adapter_type = excluded.adapter_type,
                    bytes_downloaded = bytes_downloaded + @downloadedBytes,
                    bytes_uploaded = bytes_uploaded + @uploadedBytes;";
            adapterUsageCommand.Parameters.AddWithValue("@date", today);
            adapterUsageCommand.Parameters.AddWithValue("@adapterId", adapter.AdapterId);
            adapterUsageCommand.Parameters.AddWithValue("@adapterName", adapter.AdapterName);
            adapterUsageCommand.Parameters.AddWithValue("@adapterType", adapter.AdapterType);
            adapterUsageCommand.Parameters.AddWithValue("@downloadedBytes", adapter.BytesDownloaded);
            adapterUsageCommand.Parameters.AddWithValue("@uploadedBytes", adapter.BytesUploaded);
            adapterUsageCommand.ExecuteNonQuery();
        }

        transaction.Commit();
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

    public List<AdapterDailyUsageRecord> GetTodayAdapterUsage()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        Dictionary<string, PendingAdapterUsage> pending;

        lock (_lock)
        {
            pending = _pendingAdapterUsage.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone(),
                StringComparer.OrdinalIgnoreCase);
        }

        var records = new Dictionary<string, AdapterDailyUsageRecord>(StringComparer.OrdinalIgnoreCase);

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT adapter_id, adapter_name, adapter_type, bytes_downloaded, bytes_uploaded
            FROM adapter_daily_usage
            WHERE date = @date";
        cmd.Parameters.AddWithValue("@date", today);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var record = new AdapterDailyUsageRecord
            {
                AdapterId = reader.GetString(0),
                AdapterName = reader.GetString(1),
                AdapterType = reader.GetString(2),
                BytesDownloaded = reader.GetInt64(3),
                BytesUploaded = reader.GetInt64(4)
            };

            records[record.AdapterId] = record;
        }

        foreach (var pendingUsage in pending.Values)
        {
            if (records.TryGetValue(pendingUsage.AdapterId, out var existing))
            {
                existing.AdapterName = pendingUsage.AdapterName;
                existing.AdapterType = pendingUsage.AdapterType;
                existing.BytesDownloaded += pendingUsage.BytesDownloaded;
                existing.BytesUploaded += pendingUsage.BytesUploaded;
                continue;
            }

            records[pendingUsage.AdapterId] = new AdapterDailyUsageRecord
            {
                AdapterId = pendingUsage.AdapterId,
                AdapterName = pendingUsage.AdapterName,
                AdapterType = pendingUsage.AdapterType,
                BytesDownloaded = pendingUsage.BytesDownloaded,
                BytesUploaded = pendingUsage.BytesUploaded
            };
        }

        return records.Values
            .OrderByDescending(record => record.BytesDownloaded + record.BytesUploaded)
            .ThenBy(record => record.AdapterName)
            .ToList();
    }

    public List<AdapterCatalogRecord> GetKnownAdapters()
    {
        var records = new List<AdapterCatalogRecord>();

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT adapter_id, adapter_name, adapter_type
            FROM adapters
            ORDER BY last_seen_utc DESC, adapter_name ASC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new AdapterCatalogRecord
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2)
            });
        }

        return records;
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
            _pendingAdapterUsage.Clear();
        }

        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM daily_usage;
            DELETE FROM adapter_daily_usage;";
        cmd.ExecuteNonQuery();
    }

    public string GetSettingString(string key, string defaultValue)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT setting_value FROM app_settings WHERE setting_key = @key";
        cmd.Parameters.AddWithValue("@key", key);

        object? result = cmd.ExecuteScalar();
        return result?.ToString() ?? defaultValue;
    }

    public void SetSettingString(string key, string value)
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO app_settings (setting_key, setting_value)
            VALUES (@key, @value)
            ON CONFLICT(setting_key) DO UPDATE SET
                setting_value = excluded.setting_value";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    private sealed class PendingAdapterUsage
    {
        public string AdapterId { get; set; } = string.Empty;
        public string AdapterName { get; set; } = string.Empty;
        public string AdapterType { get; set; } = string.Empty;
        public long BytesDownloaded { get; set; }
        public long BytesUploaded { get; set; }

        public PendingAdapterUsage Clone()
        {
            return new PendingAdapterUsage
            {
                AdapterId = AdapterId,
                AdapterName = AdapterName,
                AdapterType = AdapterType,
                BytesDownloaded = BytesDownloaded,
                BytesUploaded = BytesUploaded
            };
        }
    }
}

public class UsageRecord
{
    public string Date { get; set; } = "";
    public long BytesDownloaded { get; set; }
    public long BytesUploaded { get; set; }

    public string Downloaded => SpeedFormatter.FormatSize(BytesDownloaded);
    public string Uploaded => SpeedFormatter.FormatSize(BytesUploaded);
}
