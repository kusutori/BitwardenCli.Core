namespace BitwardenCli.Core.Execution;

/// <summary>Executes commands against a Bitwarden CLI process.</summary>
public interface IBitwardenCliRunner
{
    /// <summary>Executes a command asynchronously.</summary>
    Task<CliProcessResult> RunAsync(CliCommand command, CancellationToken cancellationToken = default);
}
