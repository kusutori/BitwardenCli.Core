using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core;

/// <summary>Configures Bitwarden CLI process execution.</summary>
public sealed record BitwardenCliOptions
{
    /// <summary>Gets the executable path or command name.</summary>
    public string ExecutablePath { get; init; } = "bw";

    /// <summary>Gets the default timeout for one CLI invocation.</summary>
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets environment variables applied to every invocation.</summary>
    public IReadOnlyDictionary<string, string> AdditionalEnvironment { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets an optional sink for redacted process diagnostics.</summary>
    public Action<CliDiagnosticEvent>? DiagnosticSink { get; init; }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ExecutablePath);
        if (DefaultTimeout <= TimeSpan.Zero && DefaultTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(DefaultTimeout));
        }
    }
}
