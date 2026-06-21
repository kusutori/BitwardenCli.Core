using System.Text.Json.Nodes;
using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class CommandCoverageTests
{
    [Fact]
    public async Task Status_updates_non_sensitive_profile_metadata()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("""{"status":"locked","userEmail":"user@example.com","userId":"user-id","serverUrl":"https://vault.example"}"""));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.GetStatusAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", client.Profile.Email);
        Assert.Equal("user-id", client.Profile.UserId);
        Assert.Equal("https://vault.example", client.Profile.ServerUrl);
        Assert.NotNull(client.Profile.LastUsedAt);
    }

    [Fact]
    public async Task Template_and_fingerprint_commands_are_exposed()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("{}"), CapturingRunner.Success("fingerprint"));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        Assert.True((await client.Vault.GetTemplateAsync("item")).IsSuccess);
        Assert.Equal("fingerprint", (await client.Vault.GetFingerprintAsync("phrase")).Value);
        Assert.Equal(["get", "template", "item"], runner.Commands[1].Arguments.Select(x => x.Value));
        Assert.Equal(["get", "fingerprint", "phrase"], runner.Commands[2].Arguments.Select(x => x.Value));
    }

    [Fact]
    public async Task Organization_collection_write_uses_sensitive_stdin()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("{}"));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Organizations.CreateOrganizationCollectionAsync("org-id", new JsonObject { ["name"] = "Team" });

        Assert.True(result.IsSuccess);
        Assert.True(runner.Commands[1].IsStandardInputSensitive);
        Assert.Equal(["create", "org-collection", "--organizationid", "org-id"], runner.Commands[1].Arguments.Select(x => x.Value));
    }

    private static BitwardenCliClient CreateClient(string directory, IBitwardenCliRunner runner) =>
        new BitwardenCliClientFactory(runner).Create(new BitwardenAccountProfile { Id = Guid.NewGuid(), DisplayName = "Test", CliDataDirectory = directory });
}
