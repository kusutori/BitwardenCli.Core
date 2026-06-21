using System.Text.Json;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Serialization;

namespace BitwardenCli.Core.Sends;

#pragma warning disable CS1591
public sealed class SendClient
{
    private readonly AccountCommandContext _context;
    internal SendClient(AccountCommandContext context) => _context = context;

    public Task<CliResult<IReadOnlyList<BitwardenSend>>> ListAsync(CancellationToken cancellationToken = default) => RunSendListAsync(new CliCommand("list-sends", CliArgument.Plain("send"), CliArgument.Plain("list")), cancellationToken);
    public Task<CliResult<BitwardenSend>> GetAsync(string id, CancellationToken cancellationToken = default) { ArgumentException.ThrowIfNullOrWhiteSpace(id); return RunSendAsync(new CliCommand("get-send", CliArgument.Plain("send"), CliArgument.Plain("get"), CliArgument.Plain(id)), false, cancellationToken); }

    public async Task<CliResult> DownloadFileAsync(string id, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ValidateAbsolutePath(outputPath);
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var command = new CliCommand("download-send", CliArgument.Plain("send"), CliArgument.Plain("get"), CliArgument.Plain(id), CliArgument.Plain("--output"), CliArgument.Plain(outputPath));
        return CliResultFactory.FromProcess(await _context.RunAsync(command, true, false, cancellationToken));
    }

    public async Task<CliResult> ReceiveFileAsync(string url, string outputPath, string? password = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url); ValidateAbsolutePath(outputPath);
        var environment = new List<CliEnvironmentVariable>();
        var args = new List<CliArgument> { CliArgument.Plain("send"), CliArgument.Plain("receive"), CliArgument.Plain(url), CliArgument.Plain("--output"), CliArgument.Plain(outputPath) };
        if (password is not null) { const string name = "BWCLI_SEND_PASSWORD"; args.Add(CliArgument.Plain("--passwordenv")); args.Add(CliArgument.Plain(name)); environment.Add(CliEnvironmentVariable.Secret(name, password)); }
        var process = await _context.RunAsync(new CliCommand("receive-send-file", [.. args]) { Environment = environment }, false, false, cancellationToken);
        return CliResultFactory.FromProcess(process);
    }

    public Task<CliResult<BitwardenSend>> CreateAsync(JsonObject send, string? filePath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(send);
        var args = new List<CliArgument> { CliArgument.Plain("send"), CliArgument.Plain("create") };
        if (filePath is not null) { ValidateAbsolutePath(filePath); args.Add(CliArgument.Plain("--file")); args.Add(CliArgument.Plain(filePath)); }
        return RunSendAsync(JsonCommand.WithPayload("create-send", send, [.. args]), true, cancellationToken);
    }

    public Task<CliResult<BitwardenSend>> EditAsync(string id, JsonObject send, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentNullException.ThrowIfNull(send);
        return RunSendAsync(JsonCommand.WithPayload("edit-send", send, CliArgument.Plain("send"), CliArgument.Plain("edit"), CliArgument.Plain("--itemid"), CliArgument.Plain(id)), true, cancellationToken);
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

    private async Task<CliResult<IReadOnlyList<BitwardenSend>>> RunSendListAsync(CliCommand command, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<IReadOnlyList<BitwardenSend>>();
        var process = await _context.RunAsync(command, true, false, cancellationToken); if (!process.IsSuccess) return CliResultFactory.Failure<IReadOnlyList<BitwardenSend>>(process);
        try { var values = JsonSerializer.Deserialize(process.StandardOutput, BitwardenJsonContext.Default.BitwardenSendArray); return values is null ? CliResultFactory.InvalidResponse<IReadOnlyList<BitwardenSend>>(process, "The CLI returned invalid Send JSON.") : CliResultFactory.Success<IReadOnlyList<BitwardenSend>>(values, process); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<IReadOnlyList<BitwardenSend>>(process, "The CLI returned invalid Send JSON."); }
    }

    private async Task<CliResult<BitwardenSend>> RunSendAsync(CliCommand command, bool mutation, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<BitwardenSend>();
        var process = await _context.RunAsync(command, true, mutation, cancellationToken); if (!process.IsSuccess) return CliResultFactory.Failure<BitwardenSend>(process);
        try { return JsonSerializer.Deserialize(process.StandardOutput, BitwardenJsonContext.Default.BitwardenSend) is { } value ? CliResultFactory.Success(value, process) : CliResultFactory.InvalidResponse<BitwardenSend>(process, "The CLI returned invalid Send JSON."); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<BitwardenSend>(process, "The CLI returned invalid Send JSON."); }
    }

    private static void ValidateAbsolutePath(string path) { if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Send file path must be absolute.", nameof(path)); }
}
#pragma warning restore CS1591
