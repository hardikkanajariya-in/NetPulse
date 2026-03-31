using System;
using System.ComponentModel;
using System.Windows;
using SpeedoMeter.Services;

namespace SpeedoMeter;

public partial class MainWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly NetworkMonitor _networkMonitor;

    public MainWindow(DatabaseService databaseService, NetworkMonitor networkMonitor)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _networkMonitor = networkMonitor;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        RefreshHistory();
        UpdateTodayUsage();
    }

    public void UpdateSpeeds(long downloadSpeed, long uploadSpeed)
    {
        DownloadSpeedText.Text = SpeedFormatter.Format(downloadSpeed);
        UploadSpeedText.Text = SpeedFormatter.Format(uploadSpeed);
        UpdateTodayUsage();
    }

    private void UpdateTodayUsage()
    {
        var (dl, ul) = _databaseService.GetTodayUsage();
        TodayDownloadText.Text = FormatBytes(dl);
        TodayUploadText.Text = FormatBytes(ul);
    }

    public void RefreshHistory()
    {
        var history = _databaseService.GetUsageHistory(30);
        HistoryListView.ItemsSource = history;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

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
