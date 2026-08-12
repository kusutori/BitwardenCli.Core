namespace BitwardenCli.Core.Results;

/// <summary>Identifies a stable category of Bitwarden CLI failure.</summary>
public enum CliErrorCode
{
    /// <summary>The configured CLI executable could not be started.</summary>
    ExecutableNotFound,

    /// <summary>The installed CLI version is older than the required version.</summary>
    UnsupportedVersion,

    /// <summary>The command or its arguments are invalid.</summary>
    InvalidArguments,

    /// <summary>The account is not authenticated.</summary>
    Unauthenticated,

    /// <summary>The account vault is locked or its session is invalid.</summary>
    VaultLocked,

    /// <summary>The supplied master password is invalid.</summary>
    InvalidMasterPassword,

    /// <summary>The login requires a two-factor authentication response.</summary>
    TwoFactorRequired,

    /// <summary>A network endpoint could not be reached.</summary>
    NetworkUnavailable,

    /// <summary>The command exceeded its timeout.</summary>
    Timeout,

    /// <summary>The caller cancelled the command.</summary>
    Cancelled,

    /// <summary>The user cancelled or dismissed an interactive authentication prompt.</summary>
    UserInteractionCancelled,

    /// <summary>The CLI returned data that could not be parsed.</summary>
    InvalidResponse,

    /// <summary>The server rejected a conflicting update.</summary>
    Conflict,

    /// <summary>The operation is not permitted.</summary>
    PermissionDenied,

    /// <summary>The failure could not be categorized.</summary>
    Unknown
}
