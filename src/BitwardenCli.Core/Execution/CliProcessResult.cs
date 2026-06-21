namespace BitwardenCli.Core.Execution;

/// <summary>Contains the raw, complete result of a CLI process invocation.</summary>
public sealed record CliProcessResult(
    CliProcessOutcome Outcome,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    string? FailureMessage = null)
{
    /// <summary>Gets whether the process completed with exit code zero.</summary>
    public bool IsSuccess => Outcome == CliProcessOutcome.Completed && ExitCode == 0;
}
