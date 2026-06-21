namespace BitwardenCli.Core.Execution;

/// <summary>Represents an environment variable supplied to a CLI process.</summary>
public sealed record CliEnvironmentVariable
{
    /// <summary>Creates an environment variable.</summary>
    public CliEnvironmentVariable(string name, string value, bool isSensitive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        Name = name;
        Value = value;
        IsSensitive = isSensitive;
    }

    /// <summary>Gets the variable name.</summary>
    public string Name { get; }

    /// <summary>Gets the variable value.</summary>
    public string Value { get; }

    /// <summary>Gets whether diagnostics must redact the value.</summary>
    public bool IsSensitive { get; }

    /// <summary>Creates a non-sensitive variable.</summary>
    public static CliEnvironmentVariable Plain(string name, string value) => new(name, value);

    /// <summary>Creates a sensitive variable.</summary>
    public static CliEnvironmentVariable Secret(string name, string value) => new(name, value, true);
}
