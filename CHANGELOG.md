# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [1.0.0] - 2026-03-31

### Added

- Native Windows tray application built with .NET 8 and WPF
- Real-time download and upload speed monitoring with 1-second updates
- System tray tooltip with live network speed
- Modern 7-tab dashboard with sidebar navigation:
  - **Overview** — stat cards, 60s speed chart, adapter & process summaries
  - **Live** — large real-time speed chart with axis labels
  - **Adapters** — per-adapter live speed and daily usage ListView
  - **Applications** — process network connections via Win32 TCP/UDP tables
  - **History** — daily usage history with 7/30/90/All filter pills
  - **Alerts** — threshold rule management and alert history
  - **Settings** — theme, widget, startup, and export configuration
- Per-adapter network monitoring with adapter type classification
- Per-application bandwidth tracking using `GetExtendedTcpTable`/`GetExtendedUdpTable`
- Desktop speed widget (250×96) pinned to the desktop layer via `SetWindowPos(HWND_BOTTOM)`
  - Three display modes: total traffic, top adapter, selected adapter
  - Draggable with position persistence
  - Double-click to open dashboard
- Threshold-based alert system with 5 rule types (daily download/upload/total, speed download/upload)
  - Edge-triggered notifications (fires only on state transition)
  - Balloon tip notifications via system tray
  - Persistent rules and alert history (last 200 entries) in SQLite
- Dark / light / system theme support with runtime switching
  - GitHub-dark and GitHub-light inspired palettes
  - System mode follows Windows `AppsUseLightTheme` registry preference
  - Theme choice persisted in SQLite
- CSV and JSON data export with SaveFileDialog
- SQLite persistence with WAL mode for daily usage, adapter catalog, adapter daily usage, settings, alert rules, and alert history
- Start with Windows toggle using the current user Run registry key (enabled by default on first run)
- Single-instance protection using a named mutex (`Global\NetPulseMutex`)
- Local-only data storage with no cloud sync or telemetry
- GitHub Actions CI workflow for build validation
- GitHub Actions release workflow for single-file publish and release artifacts
