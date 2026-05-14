namespace PowerPlanPilot;

internal sealed class AutomationController : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    private readonly PowerPlanService _powerPlanService;
    private readonly ProcessCpuSampler _processCpuSampler = new();
    private readonly System.Windows.Forms.Timer _timer = new()
    {
        Interval = (int)CheckInterval.TotalMilliseconds,
    };

    private DateTimeOffset? _lowUsageSince;
    private string? _lastProcessName;

    public AutomationController(PowerPlanService powerPlanService, AutomationSettings settings)
    {
        _powerPlanService = powerPlanService;
        Settings = settings;
        Settings.Normalize();
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler<string>? StatusChanged;

    public AutomationSettings Settings { get; }

    public string StatusText { get; private set; } = "Automation disabled";

    public void Start()
    {
        _timer.Enabled = true;
        Evaluate();
    }

    public void ApplySettings()
    {
        Settings.Normalize();
        _lowUsageSince = null;
        _processCpuSampler.Reset();
        Evaluate();
    }

    public void Dispose()
    {
        _timer.Tick -= OnTimerTick;
        _timer.Dispose();
    }

    private void OnTimerTick(object? sender, EventArgs e) => Evaluate();

    private void Evaluate()
    {
        if (!Settings.IsEnabled)
        {
            SetStatus("Automation disabled");
            return;
        }

        if (Settings.TargetPowerPlanId is null)
        {
            SetStatus("Automation needs a target power plan");
            return;
        }

        try
        {
            switch (Settings.Mode)
            {
                case AutomationMode.IdleTime:
                    EvaluateIdleTime();
                    break;
                case AutomationMode.ProcessCpu:
                    EvaluateProcessCpu();
                    break;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Automation error: {ex.Message}");
        }
    }

    private void EvaluateIdleTime()
    {
        var idleTime = IdleTimeService.GetIdleTime();
        var threshold = TimeSpan.FromMinutes(Settings.IdleMinutes);

        if (idleTime >= threshold)
        {
            ActivateTargetPlan($"Idle for {FormatDuration(idleTime)}");
            return;
        }

        SetStatus($"Waiting for idle time: {FormatDuration(idleTime)} / {Settings.IdleMinutes}m");
    }

    private void EvaluateProcessCpu()
    {
        if (string.IsNullOrWhiteSpace(Settings.ProcessName))
        {
            SetStatus("Automation needs a process");
            return;
        }

        if (!string.Equals(_lastProcessName, Settings.ProcessName, StringComparison.OrdinalIgnoreCase))
        {
            _lastProcessName = Settings.ProcessName;
            _lowUsageSince = null;
            _processCpuSampler.Reset();
        }

        var usage = _processCpuSampler.GetCpuUsagePercent(Settings.ProcessName);
        if (!usage.HasBaseline)
        {
            SetStatus($"Collecting CPU baseline for {Settings.ProcessName}");
            return;
        }

        var isLowUsage = usage.Percent <= Settings.ProcessCpuThresholdPercent;
        var now = DateTimeOffset.UtcNow;

        if (isLowUsage)
        {
            _lowUsageSince ??= now;
        }
        else
        {
            _lowUsageSince = null;
        }

        var lowUsageDuration = _lowUsageSince is null ? TimeSpan.Zero : now - _lowUsageSince.Value;
        var requiredDuration = TimeSpan.FromMinutes(Settings.ProcessLowUsageMinutes);

        if (lowUsageDuration >= requiredDuration)
        {
            ActivateTargetPlan(
                $"{Settings.ProcessName} at {usage.Percent:F1}% CPU for {FormatDuration(lowUsageDuration)}");
            return;
        }

        SetStatus(
            $"{Settings.ProcessName}: {usage.Percent:F1}% CPU; low usage {FormatDuration(lowUsageDuration)} / {Settings.ProcessLowUsageMinutes}m");
    }

    private void ActivateTargetPlan(string reason)
    {
        if (Settings.TargetPowerPlanId is not { } targetPlanId)
        {
            return;
        }

        var activePlan = _powerPlanService.GetPowerPlans().FirstOrDefault(plan => plan.IsActive);
        if (activePlan?.Id == targetPlanId)
        {
            SetStatus($"Target power plan already active ({reason})");
            return;
        }

        _powerPlanService.ActivatePowerPlan(targetPlanId);
        var targetPlan = _powerPlanService.GetPowerPlans().FirstOrDefault(plan => plan.Id == targetPlanId);
        SetStatus($"Switched to {targetPlan?.Name ?? "target power plan"}: {reason}");
    }

    private void SetStatus(string status)
    {
        if (StatusText == status)
        {
            return;
        }

        StatusText = status;
        StatusChanged?.Invoke(this, status);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        }

        return $"{Math.Max(0, duration.Seconds)}s";
    }
}
