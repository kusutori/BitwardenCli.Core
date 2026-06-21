using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Internal;

internal sealed class AccountCommandContext
{
    private readonly IBitwardenCliRunner _runner;
    private readonly SemaphoreSlim _mutationGate = new(1, 1);

    public AccountCommandContext(
        BitwardenAccountProfile profile,
        BitwardenSessionState session,
        IBitwardenCliRunner runner)
    {
        Profile = profile;
        Session = session;
        _runner = runner;
    }

    public BitwardenAccountProfile Profile { get; private set; }

    public BitwardenSessionState Session { get; }

    public void UpdateProfile(Func<BitwardenAccountProfile, BitwardenAccountProfile> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        Profile = update(Profile);
    }

    public async Task<CliProcessResult> RunAsync(
        CliCommand command,
        bool includeSession,
        bool serializeMutation,
        CancellationToken cancellationToken)
    {
        var environment = new List<CliEnvironmentVariable>(command.Environment)
        {
            CliEnvironmentVariable.Plain("BITWARDENCLI_APPDATA_DIR", Profile.CliDataDirectory)
        };
        if (includeSession && Session.GetSessionKey() is { } sessionKey)
        {
            environment.Add(CliEnvironmentVariable.Secret("BW_SESSION", sessionKey));
        }

        var scopedCommand = command with { Environment = environment };
        if (!serializeMutation)
        {
            return await _runner.RunAsync(scopedCommand, cancellationToken).ConfigureAwait(false);
        }

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _runner.RunAsync(scopedCommand, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutationGate.Release();
        }
    }
}
