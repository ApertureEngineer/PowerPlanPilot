namespace PowerPlanPilot;

internal sealed class AutomationController : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    private readonly PowerPlanService _powerPlanService;
    private readonly ProcessCpuSampler _processCpuSampler;
    private readonly SystemCpuSampler _systemCpuSampler;
    private readonly Func<PowerLineStatus> _getPowerLineStatus;
    private readonly Func<DateTimeOffset> _getUtcNow;
    private readonly Func<TimeSpan> _getIdleTime;
    private readonly System.Windows.Forms.Timer _timer = new()
    {
        Interval = (int)CheckInterval.TotalMilliseconds,
    };

    private DateTimeOffset? _lowUsageSince;
    private DateTimeOffset? _highUsageSince;

    public AutomationController(
        PowerPlanService powerPlanService,
        AutomationSettings settings,
        ProcessCpuSampler? processCpuSampler = null,
        Func<PowerLineStatus>? getPowerLineStatus = null,
        SystemCpuSampler? systemCpuSampler = null,
        Func<DateTimeOffset>? getUtcNow = null,
        Func<TimeSpan>? getIdleTime = null)
    {
        _powerPlanService = powerPlanService;
        _processCpuSampler = processCpuSampler ?? new ProcessCpuSampler();
        _systemCpuSampler = systemCpuSampler ?? new SystemCpuSampler();
        _getPowerLineStatus = getPowerLineStatus ?? (() => SystemInformation.PowerStatus.PowerLineStatus);
        _getUtcNow = getUtcNow ?? (() => DateTimeOffset.UtcNow);
        _getIdleTime = getIdleTime ?? IdleTimeService.GetIdleTime;
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
        _highUsageSince = null;
        _processCpuSampler.Reset();
        _systemCpuSampler.Reset();
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
            ResetSystemCpuState();
            SetStatus("Automation disabled");
            return;
        }

        try
        {
            if (EvaluatePowerSourceSwitch())
            {
                ResetSystemCpuState();
                return;
            }

            switch (Settings.Mode)
            {
                case AutomationMode.IdleTime:
                    EvaluateIdleTime();
                    break;
                case AutomationMode.ProcessCpu:
                    EvaluateProcessCpu();
                    break;
                case AutomationMode.SystemCpu:
                    EvaluateSystemCpu();
                    break;
            }
        }
        catch (Exception ex)
        {
            ResetSystemCpuState();
            _processCpuSampler.Reset();
            SetStatus($"Automation error: {ex.Message}");
        }
    }

    private void ResetSystemCpuState()
    {
        _systemCpuSampler.Reset();
        _lowUsageSince = null;
        _highUsageSince = null;
    }

    private void EvaluateSystemCpu()
    {
        var cpu = _systemCpuSampler.GetCpuUsagePercent();
        if (cpu is null)
        {
            _lowUsageSince = null;
            _highUsageSince = null;
            SetStatus("Collecting system CPU baseline");
            return;
        }

        if (Settings.ScaleUpPowerPlanId is null && Settings.TargetPowerPlanId is null)
        {
            SetStatus("System CPU automation needs a power plan");
            return;
        }

        var now = _getUtcNow();
        if (cpu >= Settings.SystemHighCpuThresholdPercent)
        {
            _highUsageSince ??= now;
            _lowUsageSince = null;
        }
        else if (cpu <= Settings.SystemLowCpuThresholdPercent)
        {
            _lowUsageSince ??= now;
            _highUsageSince = null;
        }
        else
        {
            _lowUsageSince = null;
            _highUsageSince = null;
        }

        var highDuration = _highUsageSince is null ? TimeSpan.Zero : now - _highUsageSince.Value;
        var lowDuration = _lowUsageSince is null ? TimeSpan.Zero : now - _lowUsageSince.Value;
        if (Settings.ScaleUpPowerPlanId is { } scaleUp && highDuration >= TimeSpan.FromMinutes(Settings.SystemHighUsageMinutes))
        {
            ActivatePlan(scaleUp, $"System CPU high at {cpu:F1}% for {FormatDuration(highDuration)}");
            return;
        }

        if (Settings.TargetPowerPlanId is not null && lowDuration >= TimeSpan.FromMinutes(Settings.SystemLowUsageMinutes))
        {
            ActivateTargetPlan($"System CPU low at {cpu:F1}% for {FormatDuration(lowDuration)}");
            return;
        }

        if (Settings.TargetPowerPlanId is not null && Settings.SystemIdleScaleDown &&
            lowDuration >= TimeSpan.FromMinutes(1) && _getIdleTime() >= TimeSpan.FromMinutes(Settings.IdleMinutes))
        {
            ActivateTargetPlan($"Idle with low system CPU ({cpu:F1}%)");
            return;
        }

        SetStatus($"System CPU: {cpu:F1}%; high {FormatDuration(highDuration)} / {Settings.SystemHighUsageMinutes}m; low {FormatDuration(lowDuration)} / {Settings.SystemLowUsageMinutes}m");
    }

    private bool EvaluatePowerSourceSwitch()
    {
        var powerLineStatus = _getPowerLineStatus();

        if (powerLineStatus == PowerLineStatus.Online && Settings.SwitchOnAcPower)
        {
            if (Settings.AcPowerPlanId is not { } targetPlanId)
            {
                SetStatus("AC automation needs a power plan");
                return true;
            }

            ActivatePlan(targetPlanId, "On AC power");
            return true;
        }

        if (powerLineStatus == PowerLineStatus.Offline && Settings.SwitchOnBattery)
        {
            if (Settings.BatteryPowerPlanId is not { } targetPlanId)
            {
                SetStatus("Battery automation needs a power plan");
                return true;
            }

            ActivatePlan(targetPlanId, "On battery");
            return true;
        }

        return false;
    }

    private void EvaluateIdleTime()
    {
        if (Settings.TargetPowerPlanId is null)
        {
            SetStatus("Idle automation needs a scale-down plan");
            return;
        }

        var idleTime = _getIdleTime();
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
        if (Settings.ProcessNames.Count == 0)
        {
            SetStatus("Automation needs a process");
            return;
        }

        var usage = _processCpuSampler.GetCpuUsagePercent(Settings.ProcessNames);
        if (!usage.HasRunningProcess)
        {
            _lowUsageSince = null;
            _highUsageSince = null;
            SetStatus("Waiting for a selected process to start");
            return;
        }

        if (!usage.HasBaseline)
        {
            _lowUsageSince = null;
            _highUsageSince = null;
            SetStatus("Collecting CPU baseline for selected processes");
            return;
        }

        if (Settings.TargetPowerPlanId is null && Settings.ScaleUpPowerPlanId is null)
        {
            SetStatus("Process automation needs a power plan");
            return;
        }

        var canScaleDown = Settings.TargetPowerPlanId is not null;
        var canScaleUp = Settings.ScaleUpPowerPlanId is not null;
        var isHighUsage = canScaleUp && usage.Percent >= Settings.ProcessHighCpuThresholdPercent;
        var isLowUsage = canScaleDown && usage.Percent <= Settings.ProcessCpuThresholdPercent;
        var now = _getUtcNow();

        if (isHighUsage)
        {
            _highUsageSince ??= now;
            _lowUsageSince = null;
        }
        else if (isLowUsage)
        {
            _lowUsageSince ??= now;
            _highUsageSince = null;
        }
        else
        {
            _lowUsageSince = null;
            _highUsageSince = null;
        }

        var lowUsageDuration = _lowUsageSince is null ? TimeSpan.Zero : now - _lowUsageSince.Value;
        var highUsageDuration = _highUsageSince is null ? TimeSpan.Zero : now - _highUsageSince.Value;
        var requiredLowDuration = TimeSpan.FromMinutes(Settings.ProcessLowUsageMinutes);
        var requiredHighDuration = TimeSpan.FromMinutes(Settings.ProcessHighUsageMinutes);

        if (canScaleUp && highUsageDuration >= requiredHighDuration)
        {
            ActivatePlan(
                Settings.ScaleUpPowerPlanId!.Value,
                $"Selected processes high at {usage.Percent:F1}% CPU for {FormatDuration(highUsageDuration)}");
            return;
        }

        if (canScaleDown && lowUsageDuration >= requiredLowDuration)
        {
            ActivateTargetPlan(
                $"Selected processes low at {usage.Percent:F1}% CPU for {FormatDuration(lowUsageDuration)}");
            return;
        }

        SetStatus(FormatProcessStatus(usage.Percent, lowUsageDuration, highUsageDuration));
    }

    private void ActivateTargetPlan(string reason)
    {
        if (Settings.TargetPowerPlanId is not { } targetPlanId)
        {
            return;
        }

        ActivatePlan(targetPlanId, reason);
    }

    private void ActivatePlan(Guid targetPlanId, string reason)
    {
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

    private string FormatProcessStatus(double cpuPercent, TimeSpan lowUsageDuration, TimeSpan highUsageDuration)
    {
        var status = $"Selected processes: {cpuPercent:F1}% CPU";

        if (Settings.ScaleUpPowerPlanId is not null)
        {
            status += $"; high {FormatDuration(highUsageDuration)} / {Settings.ProcessHighUsageMinutes}m";
        }

        if (Settings.TargetPowerPlanId is not null)
        {
            status += $"; low {FormatDuration(lowUsageDuration)} / {Settings.ProcessLowUsageMinutes}m";
        }

        return status;
    }
}
