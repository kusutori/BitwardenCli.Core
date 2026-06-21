using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class SynchronizationClientTests
{
    [Fact]
    public async Task Sync_requires_session()
    {
        using var temp = new TemporaryDirectory();
        var client = CreateClient(temp.GetPath("account"), new CapturingRunner());

        var result = await client.Synchronization.SyncAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.VaultLocked, result.Error?.Code);
    }

    [Fact]
    public async Task Sync_uses_session_environment_and_force_option()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(
            CapturingRunner.Success("session-key"),
            CapturingRunner.Success());
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Synchronization.SyncAsync(force: true);

        Assert.True(result.IsSuccess);
        var sync = runner.Commands[1];
        Assert.Contains(sync.Arguments, value => value.Value == "--force");
        Assert.Contains(sync.Environment, value =>
            value.Name == "BW_SESSION" && value.Value == "session-key" && value.IsSensitive);
    }

    [Fact]
    public async Task Parses_last_sync_timestamp()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("2026-06-21T01:02:03.000Z"));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.Synchronization.GetLastSyncAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2026, result.Value?.Year);
    }

    private static BitwardenCliClient CreateClient(string directory, IBitwardenCliRunner runner) =>
        new BitwardenCliClientFactory(runner).Create(new BitwardenAccountProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test account",
            CliDataDirectory = directory
        });
}
