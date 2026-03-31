using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SpeedoMeter.Services;
using Forms = System.Windows.Forms;

namespace SpeedoMeter;

public partial class App : Application
{
    private Forms.NotifyIcon _trayIcon = null!;
    private Icon _trayAppIcon = null!;
    private NetworkMonitor _networkMonitor = null!;
    private DatabaseService _databaseService = null!;
    private SettingsService _settingsService = null!;
    private StartupManager _startupManager = null!;
    private ProcessTracker _processTracker = null!;
    private AlertService _alertService = null!;
    private ThemeManager _themeManager = null!;
    private TelemetryCoordinator _telemetryCoordinator = null!;
    private DispatcherTimer _timer = null!;
    private MainWindow? _dashboard;
    private MeterWindow? _meterWindow;
    private Mutex? _mutex;
    private Forms.ToolStripMenuItem _startWithWindowsItem = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, @"Global\NetPulseMutex", out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _databaseService = new DatabaseService();
        _settingsService = new SettingsService(_databaseService);
        _themeManager = new ThemeManager(_databaseService);
        _themeManager.Initialize();
        _networkMonitor = new NetworkMonitor();
        _startupManager = new StartupManager();
        _processTracker = new ProcessTracker();
        _alertService = new AlertService(_databaseService);
        _telemetryCoordinator = new TelemetryCoordinator(
            _networkMonitor, _databaseService, _processTracker, _alertService);
        _startupManager.EnsureDefaultEnabled();

        _alertService.AlertTriggered += (title, message) =>
        {
            Dispatcher.Invoke(() =>
                _trayIcon.ShowBalloonTip(5000, title, message, Forms.ToolTipIcon.Warning));
        };

        CreateTrayIcon();
        CreateMeterWindow();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        Timer_Tick(this, EventArgs.Empty);
    }

    private void CreateMeterWindow()
    {
        _meterWindow = new MeterWindow();
        _meterWindow.DashboardRequested += (_, _) => ShowDashboard();
        _meterWindow.UpdateTelemetry(_telemetryCoordinator.CurrentSnapshot, _settingsService);
        _meterWindow.Show();
    }

    private void CreateTrayIcon()
    {
        _startWithWindowsItem = new Forms.ToolStripMenuItem("Start with Windows")
        {
            Checked = _startupManager.IsEnabled,
            CheckOnClick = true
        };
        _startWithWindowsItem.CheckedChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_startWithWindowsItem.Checked)
                    _startupManager.Enable();
                else
                    _startupManager.Disable();
            });
        };

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open Dashboard", null, (_, _) => Dispatcher.Invoke(ShowDashboard));
        contextMenu.Items.Add(_startWithWindowsItem);
        contextMenu.Items.Add("Clear All Records", null, (_, _) => Dispatcher.Invoke(ClearRecords));
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(ExitApp));

        _trayAppIcon = IconGenerator.CreateTrayIcon();

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayAppIcon,
            Text = "NetPulse",
            Visible = true,
            ContextMenuStrip = contextMenu
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                Dispatcher.Invoke(ShowDashboard);
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowDashboard);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        TelemetrySnapshot snapshot = _telemetryCoordinator.Tick();
        string down = SpeedFormatter.Format(snapshot.DownloadSpeed);
        string up = SpeedFormatter.Format(snapshot.UploadSpeed);

        string tooltip = snapshot.TopAdapter is null
            ? $"NetPulse ↓ {down} ↑ {up}"
            : $"{snapshot.TopAdapter.Name} ↓ {down} ↑ {up}";

        // NotifyIcon.Text max is 127 chars
        _trayIcon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;

        _dashboard?.UpdateTelemetry(snapshot);
        _meterWindow?.UpdateTelemetry(snapshot, _settingsService);
    }

    private void ShowDashboard()
    {
        if (_dashboard == null || !_dashboard.IsLoaded)
        {
            _dashboard = new MainWindow(_databaseService, _settingsService, _startupManager, _alertService, _themeManager);
            MainWindow = _dashboard;
        }

        _dashboard.UpdateTelemetry(_telemetryCoordinator.CurrentSnapshot);
        _dashboard.RefreshAll();

        _dashboard.Show();
        if (_dashboard.WindowState == WindowState.Minimized)
        {
            _dashboard.WindowState = WindowState.Normal;
        }

        _dashboard.ShowInTaskbar = true;
        _dashboard.Topmost = true;
        _dashboard.Activate();
        _dashboard.Topmost = false;
        _dashboard.Focus();
    }

    private void ClearRecords()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to clear all usage records?",
            "NetPulse",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _databaseService.ClearAllRecords();
            _dashboard?.RefreshAll();
        }
    }

    private void ExitApp()
    {
        _timer?.Stop();
        _telemetryCoordinator?.FlushPending();
        if (_dashboard != null)
        {
            _dashboard.AllowClose = true;
            _dashboard.Close();
        }
        if (_meterWindow != null)
        {
            _meterWindow.AllowClose = true;
            _meterWindow.Close();
        }
        _trayIcon.Visible = false;
        _trayIcon?.Dispose();
        _trayAppIcon?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer?.Stop();
        _telemetryCoordinator?.FlushPending();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        _trayAppIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
