using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Results;

internal static class CliErrorClassifier
{
    public static CliError Classify(CliProcessResult result)
    {
        if (result.Outcome == CliProcessOutcome.Cancelled)
        {
            return new CliError(CliErrorCode.Cancelled, "The Bitwarden CLI command was cancelled.");
        }

        if (result.Outcome == CliProcessOutcome.TimedOut)
        {
            return new CliError(CliErrorCode.Timeout, "The Bitwarden CLI command timed out.", true);
        }

        if (result.Outcome == CliProcessOutcome.StartFailed)
        {
            return new CliError(
                CliErrorCode.ExecutableNotFound,
                result.FailureMessage ?? "The Bitwarden CLI executable could not be started.");
        }

        var message = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;
        var normalized = message.ToLowerInvariant();

        if (ContainsAny(normalized, "not logged in", "unauthenticated", "please log in"))
        {
            return new CliError(CliErrorCode.Unauthenticated, message);
        }

        if (ContainsAny(normalized, "vault is locked", "vault locked", "invalid session", "session key"))
        {
            return new CliError(CliErrorCode.VaultLocked, message);
        }

        if (ContainsAny(normalized, "invalid master password", "incorrect master password"))
        {
            return new CliError(CliErrorCode.InvalidMasterPassword, message);
        }

        if (ContainsAny(normalized, "two-step login", "two-factor", "2fa", "verification code"))
        {
            return new CliError(CliErrorCode.TwoFactorRequired, message);
        }

        if (ContainsAny(
            normalized,
            "network",
            "socket",
            "econnreset",
            "econnrefused",
            "timed out",
            "timeout",
            "unable to get local issuer certificate",
            "fetch failed"))
        {
            return new CliError(CliErrorCode.NetworkUnavailable, message, true);
        }

        if (ContainsAny(normalized, "permission denied", "access is denied", "forbidden"))
        {
            return new CliError(CliErrorCode.PermissionDenied, message);
        }

        if (ContainsAny(normalized, "conflict", "revision date"))
        {
            return new CliError(CliErrorCode.Conflict, message, true);
        }

        if (ContainsAny(normalized, "unknown option", "unknown command", "required argument", "invalid argument"))
        {
            return new CliError(CliErrorCode.InvalidArguments, message);
        }

        return new CliError(
            CliErrorCode.Unknown,
            string.IsNullOrWhiteSpace(message) ? $"Bitwarden CLI exited with code {result.ExitCode}." : message);
    }

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(value.Contains);
}
