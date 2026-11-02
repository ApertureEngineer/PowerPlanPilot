namespace PowerPlanPilot;

internal sealed class ProcessCpuSampler
{
    private readonly IProcessCpuSampleSource _sampleSource;
    private readonly Dictionary<int, TimestampedProcessSample> _previousSamples = [];

    public ProcessCpuSampler(IProcessCpuSampleSource? sampleSource = null)
    {
        _sampleSource = sampleSource ?? new SystemProcessService();
    }

    public ProcessCpuUsage GetCpuUsagePercent(IReadOnlyList<string> processNames)
    {
        var currentSamples = processNames
            .SelectMany(_sampleSource.GetCurrentSamples)
            .DistinctBy(sample => sample.ProcessId)
            .ToArray();
        var now = DateTimeOffset.UtcNow;
        double cpuPercent = 0;
        var hasBaseline = currentSamples.Length > 0;

        foreach (var sample in currentSamples)
        {
            if (!_previousSamples.TryGetValue(sample.ProcessId, out var previous))
            {
                hasBaseline = false;
                continue;
            }

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
            _previousSamples[sample.ProcessId] = new TimestampedProcessSample(sample.ProcessId, sample.TotalProcessorTime, now);
        }

        return new ProcessCpuUsage(
            Math.Clamp(cpuPercent, 0, 100),
            currentSamples.Length > 0,
            hasBaseline);
    }

    public void Reset() => _previousSamples.Clear();

    private sealed record TimestampedProcessSample(int ProcessId, TimeSpan TotalProcessorTime, DateTimeOffset Timestamp);
}
