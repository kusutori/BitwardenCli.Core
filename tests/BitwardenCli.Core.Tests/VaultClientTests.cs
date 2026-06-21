using System.Text;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class VaultClientTests
{
    [Fact]
    public async Task List_items_requires_unlocked_session()
    {
        using var temp = new TemporaryDirectory();
        var client = CreateClient(temp.GetPath("account"), new CapturingRunner());

        var result = await client.Vault.ListItemsAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.VaultLocked, result.Error?.Code);
    }

    [Fact]
    public async Task List_items_builds_filters_and_scopes_session()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("[]"));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Vault.ListItemsAsync(new VaultItemQuery
        {
            Search = "mail",
            FolderId = "folder-id",
            Trash = true
        });

        Assert.True(result.IsSuccess);
        var command = runner.Commands[1];
        Assert.Equal(["list", "items", "--search", "mail", "--folderid", "folder-id", "--trash"], command.Arguments.Select(x => x.Value));
        Assert.Contains(command.Environment, x => x.Name == "BW_SESSION" && x.Value == "session" && x.IsSensitive);
    }

    [Fact]
    public async Task Create_item_sends_encoded_document_only_through_sensitive_stdin()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success(ItemJson("created")));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Vault.CreateItemAsync(new JsonObject { ["type"] = 1, ["name"] = "secret item" });

        Assert.True(result.IsSuccess);
        var command = runner.Commands[1];
        Assert.Equal(["create", "item"], command.Arguments.Select(x => x.Value));
        Assert.True(command.IsStandardInputSensitive);
        Assert.NotNull(command.StandardInput);
        Assert.Contains("secret item", Encoding.UTF8.GetString(Convert.FromBase64String(command.StandardInput)));
        Assert.DoesNotContain(command.Arguments, x => x.Value.Contains("secret item", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Edit_item_preserves_unknown_properties()
    {
        using var temp = new TemporaryDirectory();
        var source = """{"object":"item","id":"item-id","type":1,"name":"before","futureField":{"enabled":true}}""";
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success(source), CapturingRunner.Success(ItemJson("after")));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Vault.EditItemAsync("item-id", new JsonObject { ["name"] = "after" });

        Assert.True(result.IsSuccess);
        var document = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(runner.Commands[2].StandardInput!)))!.AsObject();
        Assert.Equal("after", document["name"]!.GetValue<string>());
        Assert.True(document["futureField"]!["enabled"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Invalid_item_json_returns_invalid_response()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("not-json"));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Vault.GetItemAsync("item-id");

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.InvalidResponse, result.Error?.Code);
    }

    private static string ItemJson(string name) => new JsonObject
    {
        ["object"] = "item",
        ["id"] = "item-id",
        ["type"] = 1,
        ["name"] = name,
        ["login"] = new JsonObject { ["username"] = "user" }
    }.ToJsonString();

    private static BitwardenCliClient CreateClient(string directory, IBitwardenCliRunner runner) =>
        new BitwardenCliClientFactory(runner).Create(new BitwardenAccountProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test account",
            CliDataDirectory = directory
        });
}
