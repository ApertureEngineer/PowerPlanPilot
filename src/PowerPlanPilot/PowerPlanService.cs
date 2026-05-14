using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerPlanPilot;

internal sealed partial class PowerPlanService
{
    public IReadOnlyList<PowerPlan> GetPowerPlans()
    {
        var result = RunPowerCfg("/L");
        return ParsePowerPlans(result.Output);
    }

    public void ActivatePowerPlan(Guid id)
    {
        RunPowerCfg($"/S {id:D}");
    }

    internal static IReadOnlyList<PowerPlan> ParsePowerPlans(string output)
    {
        var plans = new List<PowerPlan>();

        foreach (var rawLine in output.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            var match = PowerPlanLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            if (!Guid.TryParse(match.Groups["id"].Value, out var id))
            {
                continue;
            }

            var label = match.Groups["label"].Value.Trim();
            var isActive = line.EndsWith('*');
            plans.Add(new PowerPlan(id, label, isActive));
        }

        return plans
            .OrderByDescending(plan => plan.IsActive)
            .ThenBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static PowerCfgResult RunPowerCfg(string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/d /c \"chcp 65001>nul & powercfg {arguments}\"",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                "powercfg {0} failed with exit code {1}.",
                arguments,
                process.ExitCode);

            throw new PowerPlanCommandException(message, process.ExitCode, output, error);
        }

        return new PowerCfgResult(output, error);
    }

    [GeneratedRegex(@"(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}).*?\((?<label>.+)\)\s*\*?$")]
    private static partial Regex PowerPlanLineRegex();

    private sealed record PowerCfgResult(string Output, string Error);
}
