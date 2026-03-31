using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpeedoMeter.Services;

namespace SpeedoMeter;

public partial class MainWindow : Window
{
    private readonly DatabaseService _db;
    private readonly SettingsService _settings;
    private readonly StartupManager _startupManager;
    private readonly AlertService _alertService;
    private TelemetrySnapshot _currentSnapshot = TelemetrySnapshot.Empty;

    private readonly long[] _dlHistory = new long[60];
    private readonly long[] _ulHistory = new long[60];
    private int _chartIdx;
    private int _chartCount;

    private string _currentPage = "Overview";
    private bool _loadingSettings;
    private int _historyDays = 7;

    public bool AllowClose { get; set; }

    public MainWindow(DatabaseService db, SettingsService settings,
                      StartupManager startupManager, AlertService alertService)
    {
        InitializeComponent();
        _db = db;
        _settings = settings;
        _startupManager = startupManager;
        _alertService = alertService;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        LoadSettingsUi();
    }

    // ── Navigation ──

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        if (sender is RadioButton rb && rb.Tag is string page)
            ShowPage(page);
    }

    private void ShowPage(string page)
    {
        _currentPage = page;
        if (OverviewPage == null) return;

        OverviewPage.Visibility = page == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        LivePage.Visibility = page == "Live" ? Visibility.Visible : Visibility.Collapsed;
        AdaptersPage.Visibility = page == "Adapters" ? Visibility.Visible : Visibility.Collapsed;
        ApplicationsPage.Visibility = page == "Applications" ? Visibility.Visible : Visibility.Collapsed;
        HistoryPage.Visibility = page == "History" ? Visibility.Visible : Visibility.Collapsed;
        AlertsPage.Visibility = page == "Alerts" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;

        switch (page)
        {
            case "Overview": UpdateOverviewSummary(); UpdateMiniChart(); break;
            case "Adapters": RefreshAdapters(); break;
            case "Applications": RefreshProcesses(); break;
            case "History": RefreshHistory(); break;
            case "Alerts": RefreshAlerts(); break;
            case "Settings": LoadSettingsUi(); break;
        }
    }

    // ── Telemetry ──

    public void UpdateTelemetry(TelemetrySnapshot snapshot)
    {
        _currentSnapshot = snapshot;

        DownloadSpeedText.Text = SpeedFormatter.Format(snapshot.DownloadSpeed);
        UploadSpeedText.Text = SpeedFormatter.Format(snapshot.UploadSpeed);
        UpdateTodayUsage();

        _dlHistory[_chartIdx] = snapshot.DownloadSpeed;
        _ulHistory[_chartIdx] = snapshot.UploadSpeed;
        _chartIdx = (_chartIdx + 1) % 60;
        _chartCount = Math.Min(_chartCount + 1, 60);

        switch (_currentPage)
        {
            case "Overview":
                UpdateMiniChart();
                UpdateOverviewSummary();
                break;
            case "Live":
                UpdateLiveChart();
                LiveDownloadText.Text = SpeedFormatter.Format(snapshot.DownloadSpeed);
                LiveUploadText.Text = SpeedFormatter.Format(snapshot.UploadSpeed);
                break;
            case "Adapters":
                RefreshAdapters();
                break;
            case "Applications":
                RefreshProcesses();
                break;
        }
    }

    public void RefreshAll()
    {
        UpdateTodayUsage();
        switch (_currentPage)
        {
            case "History": RefreshHistory(); break;
            case "Adapters": RefreshAdapters(); break;
            case "Alerts": RefreshAlerts(); break;
            case "Settings": LoadSettingsUi(); break;
        }
    }

    // ── Overview ──

    private void UpdateTodayUsage()
    {
        var (dl, ul) = _db.GetTodayUsage();
        TodayDownloadText.Text = SpeedFormatter.FormatSize(dl);
        TodayUploadText.Text = SpeedFormatter.FormatSize(ul);
    }

    private void UpdateOverviewSummary()
    {
        int ac = _currentSnapshot.Adapters.Count;
        ActiveAdaptersText.Text = $"{ac} adapter{(ac != 1 ? "s" : "")}";
        var top = _currentSnapshot.TopAdapter;
        TopAdapterText.Text = top != null
            ? $"Top: {top.Name} ({SpeedFormatter.Format(top.TotalSpeed)})" : "";

        int pc = _currentSnapshot.Processes.Count;
        ActiveProcessesText.Text = $"{pc} process{(pc != 1 ? "es" : "")}";
        var tp = _currentSnapshot.Processes.FirstOrDefault();
        TopProcessText.Text = tp != null
            ? $"Top: {tp.ProcessName} ({tp.ConnectionCount} conn)" : "";
    }

    // ── Charts ──

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender == MiniChartCanvas) UpdateMiniChart();
        else if (sender == LiveChartCanvas) UpdateLiveChart();
    }

    private void UpdateMiniChart()
    {
        if (MiniChartCanvas == null) return;
        DrawChart(MiniChartCanvas, MiniDownloadLine, MiniUploadLine, MiniDownloadFill, MiniUploadFill);
    }

    private void UpdateLiveChart()
    {
        if (LiveChartCanvas == null) return;
        DrawChart(LiveChartCanvas, LiveDownloadLine, LiveUploadLine, LiveDownloadFill, LiveUploadFill);
        long max = GetChartMax();
        ChartMaxLabel.Text = SpeedFormatter.Format(max);
        ChartMidLabel.Text = SpeedFormatter.Format(max / 2);
    }

    private void DrawChart(Canvas canvas,
        System.Windows.Shapes.Polyline dlLine, System.Windows.Shapes.Polyline ulLine,
        System.Windows.Shapes.Polygon dlFill, System.Windows.Shapes.Polygon ulFill)
    {
        double w = canvas.ActualWidth;
        double h = canvas.ActualHeight;
        if (w <= 0 || h <= 0 || _chartCount < 2) return;

        long max = GetChartMax();
        var dlPts = new PointCollection();
        var ulPts = new PointCollection();
        double step = w / (_chartCount - 1);

        for (int i = 0; i < _chartCount; i++)
        {
            int idx = (_chartIdx - _chartCount + i + 60) % 60;
            double x = i * step;
            double dlY = h - (_dlHistory[idx] / (double)max) * (h - 4);
            double ulY = h - (_ulHistory[idx] / (double)max) * (h - 4);
            dlPts.Add(new Point(x, dlY));
            ulPts.Add(new Point(x, ulY));
        }

        dlLine.Points = dlPts;
        ulLine.Points = ulPts;

        var dlF = new PointCollection(dlPts);
        dlF.Add(new Point(dlPts[dlPts.Count - 1].X, h));
        dlF.Add(new Point(dlPts[0].X, h));
        dlFill.Points = dlF;

        var ulF = new PointCollection(ulPts);
        ulF.Add(new Point(ulPts[ulPts.Count - 1].X, h));
        ulF.Add(new Point(ulPts[0].X, h));
        ulFill.Points = ulF;
    }

    private long GetChartMax()
    {
        long max = 1024;
        for (int i = 0; i < _chartCount; i++)
        {
            max = Math.Max(max, _dlHistory[i]);
            max = Math.Max(max, _ulHistory[i]);
        }
        return (long)(max * 1.15);
    }

    // ── Adapters ──

    private void RefreshAdapters()
    {
        var live = _currentSnapshot.Adapters.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
        var today = _db.GetTodayAdapterUsage();
        var rows = new List<AdapterViewRow>();

        foreach (var rec in today)
        {
            live.TryGetValue(rec.AdapterId, out var la);
            rows.Add(new AdapterViewRow
            {
                Id = rec.AdapterId, Name = rec.AdapterName, Type = rec.AdapterType,
                LiveDownloadSpeed = la?.DownloadSpeed ?? 0, LiveUploadSpeed = la?.UploadSpeed ?? 0,
                TodayDownloadBytes = rec.BytesDownloaded, TodayUploadBytes = rec.BytesUploaded
            });
        }

        foreach (var la in _currentSnapshot.Adapters)
        {
            if (rows.Any(r => r.Id.Equals(la.Id, StringComparison.OrdinalIgnoreCase))) continue;
            rows.Add(new AdapterViewRow
            {
                Id = la.Id, Name = la.Name, Type = la.Type,
                LiveDownloadSpeed = la.DownloadSpeed, LiveUploadSpeed = la.UploadSpeed
            });
        }

        AdapterListView.ItemsSource = rows
            .OrderByDescending(r => r.LiveDownloadSpeed + r.LiveUploadSpeed)
            .ThenByDescending(r => r.TodayDownloadBytes + r.TodayUploadBytes)
            .ThenBy(r => r.Name).ToList();

        RefreshWidgetAdapterOptions(rows);
    }

    // ── Processes ──

    private void RefreshProcesses()
    {
        ProcessListView.ItemsSource = _currentSnapshot.Processes;
    }

    // ── History ──

    private void HistoryFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out int days))
        {
            _historyDays = days;
            RefreshHistory();
        }
    }

    private void RefreshHistory()
    {
        if (HistoryListView == null) return;
        HistoryListView.ItemsSource = _db.GetUsageHistory(_historyDays);
    }

    // ── Alerts ──

    private void RefreshAlerts()
    {
        if (AlertRulesPanel == null) return;
        AlertRulesPanel.ItemsSource = _alertService.Rules
            .Select(r => new AlertRuleView
            {
                RuleId = r.RuleId, RuleName = r.RuleName,
                TypeLabel = r.RuleType.Replace("-", " "),
                ThresholdLabel = SpeedFormatter.FormatSize(r.ThresholdBytes),
                Enabled = r.Enabled
            }).ToList();
        AlertHistoryListView.ItemsSource = _alertService.GetHistory();
    }

    private void AddAlert_Click(object sender, RoutedEventArgs e)
    {
        string name = AlertRuleNameInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) return;

        string type = (AlertRuleTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "daily-total";
        if (!long.TryParse(AlertThresholdInput.Text?.Trim(), out long mb) || mb <= 0) return;

        _alertService.AddRule(new AlertRule
        {
            RuleId = Guid.NewGuid().ToString("N")[..8],
            RuleName = name,
            RuleType = type,
            ThresholdBytes = mb * 1024 * 1024,
            Enabled = true
        });
        AlertRuleNameInput.Clear();
        AlertThresholdInput.Clear();
        RefreshAlerts();
    }

    private void DeleteAlert_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string ruleId)
        {
            _alertService.RemoveRule(ruleId);
            RefreshAlerts();
        }
    }

    private void AlertToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is AlertRuleView rule)
            _alertService.ToggleRule(rule.RuleId, cb.IsChecked == true);
    }

    // ── Settings ──

    private void LoadSettingsUi()
    {
        if (WidgetModeComboBox == null) return;
        _settings.Reload();
        _loadingSettings = true;

        foreach (var item in WidgetModeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), _settings.WidgetMode, StringComparison.OrdinalIgnoreCase))
            { WidgetModeComboBox.SelectedItem = item; break; }
        }
        WidgetAdapterComboBox.SelectedValue = _settings.SelectedAdapterId;
        StartWithWindowsCheckBox.IsChecked = _startupManager.IsEnabled;
        _loadingSettings = false;
        ApplyWidgetSettingsState();
    }

    private void RefreshWidgetAdapterOptions(IEnumerable<AdapterViewRow> rows)
    {
        var options = rows
            .Select(r => new AdapterCatalogRecord { Id = r.Id, Name = r.Name, Type = r.Type })
            .Concat(_db.GetKnownAdapters())
            .GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()).OrderBy(a => a.Name).ToList();

        _loadingSettings = true;
        WidgetAdapterComboBox.ItemsSource = options;
        WidgetAdapterComboBox.SelectedValue = _settings.SelectedAdapterId;
        if (WidgetAdapterComboBox.SelectedItem == null && options.Count > 0
            && _settings.WidgetMode == SettingsService.WidgetModeSelectedAdapter)
        {
            WidgetAdapterComboBox.SelectedIndex = 0;
            if (WidgetAdapterComboBox.SelectedValue is string id) _settings.SetSelectedAdapterId(id);
        }
        _loadingSettings = false;
        ApplyWidgetSettingsState();
    }

    private void WidgetModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        if (WidgetModeComboBox.SelectedItem is ComboBoxItem item)
            _settings.SetWidgetMode(item.Tag?.ToString());
        ApplyWidgetSettingsState();
    }

    private void WidgetAdapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSettings) return;
        _settings.SetSelectedAdapterId(WidgetAdapterComboBox.SelectedValue?.ToString());
    }

    private void ApplyWidgetSettingsState()
    {
        if (WidgetAdapterComboBox != null)
            WidgetAdapterComboBox.IsEnabled = _settings.WidgetMode == SettingsService.WidgetModeSelectedAdapter;
    }

    private void StartWithWindows_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;
        if (StartWithWindowsCheckBox.IsChecked == true) _startupManager.Enable();
        else _startupManager.Disable();
    }

    // ── Export ──

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"netpulse-{DateTime.Now:yyyy-MM-dd}"
        };
        if (dlg.ShowDialog() == true)
        {
            var records = _db.GetUsageHistory(9999);
            ExportService.SaveToFile(dlg.FileName, ExportService.ExportDailyUsageCsv(records));
        }
    }

    private void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = $"netpulse-{DateTime.Now:yyyy-MM-dd}"
        };
        if (dlg.ShowDialog() == true)
        {
            var records = _db.GetUsageHistory(9999);
            ExportService.SaveToFile(dlg.FileName, ExportService.ExportDailyUsageJson(records));
        }
    }

    private void ClearData_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Clear all usage records? This cannot be undone.",
            "NetPulse", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _db.ClearAllRecords();
            RefreshAll();
        }
    }

    // ── Lifecycle ──

    protected override void OnClosing(CancelEventArgs e)
    {
        if (AllowClose) { base.OnClosing(e); return; }
        e.Cancel = true;
        Hide();
    }

    // ── View Models ──

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

    private sealed class AlertRuleView
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = string.Empty;
        public string ThresholdLabel { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }
}
