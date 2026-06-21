using System.Globalization;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Synchronization;

/// <summary>Provides vault synchronization commands.</summary>
public sealed class SynchronizationClient
{
    private readonly AccountCommandContext _context;

    internal SynchronizationClient(AccountCommandContext context)
    {
        _context = context;
    }

    /// <summary>Synchronizes the account vault.</summary>
    public async Task<CliResult> SyncAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Session.IsUnlocked)
        {
            return AccountResultFactory.MissingSession();
        }

        var arguments = new List<CliArgument> { CliArgument.Plain("sync") };
        if (force)
        {
            arguments.Add(CliArgument.Plain("--force"));
        }

        var process = await _context.RunAsync(
            new CliCommand("sync", arguments.ToArray()),
            includeSession: true,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
        return CliResultFactory.FromProcess(process);
    }

    /// <summary>Gets the last successful vault synchronization time.</summary>
    public async Task<CliResult<DateTimeOffset?>> GetLastSyncAsync(
        CancellationToken cancellationToken = default)
    {
        var process = await _context.RunAsync(
            new CliCommand(
                "sync-last",
                CliArgument.Plain("sync"),
                CliArgument.Plain("--last")),
            includeSession: true,
            serializeMutation: false,
            cancellationToken).ConfigureAwait(false);
        if (!process.IsSuccess)
        {
            return CliResultFactory.Failure<DateTimeOffset?>(process);
        }

        if (string.IsNullOrWhiteSpace(process.StandardOutput) ||
            string.Equals(process.StandardOutput, "null", StringComparison.OrdinalIgnoreCase))
        {
            return CliResultFactory.Success<DateTimeOffset?>(null, process);
        }

        return DateTimeOffset.TryParse(
            process.StandardOutput,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var timestamp)
            ? CliResultFactory.Success<DateTimeOffset?>(timestamp, process)
            : CliResultFactory.InvalidResponse<DateTimeOffset?>(
                process,
                $"Bitwarden CLI returned an invalid sync timestamp: {process.StandardOutput}");
    }
}
