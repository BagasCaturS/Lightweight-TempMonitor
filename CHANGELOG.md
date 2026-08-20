# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-08-20

### Added

- Initial release of the system-tray temperature monitor.
- Dynamic tray icon showing the hottest component (CPU / GPU / SSD / MB / Other) with color state:
  green (ok), amber (warm), red (hot); gray when no data.
- Hover tooltip with current temperatures, fan speed, load and session peak.
- Threshold alerts with balloon notification naming the offending hardware and optional beep.
- Hysteresis + cooldown logic to prevent alert spam.
- Details window with a live 10-minute history graph (300 samples at 2 s polling) and a full
  sensor table (value / min / max per session).
- Dynamic °C gridlines on the history graph that follow real hardware temperatures.
- Settings window with per-group thresholds, per-sensor overrides, and beep toggle.
- Windows auto-start toggle (HKCU Run) from the tray menu.
- `settings.json` configuration auto-created next to the executable.
- Error dialog when the sensor driver fails to initialize; UI exceptions logged to `error.log`.
- Single-instance guard via a named mutex.

### Fixed

- Red tray icon could remain stuck after a temperature spike; icon now clears as soon as the value
  drops below the threshold (hysteresis now only gates notifications).
- Details window silently failing to open — `ListViewItem` sub-items were accessed before being
  created, throwing an exception that was swallowed by the exception handler.
- Static "Warning Temperature" / "Critical Temperature" storage sensors causing false alerts.

### Performance

- 2 s polling interval with a zero-allocation tick and cached sensor references.
- Steady state: ~78 MB private memory, ~0.25 s CPU time / 15 s.