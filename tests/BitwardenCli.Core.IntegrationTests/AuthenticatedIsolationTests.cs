using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;

namespace BitwardenCli.Core.IntegrationTests;

public sealed class AuthenticatedIsolationTests
{
    [AuthenticatedCliIntegrationFact]
    public async Task Login_and_logout_are_isolated_between_profiles()
    {
        var email = Environment.GetEnvironmentVariable("BITWARDEN_CLI_TEST_EMAIL")!;
        var password = Environment.GetEnvironmentVariable("BITWARDEN_CLI_TEST_PASSWORD")!;
        var root = Path.Combine(Path.GetTempPath(), $"BitwardenCli.Core.AuthTests-{Guid.NewGuid():N}");
        try
        {
            var factory = new BitwardenCliClientFactory();
            var first = factory.Create(Profile("First", Path.Combine(root, "first")));
            var second = factory.Create(Profile("Second", Path.Combine(root, "second")));
            var secrets = DelegateSecretProvider.FromMasterPassword(password);

            Assert.True((await first.LoginAsync(new PasswordLoginRequest(email), secrets)).IsSuccess);
            Assert.True((await second.LoginAsync(new PasswordLoginRequest(email), secrets)).IsSuccess);
            Assert.True(first.Session.IsUnlocked);
            Assert.True(second.Session.IsUnlocked);

            Assert.True((await first.LogoutAsync()).IsSuccess);
            Assert.False(first.Session.IsUnlocked);
            Assert.True(second.Session.IsUnlocked);
            Assert.True((await second.GetStatusAsync()).Value?.IsLoggedIn);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static BitwardenAccountProfile Profile(string name, string directory) => new()
    {
        Id = Guid.NewGuid(), DisplayName = name, CliDataDirectory = directory
    };
}
