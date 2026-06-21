using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class BitwardenCliClientFactoryTests
{
    [Fact]
    public void Requires_absolute_profile_directory()
    {
        var factory = new BitwardenCliClientFactory(new CapturingRunner());
        var profile = Profile("relative-path");

        Assert.Throws<ArgumentException>(() => factory.Create(profile));
    }

    [Fact]
    public void Returns_same_client_for_same_profile()
    {
        using var temp = new TemporaryDirectory();
        var factory = new BitwardenCliClientFactory(new CapturingRunner());
        var profile = Profile(temp.GetPath("account"));

        var first = factory.Create(profile);
        var second = factory.Create(profile);

        Assert.Same(first, second);
        Assert.Single(factory.Clients);
    }

    [Fact]
    public void Rejects_shared_directory_between_accounts()
    {
        using var temp = new TemporaryDirectory();
        var factory = new BitwardenCliClientFactory(new CapturingRunner());
        var directory = temp.GetPath("shared");
        factory.Create(Profile(directory));

        Assert.Throws<InvalidOperationException>(() => factory.Create(Profile(directory)));
    }

    [Fact]
    public async Task Remove_clears_session_without_deleting_profile_data()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session-key"));
        var factory = new BitwardenCliClientFactory(runner);
        var client = factory.Create(Profile(temp.GetPath("account")));
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var removed = factory.Remove(client.Profile.Id);

        Assert.True(removed);
        Assert.False(client.Session.IsUnlocked);
        Assert.True(Directory.Exists(client.Profile.CliDataDirectory));
    }

    private static BitwardenAccountProfile Profile(string directory) => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = "Test account",
        CliDataDirectory = directory
    };
}
