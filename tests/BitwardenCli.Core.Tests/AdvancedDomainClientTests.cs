using System.Text.Json.Nodes;
using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.ImportExport;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class AdvancedDomainClientTests
{
    [Fact]
    public async Task Send_receive_places_password_only_in_sensitive_environment()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("content"));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.Sends.ReceiveTextAsync("https://send.example/id", "send-secret");

        Assert.True(result.IsSuccess);
        var command = Assert.Single(runner.Commands);
        Assert.DoesNotContain(command.Arguments, x => x.Value.Contains("send-secret", StringComparison.Ordinal));
        Assert.Contains(command.Environment, x => x.Name == "BWCLI_SEND_PASSWORD" && x.Value == "send-secret" && x.IsSensitive);
        Assert.Contains(command.Arguments, x => x.Value == "--passwordenv");
    }

    [Fact]
    public async Task Send_create_uses_sensitive_json_stdin()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("""{"id":"send-id","name":"Test"}"""));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Sends.CreateAsync(new JsonObject { ["name"] = "Test", ["password"] = "send-password" });

        Assert.True(result.IsSuccess);
        Assert.True(runner.Commands[1].IsStandardInputSensitive);
        Assert.DoesNotContain(runner.Commands[1].Arguments, x => x.Value.Contains("send-password", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Advanced_json_models_keep_unknown_fields()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("""[{"object":"send","id":"send-id","name":"Test","type":0,"future":{"enabled":true}}]"""));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Sends.ListAsync();

        Assert.True(result.IsSuccess);
        var send = Assert.Single(result.Value!);
        Assert.True(send.AdditionalProperties!["future"].GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task Export_uses_typed_format_and_absolute_output()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success());
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.ImportExport.ExportAsync(new VaultExportOptions { OutputPath = temp.GetPath("vault.json"), Format = VaultExportFormat.EncryptedJson });

        Assert.True(result.IsSuccess);
        Assert.Contains(runner.Commands[1].Arguments, x => x.Value == "encrypted_json");
        Assert.DoesNotContain(runner.Commands[1].Arguments, x => x.Value == "--password");
        await Assert.ThrowsAsync<ArgumentException>(() => client.ImportExport.ImportAsync("bitwardencsv", "relative.csv"));
    }

    [Fact]
    public async Task Device_approval_is_scoped_to_organization()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success());
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Administration.ApproveDeviceAsync("org-id", "request-id");

        Assert.True(result.IsSuccess);
        Assert.Equal(["device-approval", "approve", "request-id", "--organizationid", "org-id"], runner.Commands[1].Arguments.Select(x => x.Value));
    }

    private static BitwardenCliClient CreateClient(string directory, IBitwardenCliRunner runner) =>
        new BitwardenCliClientFactory(runner).Create(new BitwardenAccountProfile { Id = Guid.NewGuid(), DisplayName = "Test", CliDataDirectory = directory });
}
