using Microsoft.Win32;

namespace PowerPlanPilot;

internal static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PowerPlanPilot";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var configuredCommand = key?.GetValue(ValueName) as string;

        return string.Equals(configuredCommand, GetStartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open the Windows autostart registry key.");

        if (enabled)
        {
            key.SetValue(ValueName, GetStartupCommand(), RegistryValueKind.String);
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string GetStartupCommand()
    {
        var executablePath = Environment.ProcessPath ?? Application.ExecutablePath;
        return $"\"{executablePath}\"";
    }
}
