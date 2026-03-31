# NetPulse

**A lightweight, real-time network speed monitor for Windows 11**

[![Build](https://github.com/hardikkanajariya/net-pulse/actions/workflows/build.yml/badge.svg)](https://github.com/hardikkanajariya/net-pulse/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Release](https://img.shields.io/github/v/release/hardikkanajariya/net-pulse?include_prereleases)](https://github.com/hardikkanajariya/net-pulse/releases)

---

NetPulse is a native Windows 11 desktop application that lives in your system tray and monitors real-time internet download and upload speeds. It stores daily usage history locally and provides a clean dashboard to view your network activity.

## Features

- **System Tray** — Runs silently in the tray with live speed in the tooltip
- **Real-Time Monitoring** — Tracks download and upload speed, updated every second
- **Auto-Format** — Displays speeds in B/s, KB/s, MB/s, or GB/s automatically
- **Daily Usage Tracking** — Records total daily download/upload to a local SQLite database
- **Dashboard** — Clean, minimal window showing live speeds, today's usage, and 30-day history
- **Auto-Start** — Toggle "Start with Windows" from the tray menu
- **Single Instance** — Only one instance runs at a time
- **Lightweight** — Low CPU and RAM usage, no background services

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

1. Launch `NetPulse.exe` — the app starts silently in the system tray
2. **Left-click** the tray icon to open the dashboard
3. **Right-click** the tray icon for the context menu:
   - **Open Dashboard** — Show the main window
   - **Start with Windows** — Toggle auto-start on login
   - **Clear All Records** — Reset all saved usage data
   - **Exit** — Close the application

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8 |
| UI Framework | WPF |
| Tray Icon | System.Windows.Forms.NotifyIcon |
| Database | SQLite (Microsoft.Data.Sqlite) |
| Platform | Windows 10/11 |

## Data Storage

- **Database:** `%LOCALAPPDATA%\NetPulse\usage.db`
- **Auto-start:** Windows Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)

No data is sent to any server. Everything stays on your machine.

## Contributing

Contributions are welcome! Please read the [Contributing Guide](CONTRIBUTING.md) before submitting a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

## Author

Created by **[Hardik Kanajariya](https://hardikkanajariya.in)**
