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

Automation will build on the same power-plan service and add demand-aware scaling
between efficient and high-performance plans.

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run --project .\src\PowerPlanPilot\PowerPlanPilot.csproj
```
