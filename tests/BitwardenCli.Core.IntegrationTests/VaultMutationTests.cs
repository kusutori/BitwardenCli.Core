using System.Text.Json.Nodes;
using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Models;

namespace BitwardenCli.Core.IntegrationTests;

public sealed class VaultMutationTests
{
    [AuthenticatedCliIntegrationFact]
    public async Task Create_edit_delete_restore_and_permanent_delete_round_trip()
    {
        var email = Environment.GetEnvironmentVariable("BITWARDEN_CLI_TEST_EMAIL")!;
        var password = Environment.GetEnvironmentVariable("BITWARDEN_CLI_TEST_PASSWORD")!;
        var root = Path.Combine(Path.GetTempPath(), $"BitwardenCli.Core.MutationTests-{Guid.NewGuid():N}");
        string? itemId = null;
        BitwardenCliClient? client = null;
        try
        {
            client = new BitwardenCliClientFactory().Create(new BitwardenAccountProfile
            {
                Id = Guid.NewGuid(), DisplayName = "Mutation test", CliDataDirectory = root
            });
            var login = await client.LoginAsync(
                new PasswordLoginRequest(email),
                DelegateSecretProvider.FromMasterPassword(password));
            Assert.True(login.IsSuccess, login.Error?.Message);

            var marker = $"BitwardenCli.Core test {Guid.NewGuid():N}";
            var created = await client.Vault.CreateItemAsync(new JsonObject
            {
                ["type"] = (int)VaultItemType.Login,
                ["name"] = marker,
                ["login"] = new JsonObject { ["username"] = "integration-test", ["uris"] = new JsonArray() }
            });
            Assert.True(created.IsSuccess, created.Error?.Message);
            itemId = created.Value!.Id;

            var edited = await client.Vault.EditItemAsync(itemId, new JsonObject { ["name"] = marker + " edited" });
            Assert.True(edited.IsSuccess, edited.Error?.Message);
            Assert.Equal(marker + " edited", edited.Value?.Name);

            Assert.True((await client.Vault.DeleteItemAsync(itemId)).IsSuccess);
            var trash = await client.Vault.ListItemsAsync(new VaultItemQuery { Trash = true });
            Assert.Contains(trash.Value ?? [], item => item.Id == itemId);

            Assert.True((await client.Vault.RestoreItemAsync(itemId)).IsSuccess);
            Assert.True((await client.Vault.DeleteItemAsync(itemId, permanent: true)).IsSuccess);
            itemId = null;
        }
        finally
        {
            if (client is not null && itemId is not null)
            {
                await client.Vault.DeleteItemAsync(itemId, permanent: true);
            }
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
