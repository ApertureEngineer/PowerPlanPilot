using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PowerPlanPilot;

internal readonly record struct SystemCpuTimes(ulong Idle, ulong Kernel, ulong User);

internal sealed class SystemCpuSampler
{
    private readonly Func<SystemCpuTimes> _getTimes;
    private SystemCpuTimes? _previous;

    public SystemCpuSampler(Func<SystemCpuTimes>? getTimes = null)
    {
        _getTimes = getTimes ?? ReadSystemTimes;
    }

    public double? GetCpuUsagePercent()
    {
        var current = _getTimes();
        var previous = _previous;
        _previous = current;
        if (previous is null || current.Idle < previous.Value.Idle ||
            current.Kernel < previous.Value.Kernel || current.User < previous.Value.User)
        {
            return null;
        }

        // Kernel time includes idle time in GetSystemTimes.
        var idle = current.Idle - previous.Value.Idle;
        var total = (current.Kernel - previous.Value.Kernel) + (current.User - previous.Value.User);
        return total == 0 || idle > total ? null : 100.0 * (total - idle) / total;
    }

    public void Reset() => _previous = null;

    private static SystemCpuTimes ReadSystemTimes()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new SystemCpuTimes(idle.Value, kernel.Value, user.Value);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        private uint _low;
        private uint _high;

        public readonly ulong Value => ((ulong)_high << 32) | _low;
    }
}
