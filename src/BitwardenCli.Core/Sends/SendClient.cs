using System.Text.Json;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Sends;

#pragma warning disable CS1591
public sealed class SendClient
{
    private readonly AccountCommandContext _context;
    internal SendClient(AccountCommandContext context) => _context = context;

    public Task<CliResult<JsonArray>> ListAsync(CancellationToken cancellationToken = default) => RunJsonArrayAsync(new CliCommand("list-sends", CliArgument.Plain("send"), CliArgument.Plain("list")), false, cancellationToken);
    public Task<CliResult<JsonObject>> GetAsync(string id, CancellationToken cancellationToken = default) { ArgumentException.ThrowIfNullOrWhiteSpace(id); return RunJsonObjectAsync(new CliCommand("get-send", CliArgument.Plain("send"), CliArgument.Plain("get"), CliArgument.Plain(id)), false, cancellationToken); }

    public Task<CliResult<JsonObject>> CreateAsync(JsonObject send, string? filePath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(send);
        var args = new List<CliArgument> { CliArgument.Plain("send"), CliArgument.Plain("create") };
        if (filePath is not null) { ValidateAbsolutePath(filePath); args.Add(CliArgument.Plain("--file")); args.Add(CliArgument.Plain(filePath)); }
        return RunJsonObjectAsync(JsonCommand.WithPayload("create-send", send, [.. args]), true, cancellationToken);
    }

    public Task<CliResult<JsonObject>> EditAsync(string id, JsonObject send, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentNullException.ThrowIfNull(send);
        return RunJsonObjectAsync(JsonCommand.WithPayload("edit-send", send, CliArgument.Plain("send"), CliArgument.Plain("edit"), CliArgument.Plain("--itemid"), CliArgument.Plain(id)), true, cancellationToken);
    }

    public Task<CliResult> DeleteAsync(string id, CancellationToken cancellationToken = default) => MutateAsync("delete-send", id, cancellationToken);
    public Task<CliResult> RemovePasswordAsync(string id, CancellationToken cancellationToken = default) => MutateAsync("remove-send-password", id, cancellationToken, "remove-password");

    public async Task<CliResult<string>> ReceiveTextAsync(string url, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var environment = new List<CliEnvironmentVariable>();
        var args = new List<CliArgument> { CliArgument.Plain("send"), CliArgument.Plain("receive"), CliArgument.Plain(url) };
        if (password is not null) { const string name = "BWCLI_SEND_PASSWORD"; args.Add(CliArgument.Plain("--passwordenv")); args.Add(CliArgument.Plain(name)); environment.Add(CliEnvironmentVariable.Secret(name, password)); }
        var process = await _context.RunAsync(new CliCommand("receive-send", [.. args]) { Environment = environment }, false, false, cancellationToken);
        return process.IsSuccess ? CliResultFactory.Success(process.StandardOutput, process) : CliResultFactory.Failure<string>(process);
    }

    private async Task<CliResult> MutateAsync(string operation, string id, CancellationToken cancellationToken, string verb = "delete")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var process = await _context.RunAsync(new CliCommand(operation, CliArgument.Plain("send"), CliArgument.Plain(verb), CliArgument.Plain(id)), true, true, cancellationToken);
        return CliResultFactory.FromProcess(process);
    }

    private async Task<CliResult<JsonArray>> RunJsonArrayAsync(CliCommand command, bool mutation, CancellationToken cancellationToken)
    {
        var result = await RunJsonAsync(command, mutation, cancellationToken); if (!result.IsSuccess || result.Value is not JsonArray array) return result.IsSuccess ? CliResult<JsonArray>.Failure(new CliError(CliErrorCode.InvalidResponse, "The CLI returned invalid Send JSON."), result.ExitCode, result.StandardError, result.Duration) : CliResult<JsonArray>.Failure(result.Error!, result.ExitCode, result.StandardError, result.Duration);
        return CliResult<JsonArray>.Success(array, result.ExitCode, result.StandardError, result.Duration);
    }

    private async Task<CliResult<JsonObject>> RunJsonObjectAsync(CliCommand command, bool mutation, CancellationToken cancellationToken)
    {
        var result = await RunJsonAsync(command, mutation, cancellationToken); if (!result.IsSuccess || result.Value is not JsonObject obj) return result.IsSuccess ? CliResult<JsonObject>.Failure(new CliError(CliErrorCode.InvalidResponse, "The CLI returned invalid Send JSON."), result.ExitCode, result.StandardError, result.Duration) : CliResult<JsonObject>.Failure(result.Error!, result.ExitCode, result.StandardError, result.Duration);
        return CliResult<JsonObject>.Success(obj, result.ExitCode, result.StandardError, result.Duration);
    }

    private async Task<CliResult<JsonNode>> RunJsonAsync(CliCommand command, bool mutation, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<JsonNode>();
        var process = await _context.RunAsync(command, true, mutation, cancellationToken); if (!process.IsSuccess) return CliResultFactory.Failure<JsonNode>(process);
        try { return JsonNode.Parse(process.StandardOutput) is { } node ? CliResultFactory.Success(node, process) : CliResultFactory.InvalidResponse<JsonNode>(process, "The CLI returned invalid Send JSON."); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<JsonNode>(process, "The CLI returned invalid Send JSON."); }
    }

    private static void ValidateAbsolutePath(string path) { if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Send file path must be absolute.", nameof(path)); }
}
#pragma warning restore CS1591
