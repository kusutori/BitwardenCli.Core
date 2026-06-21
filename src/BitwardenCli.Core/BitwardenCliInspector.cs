using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core;

/// <summary>Inspects the installed Bitwarden CLI executable.</summary>
public sealed class BitwardenCliInspector
{
    private readonly IBitwardenCliRunner _runner;

    /// <summary>Creates a CLI inspector.</summary>
    public BitwardenCliInspector(IBitwardenCliRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <summary>Gets the installed CLI version.</summary>
    public async Task<CliResult<Version>> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var process = await _runner.RunAsync(
            new CliCommand("version", CliArgument.Plain("--version")),
            cancellationToken).ConfigureAwait(false);
        if (!process.IsSuccess)
        {
            return CliResultFactory.Failure<Version>(process);
        }

        return Version.TryParse(process.StandardOutput.Trim(), out var version)
            ? CliResultFactory.Success(version, process)
            : CliResultFactory.InvalidResponse<Version>(
                process,
                $"The Bitwarden CLI returned an invalid version: {process.StandardOutput}");
    }

    /// <summary>Checks whether the installed CLI meets a minimum version.</summary>
    public async Task<CliResult<Version>> CheckMinimumVersionAsync(
        Version minimumVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(minimumVersion);
        var result = await GetVersionAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null || result.Value >= minimumVersion)
        {
            return result;
        }

        return CliResult<Version>.Failure(
            new CliError(
                CliErrorCode.UnsupportedVersion,
                $"Bitwarden CLI {result.Value} is older than the required version {minimumVersion}."),
            result.ExitCode,
            result.StandardError,
            result.Duration);
    }
}
