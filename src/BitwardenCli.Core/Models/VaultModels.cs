using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CS1591 // Public wire DTO members mirror the Bitwarden CLI JSON schema.

namespace BitwardenCli.Core.Models;

public enum VaultItemType
{
    Login = 1,
    SecureNote = 2,
    Card = 3,
    Identity = 4
}

public enum VaultFieldType
{
    Text = 0,
    Hidden = 1,
    Boolean = 2,
    Linked = 3
}

public sealed class VaultItem
{
    public string Object { get; init; } = "item";
    public string Id { get; init; } = string.Empty;
    public string? OrganizationId { get; init; }
    public string? FolderId { get; init; }
    public VaultItemType Type { get; init; }
    public int Reprompt { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool Favorite { get; init; }
    public IReadOnlyList<string> CollectionIds { get; init; } = [];
    public DateTimeOffset? RevisionDate { get; init; }
    public DateTimeOffset? CreationDate { get; init; }
    public DateTimeOffset? DeletedDate { get; init; }
    public DateTimeOffset? ArchivedDate { get; init; }
    public VaultLogin? Login { get; init; }
    public VaultSecureNote? SecureNote { get; init; }
    public VaultCard? Card { get; init; }
    public VaultIdentity? Identity { get; init; }
    public IReadOnlyList<VaultField> Fields { get; init; } = [];
    public IReadOnlyList<VaultAttachment> Attachments { get; init; } = [];
    public IReadOnlyList<VaultPasswordHistoryEntry> PasswordHistory { get; init; } = [];

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed record VaultLogin
{
    public IReadOnlyList<VaultUri> Uris { get; init; } = [];
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? Totp { get; init; }
    public DateTimeOffset? PasswordRevisionDate { get; init; }
    public IReadOnlyList<string> Fido2Credentials { get; init; } = [];
}

public sealed record VaultUri
{
    public int? Match { get; init; }
    public string? Uri { get; init; }
}

public sealed record VaultSecureNote
{
    public int Type { get; init; }
}

public sealed record VaultCard
{
    public string? CardholderName { get; init; }
    public string? Brand { get; init; }
    public string? Number { get; init; }
    public string? ExpMonth { get; init; }
    public string? ExpYear { get; init; }
    public string? Code { get; init; }
}

public sealed record VaultIdentity
{
    public string? Title { get; init; }
    public string? FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public string? Address1 { get; init; }
    public string? Address2 { get; init; }
    public string? Address3 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Company { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Ssn { get; init; }
    public string? Username { get; init; }
    public string? PassportNumber { get; init; }
    public string? LicenseNumber { get; init; }
}

public sealed record VaultField
{
    public string? Name { get; init; }
    public string? Value { get; init; }
    public VaultFieldType Type { get; init; }
    public int? LinkedId { get; init; }
}

public sealed record VaultAttachment
{
    public string Id { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long? Size { get; init; }
    public string? SizeName { get; init; }
    public string? Url { get; init; }
}

public sealed record VaultPasswordHistoryEntry
{
    public DateTimeOffset? LastUsedDate { get; init; }
    public string Password { get; init; } = string.Empty;
}

public sealed record VaultFolder
{
    public string Object { get; init; } = "folder";
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record VaultItemQuery
{
    public string? Search { get; init; }
    public string? Url { get; init; }
    public string? FolderId { get; init; }
    public string? CollectionId { get; init; }
    public string? OrganizationId { get; init; }
    public bool Trash { get; init; }
    public bool Archived { get; init; }
}

#pragma warning restore CS1591
