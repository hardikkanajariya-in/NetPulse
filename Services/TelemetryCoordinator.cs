using System;

namespace SpeedoMeter.Services;

public sealed class TelemetryCoordinator
{
    private readonly NetworkMonitor _networkMonitor;
    private readonly DatabaseService _databaseService;
    private DateTime _lastFlushUtc = DateTime.UtcNow;

    public TelemetryCoordinator(NetworkMonitor networkMonitor, DatabaseService databaseService)
    {
        _networkMonitor = networkMonitor;
        _databaseService = databaseService;
    }

    public TelemetrySnapshot CurrentSnapshot { get; private set; } = TelemetrySnapshot.Empty;

    public TelemetrySnapshot Tick()
    {
        CurrentSnapshot = _networkMonitor.Update();
        _databaseService.AccumulateTelemetry(CurrentSnapshot);

        if ((DateTime.UtcNow - _lastFlushUtc).TotalSeconds >= 60)
        {
            _databaseService.FlushToDatabase();
            _lastFlushUtc = DateTime.UtcNow;
        }

        return CurrentSnapshot;
    }

    public void FlushPending()
    {
        _databaseService.FlushToDatabase();
        _lastFlushUtc = DateTime.UtcNow;
    }
}