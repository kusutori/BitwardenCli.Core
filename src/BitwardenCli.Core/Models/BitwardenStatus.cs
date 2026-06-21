using System.Text.Json.Serialization;

namespace BitwardenCli.Core.Models;

/// <summary>Represents the output of <c>bw status</c>.</summary>
public sealed record BitwardenStatus
{
    /// <summary>Gets the configured server URL.</summary>
    [JsonPropertyName("serverUrl")]
    public string? ServerUrl { get; init; }

    /// <summary>Gets the last successful synchronization timestamp.</summary>
    [JsonPropertyName("lastSync")]
    public string? LastSync { get; init; }

    /// <summary>Gets the authenticated user email.</summary>
    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; init; }

    /// <summary>Gets the authenticated user identifier.</summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }

    /// <summary>Gets <c>unauthenticated</c>, <c>locked</c> or <c>unlocked</c>.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = "unauthenticated";

    /// <summary>Gets whether the profile is authenticated.</summary>
    [JsonIgnore]
    public bool IsLoggedIn => !string.Equals(Status, "unauthenticated", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether the vault is unlocked.</summary>
    [JsonIgnore]
    public bool IsUnlocked => string.Equals(Status, "unlocked", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether the vault is locked.</summary>
    [JsonIgnore]
    public bool IsLocked => string.Equals(Status, "locked", StringComparison.OrdinalIgnoreCase);
}
