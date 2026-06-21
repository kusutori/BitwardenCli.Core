namespace BitwardenCli.Core.Execution;

/// <summary>Represents one process argument and whether diagnostics must redact it.</summary>
public sealed record CliArgument
{
    /// <summary>Creates a process argument.</summary>
    public CliArgument(string value, bool isSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
        IsSensitive = isSensitive;
    }

    /// <summary>Gets the argument value passed to the child process.</summary>
    public string Value { get; }

    /// <summary>Gets whether the value must be removed from diagnostics.</summary>
    public bool IsSensitive { get; }

    /// <summary>Creates a non-sensitive argument.</summary>
    public static CliArgument Plain(string value) => new(value);

    /// <summary>Creates a sensitive argument.</summary>
    public static CliArgument Secret(string value) => new(value, true);
}
