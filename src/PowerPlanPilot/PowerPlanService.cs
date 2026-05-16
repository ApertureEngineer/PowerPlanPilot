using System.Globalization;
using System.Text.RegularExpressions;

namespace PowerPlanPilot;

internal sealed partial class PowerPlanService
{
    private readonly IPowerCfgRunner _powerCfgRunner;

    public PowerPlanService(IPowerCfgRunner? powerCfgRunner = null)
    {
        _powerCfgRunner = powerCfgRunner ?? new PowerCfgRunner();
    }

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

    private PowerCfgResult RunPowerCfg(string arguments)
    {
        var result = _powerCfgRunner.Run(arguments);
        if (result.ExitCode == 0)
        {
            return result;
        }

        var message = string.Format(
            CultureInfo.CurrentCulture,
            "powercfg {0} failed with exit code {1}.",
            arguments,
            result.ExitCode);

        throw new PowerPlanCommandException(message, result.ExitCode, result.Output, result.Error);
    }

    [GeneratedRegex(@"(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}).*?\((?<label>.+)\)\s*\*?$")]
    private static partial Regex PowerPlanLineRegex();
}
