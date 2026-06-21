using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.IntegrationTests;

public sealed class ProfileIsolationTests
{
    [CliIntegrationFact]
    public async Task Real_cli_profiles_keep_status_and_server_configuration_isolated()
    {
        using var temp = new TemporaryDirectory();
        var factory = new BitwardenCliClientFactory(new BitwardenCliOptions
        {
            ExecutablePath = Environment.GetEnvironmentVariable("BITWARDEN_CLI_PATH") ?? "bw"
        });
        var first = factory.Create(Profile(temp.GetPath("first"), "First"));
        var second = factory.Create(Profile(temp.GetPath("second"), "Second"));

        var firstStatus = await first.GetStatusAsync();
        var secondStatus = await second.GetStatusAsync();
        var configure = await first.Authentication.ConfigureServerAsync("https://vault.example.test");
        var configuredStatus = await first.GetStatusAsync();
        var untouchedStatus = await second.GetStatusAsync();

        Assert.True(firstStatus.IsSuccess, firstStatus.Error?.Message);
        Assert.True(secondStatus.IsSuccess, secondStatus.Error?.Message);
        Assert.True(configure.IsSuccess, configure.Error?.Message);
        Assert.Equal("unauthenticated", firstStatus.Value?.Status);
        Assert.Equal("unauthenticated", secondStatus.Value?.Status);
        Assert.Equal("https://vault.example.test", configuredStatus.Value?.ServerUrl);
        Assert.Null(untouchedStatus.Value?.ServerUrl);
        Assert.True(File.Exists(Path.Combine(first.Profile.CliDataDirectory, "data.json")));
        Assert.True(File.Exists(Path.Combine(second.Profile.CliDataDirectory, "data.json")));
    }

    private static BitwardenAccountProfile Profile(string directory, string name) => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = name,
        CliDataDirectory = directory
    };

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"BitwardenCli.Core.IntegrationTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string GetPath(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose()
        {
            var tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
            var target = System.IO.Path.GetFullPath(Path);
            if (target.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }
        }
    }
}
