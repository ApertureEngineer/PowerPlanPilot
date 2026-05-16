using System.Diagnostics;
using System.Text;

namespace PowerPlanPilot;

internal interface IPowerCfgRunner
{
    PowerCfgResult Run(string arguments);
}

internal sealed class PowerCfgRunner : IPowerCfgRunner
{
    public PowerCfgResult Run(string arguments)
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

        return new PowerCfgResult(process.ExitCode, output, error);
    }
}

internal sealed record PowerCfgResult(int ExitCode, string Output, string Error);
