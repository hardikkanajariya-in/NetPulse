using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SpeedoMeter.Services;

namespace SpeedoMeter;

public partial class MeterWindow : Window
{
    public event EventHandler? DashboardRequested;

    public bool AllowClose { get; set; }

    public MeterWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PositionBottomRight();
        SourceInitialized += (_, _) => InitializeDesktopWidget();
        LocationChanged += (_, _) => KeepInsideWorkArea();
        Deactivated += (_, _) => DesktopWidgetHost.SendToDesktop(this);
    }

    public void UpdateTelemetry(TelemetrySnapshot snapshot, SettingsService settingsService)
    {
        string sourceLabel = "Total traffic";
        long downloadSpeed = snapshot.DownloadSpeed;
        long uploadSpeed = snapshot.UploadSpeed;

        switch (settingsService.WidgetMode)
        {
            case SettingsService.WidgetModeTopAdapter:
            {
                var topAdapter = snapshot.TopAdapter;
                sourceLabel = topAdapter?.Name ?? "No active adapter";
                downloadSpeed = topAdapter?.DownloadSpeed ?? 0;
                uploadSpeed = topAdapter?.UploadSpeed ?? 0;
                break;
            }
            case SettingsService.WidgetModeSelectedAdapter:
            {
                var selectedAdapter = snapshot.Adapters.FirstOrDefault(adapter => adapter.Id == settingsService.SelectedAdapterId);
                sourceLabel = selectedAdapter?.Name ?? "Selected adapter";
                downloadSpeed = selectedAdapter?.DownloadSpeed ?? 0;
                uploadSpeed = selectedAdapter?.UploadSpeed ?? 0;
                break;
            }
        }

        SourceText.Text = sourceLabel;
        DownloadSpeedText.Text = SpeedFormatter.Format(downloadSpeed);
        UploadSpeedText.Text = SpeedFormatter.Format(uploadSpeed);
    }

    private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            DashboardRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            DesktopWidgetHost.SendToDesktop(this);
        }
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

    private void PositionBottomRight()
    {
        Rect workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;
    }

    private void InitializeDesktopWidget()
    {
        PositionBottomRight();
        DesktopWidgetHost.Attach(this);
    }

    private void KeepInsideWorkArea()
    {
        Rect workArea = SystemParameters.WorkArea;

        if (Left < workArea.Left)
        {
            Left = workArea.Left;
        }

        if (Top < workArea.Top)
        {
            Top = workArea.Top;
        }

        if (Left + Width > workArea.Right)
        {
            Left = workArea.Right - Width;
        }

        if (Top + Height > workArea.Bottom)
        {
            Top = workArea.Bottom - Height;
        }
    }
}