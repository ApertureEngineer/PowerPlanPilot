using System.Diagnostics;

namespace PowerPlanPilot;

internal interface IProcessNameProvider
{
    IReadOnlyList<string> GetOpenProcessNames();
}

internal interface IProcessCpuSampleSource
{
    IReadOnlyList<ProcessSample> GetCurrentSamples(string processName);
}

internal sealed class SystemProcessService : IProcessNameProvider, IProcessCpuSampleSource
{
    public IReadOnlyList<string> GetOpenProcessNames()
    {
        return Process.GetProcesses()
            .Select(process =>
            {
                using (process)
                {
                    try
                    {
                        return process.ProcessName + ".exe";
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray()!;
    }

    public IReadOnlyList<ProcessSample> GetCurrentSamples(string processName)
    {
        var normalizedName = Path.GetFileNameWithoutExtension(processName);
        var samples = new List<ProcessSample>();

        foreach (var process in Process.GetProcessesByName(normalizedName))
        {
            using (process)
            {
                try
                {
                    if (process.HasExited)
                    {
                        continue;
                    }

                    samples.Add(new ProcessSample(process.Id, process.TotalProcessorTime));
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }
        }

        return samples;
    }
}

internal sealed record ProcessSample(int ProcessId, TimeSpan TotalProcessorTime);
