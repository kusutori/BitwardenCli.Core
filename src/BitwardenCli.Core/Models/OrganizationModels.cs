#pragma warning disable CS1591

namespace BitwardenCli.Core.Models;

public sealed record BitwardenOrganization
{
    public string Object { get; init; } = "organization";
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Status { get; init; }
    public string? Type { get; init; }
    public bool Enabled { get; init; }
}

public sealed record BitwardenCollection
{
    public string Object { get; init; } = "collection";
    public string Id { get; init; } = string.Empty;
    public string? OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ExternalId { get; init; }
    public bool? ReadOnly { get; init; }
    public bool? HidePasswords { get; init; }
}

#pragma warning restore CS1591
