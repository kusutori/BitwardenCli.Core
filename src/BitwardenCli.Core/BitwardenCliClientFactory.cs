using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;

namespace BitwardenCli.Core;

/// <summary>Creates and tracks isolated account-scoped CLI clients.</summary>
public sealed class BitwardenCliClientFactory
{
    private readonly object _sync = new();
    private readonly IBitwardenCliRunner _runner;
    private readonly Dictionary<Guid, BitwardenCliClient> _clients = [];
    private readonly Dictionary<string, Guid> _directoryOwners;

    /// <summary>Creates a factory backed by a process runner.</summary>
    public BitwardenCliClientFactory(BitwardenCliOptions? options = null)
        : this(new ProcessBitwardenCliRunner(options))
    {
    }

    /// <summary>Creates a factory backed by a custom runner.</summary>
    public BitwardenCliClientFactory(IBitwardenCliRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _directoryOwners = new Dictionary<string, Guid>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    /// <summary>Gets a snapshot of clients created by this factory.</summary>
    public IReadOnlyCollection<BitwardenCliClient> Clients
    {
        get
        {
            lock (_sync)
            {
                return _clients.Values.ToArray();
            }
        }
    }

    /// <summary>Creates or returns the client for a profile.</summary>
    public BitwardenCliClient Create(BitwardenAccountProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var normalized = NormalizeProfile(profile);
        lock (_sync)
        {
            if (_clients.TryGetValue(normalized.Id, out var existing))
            {
                if (!PathsEqual(existing.Profile.CliDataDirectory, normalized.CliDataDirectory))
                {
                    throw new InvalidOperationException("An account ID cannot be assigned to multiple CLI data directories.");
                }

                return existing;
            }

            if (_directoryOwners.TryGetValue(normalized.CliDataDirectory, out var owner) && owner != normalized.Id)
            {
                throw new InvalidOperationException("Two account profiles cannot share a CLI data directory.");
            }

            Directory.CreateDirectory(normalized.CliDataDirectory);
            var session = new BitwardenSessionState();
            var context = new AccountCommandContext(normalized, session, _runner);
            var client = new BitwardenCliClient(context);
            _directoryOwners.Add(normalized.CliDataDirectory, normalized.Id);
            _clients.Add(normalized.Id, client);
            return client;
        }
    }

    /// <summary>Removes a client from this factory without deleting its CLI data.</summary>
    public bool Remove(Guid accountId)
    {
        lock (_sync)
        {
            if (!_clients.Remove(accountId, out var client))
            {
                return false;
            }

            client.Session.Clear();
            _directoryOwners.Remove(client.Profile.CliDataDirectory);
            return true;
        }
    }

    private static BitwardenAccountProfile NormalizeProfile(BitwardenAccountProfile profile)
    {
        if (profile.Id == Guid.Empty)
        {
            throw new ArgumentException("Account ID cannot be empty.", nameof(profile));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(profile.DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.CliDataDirectory);
        if (!Path.IsPathFullyQualified(profile.CliDataDirectory))
        {
            throw new ArgumentException("CLI data directory must be an absolute path.", nameof(profile));
        }

        if (!string.IsNullOrWhiteSpace(profile.ServerUrl) &&
            (!Uri.TryCreate(profile.ServerUrl, UriKind.Absolute, out var serverUri) ||
             (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new ArgumentException("Server URL must be an absolute HTTP or HTTPS URL.", nameof(profile));
        }

        return profile with { CliDataDirectory = Path.GetFullPath(profile.CliDataDirectory) };
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
