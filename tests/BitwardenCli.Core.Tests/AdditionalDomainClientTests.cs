using System.Text;
using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Generator;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Tests.TestSupport;

namespace BitwardenCli.Core.Tests;

public sealed class AdditionalDomainClientTests
{
    [Fact]
    public async Task Folder_create_uses_sensitive_stdin()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("""{"object":"folder","id":"id","name":"Work"}"""));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Folders.CreateAsync("Work");

        Assert.True(result.IsSuccess);
        var command = runner.Commands[1];
        Assert.True(command.IsStandardInputSensitive);
        Assert.Contains("Work", Encoding.UTF8.GetString(Convert.FromBase64String(command.StandardInput!)));
        Assert.Equal(["create", "folder"], command.Arguments.Select(x => x.Value));
    }

    [Fact]
    public async Task Attachment_upload_requires_absolute_path_and_session()
    {
        using var temp = new TemporaryDirectory();
        var client = CreateClient(temp.GetPath("account"), new CapturingRunner());

        await Assert.ThrowsAsync<ArgumentException>(() => client.Attachments.UploadAsync("item", "relative.txt"));
        var result = await client.Attachments.UploadAsync("item", temp.GetPath("file.txt"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.VaultLocked, result.Error?.Code);
    }

    [Fact]
    public async Task Organization_collections_include_organization_filter()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("session"), CapturingRunner.Success("[]"));
        var client = CreateClient(temp.GetPath("account"), runner);
        await client.UnlockAsync(DelegateSecretProvider.FromMasterPassword("password"));

        var result = await client.Organizations.ListOrganizationCollectionsAsync("org-id");

        Assert.True(result.IsSuccess);
        Assert.Equal(["list", "org-collections", "--organizationid", "org-id"], runner.Commands[1].Arguments.Select(x => x.Value));
    }

    [Fact]
    public async Task Generator_does_not_require_session()
    {
        using var temp = new TemporaryDirectory();
        var runner = new CapturingRunner(CapturingRunner.Success("generated"));
        var client = CreateClient(temp.GetPath("account"), runner);

        var result = await client.Generator.GeneratePasswordAsync(new PasswordGenerationOptions { Length = 20, Special = true, AvoidAmbiguous = true });

        Assert.True(result.IsSuccess);
        Assert.Equal("generated", result.Value);
        Assert.Contains(runner.Commands[0].Arguments, x => x.Value == "--special");
        Assert.Contains(runner.Commands[0].Arguments, x => x.Value == "--ambiguous");
        Assert.DoesNotContain(runner.Commands[0].Environment, x => x.Name == "BW_SESSION");
    }

    private static BitwardenCliClient CreateClient(string directory, IBitwardenCliRunner runner) =>
        new BitwardenCliClientFactory(runner).Create(new BitwardenAccountProfile
        {
            Id = Guid.NewGuid(), DisplayName = "Test account", CliDataDirectory = directory
        });
}
