# PowerPlanPilot

PowerPlanPilot is a lightweight Windows tray application for quick power-plan
switching and simple automatic scale-down rules.

Current app version: `0.2.4`.

## Current status

- Runs as a Windows notification-area app with a compact, refreshed tray menu.
- Reads the current Windows power plans with `powercfg /L` every time the menu is
  opened or **Refresh** is clicked, so plans created outside the app appear
  without recompiling.
- Shows the active Windows power plan and switches plans with `powercfg /S <guid>`.
- Supports Unicode power-plan names, including German umlauts such as `Ä`, `Ö`,
  `Ü`, `ä`, `ö`, `ü`, `ß`, and other special characters, by forcing `powercfg`
  output through UTF-8.
- Saves automation settings in `%APPDATA%\PowerPlanPilot\automation.json`.
- Includes tray tools for Windows Power Options, per-user autostart, and project
  info/credits.

## What is saved?

PowerPlanPilot does **not** start from scratch after every compile or restart:

- The active power plan itself is stored by Windows. If PowerPlanPilot switches
  to another plan, Windows keeps that active plan until something changes it.
- PowerPlanPilot automation settings are saved as JSON under the current Windows
  user profile at `%APPDATA%\PowerPlanPilot\automation.json`.
- The saved app settings include automation enabled/disabled state, selected
  scale-down target plan GUID, switch condition, idle threshold, selected processes,
  CPU threshold, and low-usage duration.
- Manually added, renamed, or removed power plans are not copied into the app;
  they remain Windows power plans and are re-read from `powercfg` on menu open or
  refresh.

If the app cannot read the settings file, it falls back to safe defaults and will
create a fresh settings file on the next saved change.

## Automation features

- Enable or disable automation from the tray menu.
- Choose a scale-down target power plan.
- Switch to the target plan after Windows idle time exceeds the configured number
  of minutes.
- Use system CPU automation to scale up after sustained high total CPU usage and
  scale down after sustained low usage, without selecting process names. Defaults
  are 50% for one minute to scale up and 10% for five minutes to scale down.
  The thresholds are percentages of total system CPU capacity; select the plans
  you want to use in the tray menu.
- Optionally scale down sooner when Windows has reached the idle threshold, but
  only after system CPU has stayed below the low threshold for at least one minute.
  System CPU, process CPU, and idle-only automation are alternative modes.
- Select multiple process names from the current process list. Saved selections
  remain available even after those processes exit.
- Switch up or down when the selected running processes' combined CPU usage
  stays above or below the configured threshold for the configured duration.
  CPU percentage is relative to the total capacity of all logical processors.
- Sampling runs every 15 seconds. If no selected process is running, automation
  waits; it does not switch plans on process exit. AC and battery rules take
  priority over CPU and idle automation.

## Build

```powershell
dotnet build
```

## Publish locally

Create a regular Windows publish output with:

```powershell
dotnet publish .\src\PowerPlanPilot\PowerPlanPilot.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\PowerPlanPilot-win-x64
```

## Portable install and updates

Use a stable portable install folder without a version number:

```text
%USERPROFILE%\OneDrive\Anwendungen\PortableApps\PowerPlanPilot
```

Keeping the folder name stable matters because Windows autostart stores the full
path to `PowerPlanPilot.exe` in the current user's Run registry key. Version
numbers belong in the project file and changelog, not in the install folder.

Update checklist:

1. Build and publish the current code:

   ```powershell
   dotnet publish .\src\PowerPlanPilot\PowerPlanPilot.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\PowerPlanPilot-win-x64
   ```

2. Exit PowerPlanPilot from the tray menu, or stop the running `PowerPlanPilot`
   process.
3. Copy everything from `.\artifacts\PowerPlanPilot-win-x64\` into the stable
   portable folder.
4. Start `%USERPROFILE%\OneDrive\Anwendungen\PortableApps\PowerPlanPilot\PowerPlanPilot.exe`.
5. If autostart is enabled, confirm the tray menu still shows **Start with
   Windows** checked. If not, toggle it off and on once so the registry path is
   refreshed.

For Microsoft Store distribution, package the WinForms app as MSIX with a
Windows Application Packaging Project in Visual Studio, add Store-ready image
assets in that packaging project, test the generated package with the Windows App
Certification Kit, then upload the `.msixupload` or `.msixbundle` in Partner
Center. Store-packaged autostart should use the package manifest
`windows.startup` extension instead of the unpackaged registry Run key.

## Run

```powershell
dotnet run --project .\src\PowerPlanPilot\PowerPlanPilot.csproj
```

## Notes for validation

- Add or rename a Windows power plan, then open the tray menu or click
  **Refresh**. The plan should appear with its current name.
- Test names with umlauts and special characters, for example the german power plan: `Höchstleistung`, and confirm the tray menu displays them correctly.
- Click **Windows power options** and confirm native Windows Power Options opens.
- Toggle **Start with Windows**, restart Windows, and confirm PowerPlanPilot starts
  for the current user.
- Open **Info** and confirm credits and the repository/homepage link are shown.
- Change automation settings, close the app, rebuild or restart it, and confirm
  the saved values are restored from `%APPDATA%\PowerPlanPilot\automation.json`.
