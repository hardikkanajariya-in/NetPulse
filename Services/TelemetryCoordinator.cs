using System;
using System.Collections.Generic;

namespace SpeedoMeter.Services;

public sealed class TelemetryCoordinator
{
    private readonly NetworkMonitor _networkMonitor;
    private readonly DatabaseService _databaseService;
    private readonly ProcessTracker _processTracker;
    private readonly AlertService? _alertService;
    private DateTime _lastFlushUtc = DateTime.UtcNow;
    private int _processTickCounter;
    private IReadOnlyList<ProcessNetworkInfo> _lastProcesses = Array.Empty<ProcessNetworkInfo>();

    public TelemetryCoordinator(
        NetworkMonitor networkMonitor,
        DatabaseService databaseService,
        ProcessTracker processTracker,
        AlertService? alertService = null)
    {
        _networkMonitor = networkMonitor;
        _databaseService = databaseService;
        _processTracker = processTracker;
        _alertService = alertService;
    }

    public TelemetrySnapshot CurrentSnapshot { get; private set; } = TelemetrySnapshot.Empty;

    public TelemetrySnapshot Tick()
    {
        var networkSnapshot = _networkMonitor.Update();
        _databaseService.AccumulateTelemetry(networkSnapshot);

        _processTickCounter++;
        if (_processTickCounter >= 5)
        {
            _processTickCounter = 0;
            try { _lastProcesses = _processTracker.Poll(); }
            catch { /* graceful fallback to last known */ }
        }

        CurrentSnapshot = networkSnapshot with { Processes = _lastProcesses };

        if ((DateTime.UtcNow - _lastFlushUtc).TotalSeconds >= 60)
        {
            _databaseService.FlushToDatabase();
            _lastFlushUtc = DateTime.UtcNow;
        }

        if (_alertService != null)
        {
            var (todayDl, todayUl) = _databaseService.GetTodayUsage();
            _alertService.Evaluate(CurrentSnapshot, todayDl, todayUl);
        }

        return CurrentSnapshot;
    }

    public void FlushPending()
    {
        _databaseService.FlushToDatabase();
        _lastFlushUtc = DateTime.UtcNow;
    }
}