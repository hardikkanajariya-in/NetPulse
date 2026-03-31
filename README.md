# NetPulse

**A feature-rich, real-time network speed monitor and usage tracker for Windows**

[![Build](https://github.com/hardikkanajariya/net-pulse/actions/workflows/build.yml/badge.svg)](https://github.com/hardikkanajariya/net-pulse/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Release](https://img.shields.io/github/v/release/hardikkanajariya/net-pulse?include_prereleases)](https://github.com/hardikkanajariya/net-pulse/releases)

---

NetPulse is a native Windows desktop application that monitors real-time network speed across all adapters, tracks per-application bandwidth usage, stores daily history in a local SQLite database, and provides a modern multi-tab dashboard with a desktop speed widget. It supports dark and light themes, configurable alerts, and CSV/JSON data export.

## Features

### Real-Time Monitoring
- **Live Speed Tracking** — Download and upload speed updated every second
- **60-Second Speed Chart** — Polyline/area chart with filled gradients on Overview and Live tabs
- **Auto-Format** — Speeds displayed as B/s, KB/s, MB/s, or GB/s automatically

### Per-Adapter Tracking
- **All Network Adapters** — Monitors Ethernet, Wi-Fi, VPN, Cellular, and other adapters simultaneously
- **Adapter Catalog** — Persists known adapters with type classification
- **Per-Adapter Daily Usage** — Tracks daily download/upload per adapter in SQLite

### Per-Application Tracking
- **Process Network Detection** — Uses Win32 `GetExtendedTcpTable` and `GetExtendedUdpTable` to enumerate processes with active network connections
- **Connection Counting** — Shows TCP+UDP connection count per process
- **5-Second Polling** — Process list refreshed every 5 seconds with a 30s cache

### Dashboard (7 Tabs)
| Tab | Description |
|-----|-------------|
| **Overview** | 4 stat cards (download/upload speed + today's usage), 60s mini speed chart, active adapter & process count summaries |
| **Live** | Large real-time speed chart with axis labels, big speed readout |
| **Adapters** | ListView of all adapters with live speed and today's usage per adapter |
| **Applications** | ListView of processes with active network connections and connection count |
| **History** | Daily usage history with 7 / 30 / 90 / All day filter pills |
| **Alerts** | Create threshold rules, toggle/delete rules, view alert history |
| **Settings** | Theme selection, widget display mode, adapter picker, startup toggle, CSV/JSON export, clear data |

### Desktop Widget
- **Always-on-Desktop** — Renders below all windows on the desktop layer via `SetWindowPos(HWND_BOTTOM)`
- **Compact Design** — 250×96px borderless pill showing download/upload speed
- **Display Modes** — Total traffic, top active adapter, or a selected adapter
- **Draggable** — Click and drag to reposition; position saved between sessions
- **Quick Access** — Double-click to open the full dashboard

### Alerts
- **Threshold Rules** — Set alerts for daily download, daily upload, daily total, speed download, or speed upload
- **Edge-Triggered** — Fires only on state transition (not repeated every tick)
- **Balloon Notifications** — Windows tray balloon tip on threshold breach
- **Persistent Rules** — Stored in SQLite; survives app restart
- **Alert History** — Last 200 triggered alerts with timestamps

### Theme Support
- **Dark Theme** — GitHub-dark inspired palette (default)
- **Light Theme** — GitHub-light inspired palette
- **System Theme** — Follows your Windows `AppsUseLightTheme` registry preference
- **Runtime Switching** — Theme changes apply instantly without restart
- **Persistent** — Theme choice saved in SQLite settings

### Data Export
- **CSV Export** — Export daily usage history as a CSV file
- **JSON Export** — Export daily usage history as a JSON file
- **SaveFileDialog** — Choose your own file name and location

### System Integration
- **System Tray** — Runs silently in the tray with live speed in the tooltip
- **Auto-Start** — Launches on Windows login via `HKCU\...\Run` registry key (enabled by default on first run)
- **Single Instance** — Named mutex (`Global\NetPulseMutex`) ensures only one instance
- **Lightweight** — Low CPU and RAM usage, no background services or telemetry

## Screenshot

<!-- Add a screenshot of the dashboard here -->
<!-- ![NetPulse Dashboard](docs/screenshot.png) -->

## Installation

### Download

Download the latest release from the [Releases](https://github.com/hardikkanajariya/net-pulse/releases) page.

### Build from Source

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/hardikkanajariya/net-pulse.git
cd net-pulse
dotnet restore
dotnet build
dotnet run
```

### Publish as Single File

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

The output will be in `bin/Release/net8.0-windows/win-x64/publish/`.

## Usage

1. Launch `NetPulse.exe` — the app starts silently in the system tray with the desktop widget visible
2. **Left-click** the tray icon to open the dashboard
3. **Right-click** the tray icon for the context menu:
   - **Open Dashboard** — Show the main window
   - **Start with Windows** — Toggle auto-start on login
   - **Clear All Records** — Reset all saved usage data
   - **Exit** — Close the application
4. Use the **Settings** tab in the dashboard to:
   - Switch between dark, light, or system theme
   - Change widget display mode (total / top adapter / selected adapter)
   - Export your data as CSV or JSON
5. Use the **Alerts** tab to set threshold-based notifications

## Architecture

```
App.xaml.cs (Composition Root)
├── NetworkMonitor          1s tick — per-adapter byte delta
├── ProcessTracker          5s poll — Win32 TCP/UDP tables
├── AlertService            evaluates rules against snapshot
├── TelemetryCoordinator    orchestrates all services, 60s DB flush
├── DatabaseService         SQLite WAL — daily_usage, adapters, alerts
├── SettingsService         widget mode + adapter persistence
├── ThemeManager            dark/light/system theme switching
├── StartupManager          registry-based auto-start
├── MainWindow              7-tab dashboard (WPF)
└── MeterWindow             desktop-layer speed widget (WPF)
```

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8 |
| UI Framework | WPF (Windows Presentation Foundation) |
| Tray Icon | System.Windows.Forms.NotifyIcon |
| Database | SQLite via Microsoft.Data.Sqlite |
| Win32 Interop | iphlpapi.dll (TCP/UDP tables), user32.dll (desktop widget) |
| Platform | Windows 10 / 11 |

## Data Storage

- **Database:** `%LOCALAPPDATA%\NetPulse\usage.db`
- **Schema:** `daily_usage`, `adapters`, `adapter_daily_usage`, `app_settings`, `alert_rules`, `alert_history`
- **Auto-start:** Windows Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)

No data is sent to any server. Everything stays on your machine.

## Contributing

Contributions are welcome! Please read the [Contributing Guide](CONTRIBUTING.md) before submitting a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

## Author

Created by **[Hardik Kanajariya](https://hardikkanajariya.in)**
