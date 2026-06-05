using System.Diagnostics;
using System.Text;

namespace PowerPlanPilot;

internal interface IPowerCfgRunner
{
    PowerCfgResult Run(string arguments);
}

internal sealed class PowerCfgRunner : IPowerCfgRunner
{
    private const int TimeoutMilliseconds = 10_000;

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
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeoutMilliseconds))
        {
            KillProcess(process);

            var timeoutOutput = ReadCompletedOutput(outputTask);
            var timeoutError = ReadCompletedOutput(errorTask);
            var errorMessage = $"powercfg {arguments} timed out after {TimeoutMilliseconds / 1000} seconds.";

            if (!string.IsNullOrWhiteSpace(timeoutError))
            {
                errorMessage += Environment.NewLine + timeoutError.Trim();
            }

            return new PowerCfgResult(-1, timeoutOutput, errorMessage);
        }

        process.WaitForExit();
        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        return new PowerCfgResult(process.ExitCode, output, error);
    }

    private static string ReadCompletedOutput(Task<string> outputTask)
    {
        return outputTask.IsCompletedSuccessfully
            ? outputTask.GetAwaiter().GetResult()
            : string.Empty;
    }

    private static void KillProcess(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}

internal sealed record PowerCfgResult(int ExitCode, string Output, string Error);
