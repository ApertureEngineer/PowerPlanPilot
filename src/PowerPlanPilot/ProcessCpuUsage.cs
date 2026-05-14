namespace PowerPlanPilot;

internal sealed record ProcessCpuUsage(double Percent, bool HasRunningProcess, bool HasBaseline);
