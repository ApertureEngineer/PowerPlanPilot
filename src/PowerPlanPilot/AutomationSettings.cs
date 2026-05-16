namespace PowerPlanPilot;

internal sealed class AutomationSettings
{
    public bool IsEnabled { get; set; }

    public AutomationMode Mode { get; set; } = AutomationMode.IdleTime;

    public int IdleMinutes { get; set; } = 30;

    public string? ProcessName { get; set; }

    public double ProcessCpuThresholdPercent { get; set; } = 2;

    public int ProcessLowUsageMinutes { get; set; } = 10;

    public Guid? TargetPowerPlanId { get; set; }

    public bool SwitchOnAcPower { get; set; }

    public Guid? AcPowerPlanId { get; set; }

    public bool SwitchOnBattery { get; set; }

    public Guid? BatteryPowerPlanId { get; set; }

    public void Normalize()
    {
        IdleMinutes = Math.Clamp(IdleMinutes, 1, 1440);
        ProcessCpuThresholdPercent = Math.Clamp(ProcessCpuThresholdPercent, 0, 100);
        ProcessLowUsageMinutes = Math.Clamp(ProcessLowUsageMinutes, 1, 1440);

        if (string.IsNullOrWhiteSpace(ProcessName))
        {
            ProcessName = null;
        }
        else
        {
            ProcessName = ProcessName.Trim();
        }
    }
}
