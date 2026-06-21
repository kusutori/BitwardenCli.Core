namespace BitwardenCli.Core.Accounts;

/// <summary>Contains non-sensitive metadata for one isolated CLI account.</summary>
public sealed record BitwardenAccountProfile
{
    /// <summary>Gets the stable application account identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the user-visible account name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the absolute directory containing this account's CLI data.</summary>
    public required string CliDataDirectory { get; init; }

    /// <summary>Gets the account email when known.</summary>
    public string? Email { get; init; }

    /// <summary>Gets the Bitwarden user identifier when known.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets the cloud or self-hosted server URL when configured.</summary>
    public string? ServerUrl { get; init; }

    /// <summary>Gets the account authentication method.</summary>
    public BitwardenAuthenticationKind AuthenticationKind { get; init; }

    /// <summary>Gets the last time the host application used this account.</summary>
    public DateTimeOffset? LastUsedAt { get; init; }
}
