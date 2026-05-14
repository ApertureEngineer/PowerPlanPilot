namespace PowerPlanPilot;

internal sealed class PowerPlanCommandException : Exception
{
    public PowerPlanCommandException(string message, int exitCode, string output, string error)
        : base(message)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
    }

    public int ExitCode { get; }

    public string Output { get; }

    public string Error { get; }
}
