namespace BitwardenCli.Core.Execution;

/// <summary>Describes one Bitwarden CLI process invocation.</summary>
public sealed record CliCommand
{
    /// <summary>Creates a CLI command.</summary>
    public CliCommand(string operation, params CliArgument[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(arguments);
        Operation = operation;
        Arguments = arguments;
    }

    /// <summary>Gets the non-sensitive operation name used by diagnostics.</summary>
    public string Operation { get; }

    /// <summary>Gets the ordered process arguments.</summary>
    public IReadOnlyList<CliArgument> Arguments { get; init; }

    /// <summary>Gets environment variables for this invocation.</summary>
    public IReadOnlyList<CliEnvironmentVariable> Environment { get; init; } = [];

    /// <summary>Gets optional content written to standard input.</summary>
    public string? StandardInput { get; init; }

    /// <summary>Gets whether standard input contains secret material.</summary>
    public bool IsStandardInputSensitive { get; init; }

    /// <summary>Gets an invocation-specific timeout override.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Gets whether the global <c>--nointeraction</c> option is prepended.</summary>
    public bool NoInteraction { get; init; } = true;

    /// <summary>Returns argument values safe for diagnostics.</summary>
    public IReadOnlyList<string> GetSafeArguments() =>
        Arguments.Select(argument => argument.IsSensitive ? "[REDACTED]" : argument.Value).ToArray();

    /// <summary>Returns environment variable names without their values.</summary>
    public IReadOnlyList<string> GetEnvironmentNames() =>
        Environment.Select(variable => variable.Name).ToArray();
}
