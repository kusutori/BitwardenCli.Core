namespace BitwardenCli.Core.Results;

/// <summary>Represents a CLI operation that returns a value.</summary>
public sealed record CliResult<T>
{
    private CliResult(
        bool isSuccess,
        T? value,
        CliError? error,
        int exitCode,
        string standardError,
        TimeSpan duration)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ExitCode = exitCode;
        StandardError = standardError;
        Duration = duration;
    }

    /// <summary>Gets whether the operation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the result value when successful.</summary>
    public T? Value { get; }

    /// <summary>Gets the categorized failure.</summary>
    public CliError? Error { get; }

    /// <summary>Gets the child process exit code.</summary>
    public int ExitCode { get; }

    /// <summary>Gets standard error returned by the CLI.</summary>
    public string StandardError { get; }

    /// <summary>Gets the command duration.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Creates a successful result.</summary>
    public static CliResult<T> Success(T value, int exitCode, string standardError, TimeSpan duration) =>
        new(true, value, null, exitCode, standardError, duration);

    /// <summary>Creates a failed result.</summary>
    public static CliResult<T> Failure(CliError error, int exitCode, string standardError, TimeSpan duration) =>
        new(false, default, error, exitCode, standardError, duration);
}
