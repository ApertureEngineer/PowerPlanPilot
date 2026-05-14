# AGENTS.md

## Project status

PowerPlanPilot is a Windows Forms tray app for Windows power-plan switching and
basic automation.

Current behavior:

- Power plans are read live from Windows through `powercfg /L` when the tray menu
  opens and when the user clicks **Refresh**.
- Switching uses `powercfg /S <guid>`.
- `powercfg` is run through `cmd.exe` after switching the console code page to
  UTF-8 so power-plan names with umlauts and special characters are decoded
  correctly.
- Automation settings are persisted per Windows user in
  `%APPDATA%\PowerPlanPilot\automation.json`.
- Windows itself persists the active power plan; PowerPlanPilot does not need to
  save the active plan separately.

## Development notes

- Primary source code lives in `src/PowerPlanPilot/`.
- This is a Windows-only app targeting `net9.0-windows` with Windows Forms.
- Keep tray UI labels short because `NotifyIcon.Text` has a 63-character limit.
- Do not wrap imports/usings in try/catch blocks.
- Prefer small, direct changes and keep the app dependency-light.

## Validation

When a .NET SDK is available, run:

```powershell
dotnet build
```

Manual Windows validation should include:

1. Add or rename a Windows power plan and confirm it appears after opening the
   tray menu or clicking **Refresh**.
2. Test power-plan names containing `Ä`, `Ö`, `Ü`, `ä`, `ö`, `ü`, `ß`,
   parentheses, plus signs, hashes, and spaces.
3. Change automation settings, close/rebuild/restart the app, and confirm the
   values are restored from `%APPDATA%\PowerPlanPilot\automation.json`.
