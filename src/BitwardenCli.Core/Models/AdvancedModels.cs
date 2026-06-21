using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitwardenCli.Core.Models;

#pragma warning disable CS1591
public sealed class BitwardenSend
{
    public string Object { get; init; } = "send";
    public string Id { get; init; } = string.Empty;
    public string? AccessId { get; init; }
    public int Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool HideEmail { get; init; }
    public DateTimeOffset? RevisionDate { get; init; }
    public DateTimeOffset? DeletionDate { get; init; }
    public DateTimeOffset? ExpirationDate { get; init; }
    public int? MaxAccessCount { get; init; }
    public int AccessCount { get; init; }
    public string? Password { get; init; }
    public bool Disabled { get; init; }
    public BitwardenSendText? Text { get; init; }
    public BitwardenSendFile? File { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed record BitwardenSendText { public string? Text { get; init; } public bool Hidden { get; init; } }
public sealed record BitwardenSendFile { public string? Id { get; init; } public string? FileName { get; init; } public long? Size { get; init; } public string? SizeName { get; init; } }

public sealed class OrganizationMember
{
    public string Object { get; init; } = "org-member";
    public string Id { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public int? Status { get; init; }
    public int? Type { get; init; }
    public bool? TwoFactorEnabled { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class DeviceApprovalRequest
{
    public string Id { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public string? DeviceIdentifier { get; init; }
    public string? DeviceType { get; init; }
    public DateTimeOffset? RequestTime { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
#pragma warning restore CS1591
