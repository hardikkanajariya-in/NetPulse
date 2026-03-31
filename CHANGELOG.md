# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [1.0.0] - 2026-03-31

### Added

- Native Windows tray application built with .NET 8 and WPF
- Real-time download and upload speed monitoring with 1 second updates
- System tray tooltip with live network speed
- Minimal dashboard showing live speed, today's usage, and 30-day history
- SQLite persistence for daily usage history
- Start with Windows toggle using the current user Run registry key
- Single-instance protection using a named mutex
- Local-only data storage with no cloud sync or telemetry
- GitHub Actions CI workflow for build validation
- GitHub Actions release workflow for single-file publish and release artifacts
