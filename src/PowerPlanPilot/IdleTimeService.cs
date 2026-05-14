using System.Runtime.InteropServices;

namespace PowerPlanPilot;

internal static class IdleTimeService
{
    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>(),
        };

        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var tickCount = unchecked((uint)Environment.TickCount);
        var idleMilliseconds = unchecked(tickCount - info.Time);
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;

        public uint Time;
    }
}
