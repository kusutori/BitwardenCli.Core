namespace BitwardenCli.Core.Execution;

/// <summary>Describes how a child process invocation ended.</summary>
public enum CliProcessOutcome
{
    /// <summary>The process exited normally.</summary>
    Completed,

    /// <summary>The caller cancelled the operation.</summary>
    Cancelled,

    /// <summary>The configured timeout elapsed.</summary>
    TimedOut,

    /// <summary>The process could not be started.</summary>
    StartFailed
}
