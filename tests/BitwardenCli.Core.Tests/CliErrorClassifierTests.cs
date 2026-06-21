using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Tests;

public sealed class CliErrorClassifierTests
{
    [Theory]
    [InlineData("You are not logged in.", CliErrorCode.Unauthenticated)]
    [InlineData("Vault is locked.", CliErrorCode.VaultLocked)]
    [InlineData("Invalid master password.", CliErrorCode.InvalidMasterPassword)]
    [InlineData("Two-step login required.", CliErrorCode.TwoFactorRequired)]
    [InlineData("Network request failed.", CliErrorCode.NetworkUnavailable)]
    [InlineData("Unknown option '--bad'.", CliErrorCode.InvalidArguments)]
    public void Classifies_known_stderr(string standardError, CliErrorCode expected)
    {
        var process = new CliProcessResult(
            CliProcessOutcome.Completed,
            1,
            string.Empty,
            standardError,
            TimeSpan.Zero);

        Assert.Equal(expected, CliErrorClassifier.Classify(process).Code);
    }

    [Fact]
    public void Does_not_treat_every_password_message_as_invalid_master_password()
    {
        var process = new CliProcessResult(
            CliProcessOutcome.Completed,
            1,
            string.Empty,
            "Password export is not permitted.",
            TimeSpan.Zero);

        Assert.Equal(CliErrorCode.Unknown, CliErrorClassifier.Classify(process).Code);
    }
}
