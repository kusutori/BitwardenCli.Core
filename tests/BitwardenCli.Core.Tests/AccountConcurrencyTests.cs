using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class AccountConcurrencyTests
{
    [Fact]
    public async Task Same_account_mutations_are_serialized()
    {
        using var temp = new TemporaryDirectory();
        var runner = new TrackingRunner();
        var client = CreateClient(new BitwardenCliClientFactory(runner), temp.GetPath("account"));
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));
        runner.Reset();

        await Task.WhenAll(
            client.Synchronization.SyncAsync(),
            client.Synchronization.SyncAsync());

        Assert.Equal(1, runner.MaximumConcurrentSyncs);
    }

    [Fact]
    public async Task Different_accounts_can_mutate_in_parallel()
    {
        using var temp = new TemporaryDirectory();
        var runner = new TrackingRunner();
        var factory = new BitwardenCliClientFactory(runner);
        var first = CreateClient(factory, temp.GetPath("first"));
        var second = CreateClient(factory, temp.GetPath("second"));
        await first.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));
        await second.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));
        runner.Reset();

        await Task.WhenAll(
            first.Synchronization.SyncAsync(),
            second.Synchronization.SyncAsync());

        Assert.True(runner.MaximumConcurrentSyncs >= 2);
    }

    private static BitwardenCliClient CreateClient(BitwardenCliClientFactory factory, string directory) =>
        factory.Create(new BitwardenAccountProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test account",
            CliDataDirectory = directory
        });

    private sealed class TrackingRunner : IBitwardenCliRunner
    {
        private int _activeSyncs;
        private int _maximumConcurrentSyncs;

        public int MaximumConcurrentSyncs => Volatile.Read(ref _maximumConcurrentSyncs);

        public void Reset()
        {
            Volatile.Write(ref _activeSyncs, 0);
            Volatile.Write(ref _maximumConcurrentSyncs, 0);
        }

        public async Task<CliProcessResult> RunAsync(
            CliCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command.Operation == "unlock")
            {
                return CapturingRunner.Success("session-key");
            }

            if (command.Operation != "sync")
            {
                return CapturingRunner.Success();
            }

            var active = Interlocked.Increment(ref _activeSyncs);
            UpdateMaximum(active);
            try
            {
                await Task.Delay(150, cancellationToken);
                return CapturingRunner.Success();
            }
            finally
            {
                Interlocked.Decrement(ref _activeSyncs);
            }
        }

        private void UpdateMaximum(int candidate)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrentSyncs);
                if (candidate <= current ||
                    Interlocked.CompareExchange(ref _maximumConcurrentSyncs, candidate, current) == current)
                {
                    return;
                }
            }
        }
    }
}
