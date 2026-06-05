# Changelog

## 0.2.4 - 2026-06-06

- Give the About window header and summary cards more room under Windows DPI scaling.

## 0.2.3 - 2026-06-05

- Increase About window space for the summary cards and version details.
- Shorten build information to a compact commit id.

## 0.2.2 - 2026-06-05

- Make the About window larger and resizable so DPI scaling does not clip content.

## 0.2.1 - 2026-06-05

- Rework the About window with a larger three-column summary layout.
- Add explicit version, runtime, platform, and install path information.

## 0.2.0 - 2026-06-05

- Refresh the About window with a cleaner fixed-size layout and project summary.
- Make the tray menu dismiss normally when clicking away from it.
- Add a timeout and safer output handling around `powercfg`.
- Save automation settings through a temporary file before replacing the live JSON file.
- Start using a stable portable install folder instead of versioned folder names.

## 0.1.0

- Initial portable Windows tray app for switching Windows power plans.
- Read power plans live from `powercfg /L` and switch with `powercfg /S`.
- Persist automation settings under `%APPDATA%\PowerPlanPilot\automation.json`.
