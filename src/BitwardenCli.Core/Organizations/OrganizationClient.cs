using System.Text.Json;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Serialization;
using System.Text.Json.Nodes;

namespace BitwardenCli.Core.Organizations;

#pragma warning disable CS1591
public sealed class OrganizationClient
{
    private readonly AccountCommandContext _context;
    internal OrganizationClient(AccountCommandContext context) => _context = context;

    public Task<CliResult<IReadOnlyList<BitwardenOrganization>>> ListAsync(CancellationToken cancellationToken = default) =>
        ListAsync("organizations", BitwardenJsonContext.Default.BitwardenOrganizationArray, cancellationToken);

    public Task<CliResult<IReadOnlyList<BitwardenCollection>>> ListCollectionsAsync(string? organizationId = null, CancellationToken cancellationToken = default) =>
        ListAsync("collections", BitwardenJsonContext.Default.BitwardenCollectionArray, cancellationToken, organizationId);

    public Task<CliResult<IReadOnlyList<BitwardenCollection>>> ListOrganizationCollectionsAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        return ListAsync("org-collections", BitwardenJsonContext.Default.BitwardenCollectionArray, cancellationToken, organizationId);
    }

    public Task<CliResult<IReadOnlyList<OrganizationMember>>> ListMembersAsync(string organizationId, CancellationToken cancellationToken = default) =>
        ListMembersCoreAsync(organizationId, cancellationToken);

    public Task<CliResult<JsonObject>> CreateOrganizationCollectionAsync(string organizationId, JsonObject collection, CancellationToken cancellationToken = default) =>
        WriteOrganizationCollectionAsync("create-org-collection", "create", organizationId, null, collection, cancellationToken);

    public Task<CliResult<JsonObject>> EditOrganizationCollectionAsync(string organizationId, string collectionId, JsonObject collection, CancellationToken cancellationToken = default) =>
        WriteOrganizationCollectionAsync("edit-org-collection", "edit", organizationId, collectionId, collection, cancellationToken);

    public async Task<CliResult> DeleteOrganizationCollectionAsync(string organizationId, string collectionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId); ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var command = new CliCommand("delete-org-collection", CliArgument.Plain("delete"), CliArgument.Plain("org-collection"), CliArgument.Plain(collectionId), CliArgument.Plain("--organizationid"), CliArgument.Plain(organizationId));
        return CliResultFactory.FromProcess(await _context.RunAsync(command, true, true, cancellationToken));
    }

    private async Task<CliResult<JsonObject>> WriteOrganizationCollectionAsync(string operation, string verb, string organizationId, string? collectionId, JsonObject collection, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId); ArgumentNullException.ThrowIfNull(collection);
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<JsonObject>();
        var args = new List<CliArgument> { CliArgument.Plain(verb), CliArgument.Plain("org-collection") };
        if (collectionId is not null) { ArgumentException.ThrowIfNullOrWhiteSpace(collectionId); args.Add(CliArgument.Plain(collectionId)); }
        args.Add(CliArgument.Plain("--organizationid")); args.Add(CliArgument.Plain(organizationId));
        var process = await _context.RunAsync(JsonCommand.WithPayload(operation, collection, [.. args]), true, true, cancellationToken);
        if (!process.IsSuccess) return CliResultFactory.Failure<JsonObject>(process);
        try { return JsonNode.Parse(process.StandardOutput) is JsonObject value ? CliResultFactory.Success(value, process) : CliResultFactory.InvalidResponse<JsonObject>(process, "The CLI returned invalid collection JSON."); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<JsonObject>(process, "The CLI returned invalid collection JSON."); }
    }

    private async Task<CliResult<IReadOnlyList<OrganizationMember>>> ListMembersCoreAsync(string organizationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<IReadOnlyList<OrganizationMember>>();
        var command = new CliCommand("list-org-members", CliArgument.Plain("list"), CliArgument.Plain("org-members"), CliArgument.Plain("--organizationid"), CliArgument.Plain(organizationId));
        var process = await _context.RunAsync(command, true, false, cancellationToken);
        if (!process.IsSuccess) return CliResultFactory.Failure<IReadOnlyList<OrganizationMember>>(process);
        try
        {
            var values = JsonSerializer.Deserialize(process.StandardOutput, BitwardenJsonContext.Default.OrganizationMemberArray);
            return values is null ? CliResultFactory.InvalidResponse<IReadOnlyList<OrganizationMember>>(process, "The CLI returned invalid organization member JSON.") : CliResultFactory.Success<IReadOnlyList<OrganizationMember>>(values, process);
        }
        catch (JsonException) { return CliResultFactory.InvalidResponse<IReadOnlyList<OrganizationMember>>(process, "The CLI returned invalid organization member JSON."); }
    }

    private async Task<CliResult<IReadOnlyList<T>>> ListAsync<T>(string kind, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T[]> typeInfo, CancellationToken cancellationToken, string? organizationId = null)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<IReadOnlyList<T>>();
        var args = new List<CliArgument> { CliArgument.Plain("list"), CliArgument.Plain(kind) };
        if (!string.IsNullOrWhiteSpace(organizationId)) { args.Add(CliArgument.Plain("--organizationid")); args.Add(CliArgument.Plain(organizationId)); }
        var process = await _context.RunAsync(new CliCommand($"list-{kind}", [.. args]), true, false, cancellationToken);
        if (!process.IsSuccess) return CliResultFactory.Failure<IReadOnlyList<T>>(process);
        try
        {
            var values = JsonSerializer.Deserialize(process.StandardOutput, typeInfo);
            return values is null ? CliResultFactory.InvalidResponse<IReadOnlyList<T>>(process, $"The CLI returned an invalid {kind} list.") : CliResultFactory.Success<IReadOnlyList<T>>(values, process);
        }
        catch (JsonException) { return CliResultFactory.InvalidResponse<IReadOnlyList<T>>(process, $"The CLI returned an invalid {kind} list."); }
    }
}
#pragma warning restore CS1591
