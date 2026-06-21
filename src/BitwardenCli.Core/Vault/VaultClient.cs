using System.Text.Json;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Serialization;

#pragma warning disable CS1591 // Command methods are documented in the package README.

namespace BitwardenCli.Core.Vault;

public sealed class VaultClient
{
    private readonly AccountCommandContext _context;

    internal VaultClient(AccountCommandContext context)
    {
        _context = context;
    }

    public Task<CliResult<IReadOnlyList<VaultItem>>> ListItemsAsync(
        VaultItemQuery? query = null,
        CancellationToken cancellationToken = default) =>
        RunItemsAsync(query ?? new VaultItemQuery(), cancellationToken);

    public async Task<CliResult<VaultItem>> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var process = await RunAsync(new CliCommand("get-item", CliArgument.Plain("get"), CliArgument.Plain("item"), CliArgument.Plain(id)), false, cancellationToken);
        return Deserialize(process, BitwardenJsonContext.Default.VaultItem, "The CLI returned an invalid item document.");
    }

    public async Task<CliResult<JsonObject>> GetItemDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var process = await RunAsync(new CliCommand("get-item", CliArgument.Plain("get"), CliArgument.Plain("item"), CliArgument.Plain(id)), false, cancellationToken);
        if (!process.IsSuccess) return CliResultFactory.Failure<JsonObject>(process);
        try
        {
            return JsonNode.Parse(process.StandardOutput) is JsonObject item
                ? CliResultFactory.Success(item, process)
                : CliResultFactory.InvalidResponse<JsonObject>(process, "The CLI returned an invalid item document.");
        }
        catch (JsonException)
        {
            return CliResultFactory.InvalidResponse<JsonObject>(process, "The CLI returned an invalid item document.");
        }
    }

    public Task<CliResult<string>> GetUsernameAsync(string id, CancellationToken cancellationToken = default) => GetTextAsync("username", id, cancellationToken);
    public Task<CliResult<string>> GetPasswordAsync(string id, CancellationToken cancellationToken = default) => GetTextAsync("password", id, cancellationToken);
    public Task<CliResult<string>> GetUriAsync(string id, CancellationToken cancellationToken = default) => GetTextAsync("uri", id, cancellationToken);
    public Task<CliResult<string>> GetTotpAsync(string id, CancellationToken cancellationToken = default) => GetTextAsync("totp", id, cancellationToken);
    public Task<CliResult<string>> GetNotesAsync(string id, CancellationToken cancellationToken = default) => GetTextAsync("notes", id, cancellationToken);
    public Task<CliResult<string>> GetExposedAsync(string id, CancellationToken cancellationToken = default) => GetTextAsync("exposed", id, cancellationToken);

    public async Task<CliResult<VaultItem>> CreateItemAsync(JsonObject item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var process = await RunAsync(JsonCommand.WithPayload("create-item", item, CliArgument.Plain("create"), CliArgument.Plain("item")), true, cancellationToken);
        return Deserialize(process, BitwardenJsonContext.Default.VaultItem, "The CLI returned an invalid created item.");
    }

    public async Task<CliResult<VaultItem>> EditItemAsync(string id, JsonObject changedProperties, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(changedProperties);
        var current = await GetItemDocumentAsync(id, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null) return ForwardFailure<VaultItem>(current);
        foreach (var property in changedProperties) current.Value[property.Key] = property.Value?.DeepClone();
        var process = await RunAsync(JsonCommand.WithPayload("edit-item", current.Value, CliArgument.Plain("edit"), CliArgument.Plain("item"), CliArgument.Plain(id)), true, cancellationToken);
        return Deserialize(process, BitwardenJsonContext.Default.VaultItem, "The CLI returned an invalid edited item.");
    }

    public async Task<CliResult<VaultItem>> CloneItemAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var current = await GetItemDocumentAsync(id, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null) return ForwardFailure<VaultItem>(current);
        foreach (var property in new[] { "id", "creationDate", "revisionDate", "deletedDate", "archivedDate" }) current.Value.Remove(property);
        current.Value["name"] = name;
        return await CreateItemAsync(current.Value, cancellationToken).ConfigureAwait(false);
    }

    public Task<CliResult> DeleteItemAsync(string id, bool permanent = false, CancellationToken cancellationToken = default) =>
        RunMutationAsync("delete-item", [CliArgument.Plain("delete"), CliArgument.Plain("item"), CliArgument.Plain(id), .. permanent ? new[] { CliArgument.Plain("--permanent") } : []], cancellationToken);

    public Task<CliResult> RestoreItemAsync(string id, CancellationToken cancellationToken = default) =>
        RunMutationAsync("restore-item", [CliArgument.Plain("restore"), CliArgument.Plain("item"), CliArgument.Plain(id)], cancellationToken);

    public Task<CliResult> ArchiveItemAsync(string id, CancellationToken cancellationToken = default) =>
        RunMutationAsync("archive-item", [CliArgument.Plain("archive"), CliArgument.Plain("item"), CliArgument.Plain(id)], cancellationToken);

    public async Task<CliResult> SetCollectionsAsync(string id, IReadOnlyList<string> collectionIds, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var payload = new JsonArray(collectionIds.Select(id => JsonValue.Create(id)).ToArray());
        var process = await RunAsync(JsonCommand.WithPayload("edit-item-collections", payload, CliArgument.Plain("edit"), CliArgument.Plain("item-collections"), CliArgument.Plain(id)), true, cancellationToken);
        return CliResultFactory.FromProcess(process);
    }

    public async Task<CliResult> MoveToOrganizationAsync(string id, string organizationId, IReadOnlyList<string> collectionIds, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        var payload = new JsonArray(collectionIds.Select(id => JsonValue.Create(id)).ToArray());
        var process = await RunAsync(JsonCommand.WithPayload("move-item", payload, CliArgument.Plain("move"), CliArgument.Plain(id), CliArgument.Plain(organizationId)), true, cancellationToken);
        return CliResultFactory.FromProcess(process);
    }

    private async Task<CliResult<IReadOnlyList<VaultItem>>> RunItemsAsync(VaultItemQuery query, CancellationToken cancellationToken)
    {
        var args = new List<CliArgument> { CliArgument.Plain("list"), CliArgument.Plain("items") };
        AddOption(args, "--search", query.Search); AddOption(args, "--url", query.Url); AddOption(args, "--folderid", query.FolderId);
        AddOption(args, "--collectionid", query.CollectionId); AddOption(args, "--organizationid", query.OrganizationId);
        if (query.Trash) args.Add(CliArgument.Plain("--trash"));
        if (query.Archived) args.Add(CliArgument.Plain("--archived"));
        var process = await RunAsync(new CliCommand("list-items", [.. args]), false, cancellationToken);
        var parsed = Deserialize(process, BitwardenJsonContext.Default.VaultItemArray, "The CLI returned an invalid item list.");
        return parsed.IsSuccess && parsed.Value is not null
            ? CliResult<IReadOnlyList<VaultItem>>.Success(parsed.Value, parsed.ExitCode, parsed.StandardError, parsed.Duration)
            : CliResult<IReadOnlyList<VaultItem>>.Failure(parsed.Error!, parsed.ExitCode, parsed.StandardError, parsed.Duration);
    }

    private async Task<CliResult<string>> GetTextAsync(string kind, string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var process = await RunAsync(new CliCommand($"get-{kind}", CliArgument.Plain("get"), CliArgument.Plain(kind), CliArgument.Plain(id)), false, cancellationToken);
        return process.IsSuccess ? CliResultFactory.Success(process.StandardOutput.Trim(), process) : CliResultFactory.Failure<string>(process);
    }

    private async Task<CliResult> RunMutationAsync(string operation, CliArgument[] arguments, CancellationToken cancellationToken)
    {
        var process = await RunAsync(new CliCommand(operation, arguments), true, cancellationToken);
        return CliResultFactory.FromProcess(process);
    }

    private Task<CliProcessResult> RunAsync(CliCommand command, bool mutation, CancellationToken cancellationToken) =>
        _context.Session.IsUnlocked
            ? _context.RunAsync(command, includeSession: true, serializeMutation: mutation, cancellationToken)
            : Task.FromResult(MissingSessionProcess());

    private static CliProcessResult MissingSessionProcess() => new(CliProcessOutcome.Completed, -1, string.Empty, "Vault is locked.", TimeSpan.Zero);

    private static CliResult<T> Deserialize<T>(CliProcessResult process, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo, string message)
    {
        if (!process.IsSuccess) return process.ExitCode == -1 ? AccountResultFactory.MissingSession<T>() : CliResultFactory.Failure<T>(process);
        try { return JsonSerializer.Deserialize(process.StandardOutput, typeInfo) is { } value ? CliResultFactory.Success(value, process) : CliResultFactory.InvalidResponse<T>(process, message); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<T>(process, message); }
    }

    private static CliResult<TTarget> ForwardFailure<TTarget>(CliResult<JsonObject> source) =>
        CliResult<TTarget>.Failure(source.Error!, source.ExitCode, source.StandardError, source.Duration);

    private static void AddOption(List<CliArgument> args, string option, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) { args.Add(CliArgument.Plain(option)); args.Add(CliArgument.Plain(value)); }
    }
}

#pragma warning restore CS1591
