namespace BitwardenCli.Core.Authentication;

/// <summary>Describes a Bitwarden account login method without storing its secrets.</summary>
public abstract record LoginRequest;

/// <summary>Requests an email and master-password login.</summary>
public sealed record PasswordLoginRequest(
    string Email,
    int? TwoFactorMethod = null,
    bool IncludeTwoFactorCode = false) : LoginRequest;

/// <summary>Requests a personal API key login.</summary>
public sealed record ApiKeyLoginRequest(string ClientId) : LoginRequest;

/// <summary>Requests a browser-based single sign-on login.</summary>
public sealed record SsoLoginRequest(string? OrganizationIdentifier = null) : LoginRequest;
