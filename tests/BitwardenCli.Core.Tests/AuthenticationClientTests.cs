using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class AuthenticationClientTests
{
    [Fact]
    public async Task Status_uses_isolated_profile_directory()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success(
            "{\"serverUrl\":null,\"lastSync\":null,\"status\":\"unauthenticated\"}"));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.GetStatusAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("unauthenticated", result.Value?.Status);
        var appData = Assert.Single(Assert.Single(runner.Commands).Environment,
            value => value.Name == "BITWARDENCLI_APPDATA_DIR");
        Assert.Equal(client.Profile.CliDataDirectory, appData.Value);
    }

    [Fact]
    public async Task Unlock_passes_password_in_environment_and_keeps_session_in_memory()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session-key"));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("master-password"));

        Assert.True(result.IsSuccess);
        Assert.True(client.Session.IsUnlocked);
        var command = Assert.Single(runner.Commands);
        Assert.DoesNotContain(command.Arguments, value => value.Value == "master-password");
        var passwordVariable = Assert.Single(command.Environment,
            value => value.Name.StartsWith("BW_PASSWORD_", StringComparison.Ordinal));
        Assert.True(passwordVariable.IsSensitive);
        Assert.Equal("master-password", passwordVariable.Value);
    }

    [Fact]
    public async Task Password_login_returns_session_without_password_argument()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(
            CapturingRunner.Success("login-session"),
            CapturingRunner.Success(
                "{\"serverUrl\":\"https://vault.bitwarden.com\",\"userEmail\":\"user@example.com\",\"userId\":\"id\",\"status\":\"unlocked\"}"));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.LoginAsync(
            new PasswordLoginRequest("user@example.com"),
            DelegateSecretProvider.FromMasterPassword("master-password"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.HasSession);
        Assert.True(client.Session.IsUnlocked);
        var login = runner.Commands[0];
        Assert.DoesNotContain(login.Arguments, value => value.Value == "master-password");
        Assert.Contains(login.Environment, value => value.IsSensitive && value.Value == "master-password");
        Assert.Contains(runner.Commands[1].Environment, value => value.Name == "BW_SESSION" && value.IsSensitive);
    }

    [Fact]
    public async Task Api_key_login_uses_secret_environment_and_remains_locked()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(
            CapturingRunner.Success("You are logged in!"),
            CapturingRunner.Success("{\"status\":\"locked\"}"));
        var client = CreateClient(temp.GetPath("account"), runner);
        var secrets = new DelegateSecretProvider((_, purpose, _) =>
            ValueTask.FromResult<string?>(purpose == SecretPurpose.ApiClientSecret ? "client-secret" : null));

        var result = await client.LoginAsync(new ApiKeyLoginRequest("client-id"), secrets);

        Assert.True(result.IsSuccess);
        Assert.False(client.Session.IsUnlocked);
        var login = runner.Commands[0];
        Assert.Contains(login.Environment, value =>
            value.Name == "BW_CLIENTID" && value.Value == "client-id" && value.IsSensitive);
        Assert.Contains(login.Environment, value =>
            value.Name == "BW_CLIENTSECRET" && value.Value == "client-secret" && value.IsSensitive);
        Assert.DoesNotContain(login.Arguments, value => value.Value is "client-id" or "client-secret");
    }

    [Fact]
    public async Task Two_factor_code_uses_sensitive_standard_input()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(
            CapturingRunner.Success("login-session"),
            CapturingRunner.Success("{\"status\":\"unlocked\"}"));
        var client = CreateClient(temp.GetPath("account"), runner);
        var secrets = new DelegateSecretProvider((_, purpose, _) => ValueTask.FromResult<string?>(purpose switch
        {
            SecretPurpose.MasterPassword => "master-password",
            SecretPurpose.TwoFactorCode => "123456",
            _ => null
        }));

        var result = await client.LoginAsync(
            new PasswordLoginRequest("user@example.com", TwoFactorMethod: 1, IncludeTwoFactorCode: true),
            secrets);

        Assert.True(result.IsSuccess);
        var command = runner.Commands[0];
        Assert.True(command.IsStandardInputSensitive);
        Assert.Contains("123456", command.StandardInput, StringComparison.Ordinal);
        Assert.DoesNotContain(command.Arguments, argument => argument.Value == "123456");
        Assert.False(command.NoInteraction);
    }

    [Fact]
    public async Task Lock_clears_session_only_after_success()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(
            CapturingRunner.Success("session-key"),
            CapturingRunner.Success());
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.LockAsync();

        Assert.True(result.IsSuccess);
        Assert.False(client.Session.IsUnlocked);
    }

    [Fact]
    public async Task Missing_secret_returns_invalid_arguments()
    {
        using var temp = new TemporaryDirectory();
        var client = CreateClient(temp.GetPath("account"), new CapturingRunner());

        var result = await client.LoginAsync(new ApiKeyLoginRequest("client-id"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.InvalidArguments, result.Error?.Code);
    }

    private static BitwardenCliClient CreateClient(string directory, IBitwardenCliRunner runner) =>
        new BitwardenCliClientFactory(runner).Create(new BitwardenAccountProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test account",
            CliDataDirectory = directory
        });
}
