# PowerPlanPilot

Smart control of power plans for Windows systems.

PowerPlanPilot starts as a Windows tray app. It loads the power plans currently
registered in Windows and lets you switch between them with one click from the
taskbar notification area.

## Phase 1

- Load Windows power plans through `powercfg /L`
- Highlight the active plan
- Switch plans through `powercfg /S <guid>`
- Open a small tray menu next to the Windows clock

## Phase 2

- Enable or disable automation from the tray menu
- Choose the scale-down target power plan
- Switch to the target plan when Windows idle time exceeds a configured number of minutes
- Switch to the target plan when a selected running process stays below a configured CPU percentage for a configured number of minutes
- Pick the watched process from the current process list, including long-running tools such as `cTrader.exe` or `cAlgo.exe`
- Persist automation settings in `%APPDATA%\PowerPlanPilot\automation.json`

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run --project .\src\PowerPlanPilot\PowerPlanPilot.csproj
```
