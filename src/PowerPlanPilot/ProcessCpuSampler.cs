using System.Diagnostics;

namespace PowerPlanPilot;

internal sealed class ProcessCpuSampler
{
    private readonly Dictionary<int, ProcessSample> _previousSamples = [];

    public ProcessCpuUsage GetCpuUsagePercent(string processName)
    {
        var currentSamples = GetCurrentSamples(processName);
        var now = DateTimeOffset.UtcNow;
        double cpuPercent = 0;
        var matchedPreviousSample = false;

        foreach (var sample in currentSamples)
        {
            if (!_previousSamples.TryGetValue(sample.ProcessId, out var previous))
            {
                continue;
            }

            matchedPreviousSample = true;
            var elapsed = now - previous.Timestamp;
            if (elapsed <= TimeSpan.Zero)
            {
                continue;
            }

            var processorTime = sample.TotalProcessorTime - previous.TotalProcessorTime;
            if (processorTime < TimeSpan.Zero)
            {
                continue;
            }

            cpuPercent += processorTime.TotalMilliseconds
                / elapsed.TotalMilliseconds
                / Environment.ProcessorCount
                * 100;
        }

        _previousSamples.Clear();
        foreach (var sample in currentSamples)
        {
            _previousSamples[sample.ProcessId] = sample with { Timestamp = now };
        }

        return new ProcessCpuUsage(
            Math.Clamp(cpuPercent, 0, 100),
            currentSamples.Count > 0,
            currentSamples.Count > 0 && matchedPreviousSample);
    }

    public void Reset() => _previousSamples.Clear();

    private static IReadOnlyList<ProcessSample> GetCurrentSamples(string processName)
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

                    samples.Add(new ProcessSample(
                        process.Id,
                        process.TotalProcessorTime,
                        DateTimeOffset.UtcNow));
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

    private sealed record ProcessSample(int ProcessId, TimeSpan TotalProcessorTime, DateTimeOffset Timestamp);
}
