using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Tests;

public sealed class CliCommandTests
{
    [Fact]
    public void Safe_arguments_redact_only_sensitive_values()
    {
        var command = new CliCommand(
            "unlock",
            CliArgument.Plain("unlock"),
            CliArgument.Secret("master-password"),
            CliArgument.Plain("--raw"));

        Assert.Equal(["unlock", "[REDACTED]", "--raw"], command.GetSafeArguments());
    }

    [Fact]
    public void Environment_diagnostics_expose_names_only()
    {
        var command = new CliCommand("login", CliArgument.Plain("login"))
        {
            Environment =
            [
                CliEnvironmentVariable.Secret("BW_CLIENTSECRET", "secret"),
                CliEnvironmentVariable.Plain("BITWARDENCLI_APPDATA_DIR", "profile")
            ]
        };

        Assert.Equal(["BW_CLIENTSECRET", "BITWARDENCLI_APPDATA_DIR"], command.GetEnvironmentNames());
    }
}
