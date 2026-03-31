using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SpeedoMeter.Services;

namespace SpeedoMeter;

public partial class MainWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private TelemetrySnapshot _currentSnapshot = TelemetrySnapshot.Empty;
    private bool _loadingSettingsUi;

    public bool AllowClose { get; set; }

    public MainWindow(DatabaseService databaseService, SettingsService settingsService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _settingsService = settingsService;
        LoadSettingsUi();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        RefreshAll();
    }

    public void UpdateTelemetry(TelemetrySnapshot snapshot)
    {
        _currentSnapshot = snapshot;
        DownloadSpeedText.Text = SpeedFormatter.Format(snapshot.DownloadSpeed);
        UploadSpeedText.Text = SpeedFormatter.Format(snapshot.UploadSpeed);

        UpdateTodayUsage();
        RefreshAdapters();
    }

    public void RefreshAll()
    {
        RefreshHistory();
        UpdateTodayUsage();
        RefreshAdapters();
        LoadSettingsUi();
    }

    public void RefreshHistory()
    {
        HistoryListView.ItemsSource = _databaseService.GetUsageHistory(30);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (AllowClose)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void UpdateTodayUsage()
    {
        var (downloadedBytes, uploadedBytes) = _databaseService.GetTodayUsage();
        TodayDownloadText.Text = SpeedFormatter.FormatSize(downloadedBytes);
        TodayUploadText.Text = SpeedFormatter.FormatSize(uploadedBytes);
    }

    private void RefreshAdapters()
    {
        var currentAdapters = _currentSnapshot.Adapters.ToDictionary(adapter => adapter.Id, StringComparer.OrdinalIgnoreCase);
        var todayUsage = _databaseService.GetTodayAdapterUsage();
        var rows = new List<AdapterViewRow>();

        foreach (var record in todayUsage)
        {
            currentAdapters.TryGetValue(record.AdapterId, out var liveAdapter);
            rows.Add(new AdapterViewRow
            {
                Id = record.AdapterId,
                Name = record.AdapterName,
                Type = record.AdapterType,
                LiveDownloadSpeed = liveAdapter?.DownloadSpeed ?? 0,
                LiveUploadSpeed = liveAdapter?.UploadSpeed ?? 0,
                TodayDownloadBytes = record.BytesDownloaded,
                TodayUploadBytes = record.BytesUploaded
            });
        }

        foreach (var liveAdapter in _currentSnapshot.Adapters)
        {
            if (rows.Any(row => string.Equals(row.Id, liveAdapter.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            rows.Add(new AdapterViewRow
            {
                Id = liveAdapter.Id,
                Name = liveAdapter.Name,
                Type = liveAdapter.Type,
                LiveDownloadSpeed = liveAdapter.DownloadSpeed,
                LiveUploadSpeed = liveAdapter.UploadSpeed,
                TodayDownloadBytes = 0,
                TodayUploadBytes = 0
            });
        }

        AdapterListView.ItemsSource = rows
            .OrderByDescending(row => row.LiveDownloadSpeed + row.LiveUploadSpeed)
            .ThenByDescending(row => row.TodayDownloadBytes + row.TodayUploadBytes)
            .ThenBy(row => row.Name)
            .ToList();

        RefreshWidgetAdapterOptions(rows);
    }

    private void LoadSettingsUi()
    {
        _settingsService.Reload();
        _loadingSettingsUi = true;

        foreach (var item in WidgetModeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), _settingsService.WidgetMode, StringComparison.OrdinalIgnoreCase))
            {
                WidgetModeComboBox.SelectedItem = item;
                break;
            }
        }

        WidgetAdapterComboBox.SelectedValue = _settingsService.SelectedAdapterId;
        _loadingSettingsUi = false;
        ApplyWidgetSettingsState();
    }

    private void RefreshWidgetAdapterOptions(IEnumerable<AdapterViewRow> rows)
    {
        var options = rows
            .Select(row => new AdapterCatalogRecord
            {
                Id = row.Id,
                Name = row.Name,
                Type = row.Type
            })
            .Concat(_databaseService.GetKnownAdapters())
            .GroupBy(adapter => adapter.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(adapter => adapter.Name)
            .ToList();

        _loadingSettingsUi = true;
        WidgetAdapterComboBox.ItemsSource = options;
        WidgetAdapterComboBox.SelectedValue = _settingsService.SelectedAdapterId;

        if (WidgetAdapterComboBox.SelectedItem == null && options.Count > 0 && _settingsService.WidgetMode == SettingsService.WidgetModeSelectedAdapter)
        {
            WidgetAdapterComboBox.SelectedIndex = 0;
            if (WidgetAdapterComboBox.SelectedValue is string selectedAdapterId)
            {
                _settingsService.SetSelectedAdapterId(selectedAdapterId);
            }
        }

        _loadingSettingsUi = false;
        ApplyWidgetSettingsState();
    }

    private void WidgetModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettingsUi)
        {
            return;
        }

        if (WidgetModeComboBox.SelectedItem is ComboBoxItem item)
        {
            _settingsService.SetWidgetMode(item.Tag?.ToString());
        }

        ApplyWidgetSettingsState();
    }

    private void WidgetAdapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettingsUi)
        {
            return;
        }

        _settingsService.SetSelectedAdapterId(WidgetAdapterComboBox.SelectedValue?.ToString());
    }

    private void ApplyWidgetSettingsState()
    {
        WidgetAdapterComboBox.IsEnabled = _settingsService.WidgetMode == SettingsService.WidgetModeSelectedAdapter;
    }

    private sealed class AdapterViewRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long LiveDownloadSpeed { get; set; }
        public long LiveUploadSpeed { get; set; }
        public long TodayDownloadBytes { get; set; }
        public long TodayUploadBytes { get; set; }

        public string LiveDownloaded => SpeedFormatter.Format(LiveDownloadSpeed);
        public string LiveUploaded => SpeedFormatter.Format(LiveUploadSpeed);
        public string TodayDownloaded => SpeedFormatter.FormatSize(TodayDownloadBytes);
        public string TodayUploaded => SpeedFormatter.FormatSize(TodayUploadBytes);
    }
}
