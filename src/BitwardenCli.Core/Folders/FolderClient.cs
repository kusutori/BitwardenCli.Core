using System.Text.Json;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Serialization;

namespace BitwardenCli.Core.Folders;

#pragma warning disable CS1591
public sealed class FolderClient
{
    private readonly AccountCommandContext _context;
    internal FolderClient(AccountCommandContext context) => _context = context;

    public async Task<CliResult<IReadOnlyList<VaultFolder>>> ListAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var args = new List<CliArgument> { CliArgument.Plain("list"), CliArgument.Plain("folders") };
        if (!string.IsNullOrWhiteSpace(search)) { args.Add(CliArgument.Plain("--search")); args.Add(CliArgument.Plain(search)); }
        var process = await RunAsync(new CliCommand("list-folders", [.. args]), false, cancellationToken);
        var result = Deserialize(process, BitwardenJsonContext.Default.VaultFolderArray, "The CLI returned an invalid folder list.");
        return result.IsSuccess && result.Value is not null
            ? CliResult<IReadOnlyList<VaultFolder>>.Success(result.Value, result.ExitCode, result.StandardError, result.Duration)
            : CliResult<IReadOnlyList<VaultFolder>>.Failure(result.Error!, result.ExitCode, result.StandardError, result.Duration);
    }

    public async Task<CliResult<VaultFolder>> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var process = await RunAsync(new CliCommand("get-folder", CliArgument.Plain("get"), CliArgument.Plain("folder"), CliArgument.Plain(id)), false, cancellationToken);
        return Deserialize(process, BitwardenJsonContext.Default.VaultFolder, "The CLI returned an invalid folder.");
    }

    public Task<CliResult<VaultFolder>> CreateAsync(string name, CancellationToken cancellationToken = default) =>
        WriteAsync("create-folder", new JsonObject { ["name"] = RequireName(name) }, [CliArgument.Plain("create"), CliArgument.Plain("folder")], cancellationToken);

    public Task<CliResult<VaultFolder>> EditAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return WriteAsync("edit-folder", new JsonObject { ["name"] = RequireName(name) }, [CliArgument.Plain("edit"), CliArgument.Plain("folder"), CliArgument.Plain(id)], cancellationToken);
    }

    public async Task<CliResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var process = await _context.RunAsync(new CliCommand("delete-folder", CliArgument.Plain("delete"), CliArgument.Plain("folder"), CliArgument.Plain(id)), true, true, cancellationToken);
        return CliResultFactory.FromProcess(process);
    }

    private async Task<CliResult<VaultFolder>> WriteAsync(string operation, JsonObject payload, CliArgument[] args, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<VaultFolder>();
        var process = await _context.RunAsync(JsonCommand.WithPayload(operation, payload, args), true, true, cancellationToken);
        return Deserialize(process, BitwardenJsonContext.Default.VaultFolder, "The CLI returned an invalid folder.");
    }

    private Task<CliProcessResult> RunAsync(CliCommand command, bool mutation, CancellationToken cancellationToken) =>
        _context.Session.IsUnlocked ? _context.RunAsync(command, true, mutation, cancellationToken) : Task.FromResult(new CliProcessResult(CliProcessOutcome.Completed, -1, "", "Vault is locked.", TimeSpan.Zero));

    private static CliResult<T> Deserialize<T>(CliProcessResult process, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo, string message)
    {
        if (!process.IsSuccess) return process.ExitCode == -1 ? AccountResultFactory.MissingSession<T>() : CliResultFactory.Failure<T>(process);
        try { return JsonSerializer.Deserialize(process.StandardOutput, typeInfo) is { } value ? CliResultFactory.Success(value, process) : CliResultFactory.InvalidResponse<T>(process, message); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<T>(process, message); }
    }

    private static string RequireName(string name) { ArgumentException.ThrowIfNullOrWhiteSpace(name); return name; }
}
#pragma warning restore CS1591
