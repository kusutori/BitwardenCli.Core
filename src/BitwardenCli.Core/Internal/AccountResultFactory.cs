using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Internal;

internal static class AccountResultFactory
{
    public static CliResult MissingSession() => CliResult.Failure(
        new CliError(CliErrorCode.VaultLocked, "The vault must be unlocked before this command can run."),
        -1,
        string.Empty,
        TimeSpan.Zero);

    public static CliResult<T> MissingSession<T>() => CliResult<T>.Failure(
        new CliError(CliErrorCode.VaultLocked, "The vault must be unlocked before this command can run."),
        -1,
        string.Empty,
        TimeSpan.Zero);

    public static CliResult<T> MissingSecret<T>(SecretPurpose purpose) => CliResult<T>.Failure(
        new CliError(CliErrorCode.InvalidArguments, $"A secret for {purpose} was not provided."),
        -1,
        string.Empty,
        TimeSpan.Zero);
}
