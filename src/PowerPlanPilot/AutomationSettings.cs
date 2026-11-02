using System.Text.Json.Serialization;

namespace PowerPlanPilot;

internal sealed class AutomationSettings
{
    public bool IsEnabled { get; set; }

    public AutomationMode Mode { get; set; } = AutomationMode.IdleTime;

    public int IdleMinutes { get; set; } = 30;

    // Retained only to migrate settings saved by older versions.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProcessName { get; set; }

    public List<string> ProcessNames { get; set; } = [];

    public double ProcessCpuThresholdPercent { get; set; } = 2;

    public int ProcessLowUsageMinutes { get; set; } = 10;

    public Guid? ScaleUpPowerPlanId { get; set; }

    public double ProcessHighCpuThresholdPercent { get; set; } = 50;

    public int ProcessHighUsageMinutes { get; set; } = 2;

    public Guid? TargetPowerPlanId { get; set; }

    public double SystemLowCpuThresholdPercent { get; set; } = 10;

    public int SystemLowUsageMinutes { get; set; } = 5;

    public double SystemHighCpuThresholdPercent { get; set; } = 50;

    public int SystemHighUsageMinutes { get; set; } = 1;

    public bool SystemIdleScaleDown { get; set; }

    public bool SwitchOnAcPower { get; set; }

    public Guid? AcPowerPlanId { get; set; }

    public bool SwitchOnBattery { get; set; }

    public Guid? BatteryPowerPlanId { get; set; }

    public void Normalize()
    {
        IdleMinutes = Math.Clamp(IdleMinutes, 1, 1440);
        ProcessCpuThresholdPercent = Math.Clamp(ProcessCpuThresholdPercent, 0, 100);
        ProcessLowUsageMinutes = Math.Clamp(ProcessLowUsageMinutes, 1, 1440);
        ProcessHighCpuThresholdPercent = Math.Clamp(ProcessHighCpuThresholdPercent, 0, 100);
        ProcessHighUsageMinutes = Math.Clamp(ProcessHighUsageMinutes, 1, 1440);
        SystemHighCpuThresholdPercent = Math.Clamp(SystemHighCpuThresholdPercent, 1, 100);
        SystemLowCpuThresholdPercent = Math.Clamp(SystemLowCpuThresholdPercent, 0, SystemHighCpuThresholdPercent - 1);
        SystemLowUsageMinutes = Math.Clamp(SystemLowUsageMinutes, 1, 1440);
        SystemHighUsageMinutes = Math.Clamp(SystemHighUsageMinutes, 1, 1440);

        ProcessNames ??= [];
        if (!string.IsNullOrWhiteSpace(ProcessName))
        {
            ProcessNames.Add(ProcessName);
            ProcessName = null;
        }

        ProcessNames = ProcessNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => Path.GetFileNameWithoutExtension(name.Trim()) + ".exe")
            .Where(name => name.Length > 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
