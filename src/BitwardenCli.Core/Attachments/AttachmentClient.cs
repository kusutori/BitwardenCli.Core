using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Attachments;

#pragma warning disable CS1591
public sealed class AttachmentClient
{
    private readonly AccountCommandContext _context;
    internal AttachmentClient(AccountCommandContext context) => _context = context;

    public async Task<CliResult> UploadAsync(string itemId, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!Path.IsPathFullyQualified(filePath)) throw new ArgumentException("Attachment path must be absolute.", nameof(filePath));
        return await MutateAsync(new CliCommand("create-attachment", CliArgument.Plain("create"), CliArgument.Plain("attachment"), CliArgument.Plain("--file"), CliArgument.Plain(filePath), CliArgument.Plain("--itemid"), CliArgument.Plain(itemId)), cancellationToken);
    }

    public async Task<CliResult> DownloadAsync(string itemId, string attachmentId, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId); ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId); ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (!Path.IsPathFullyQualified(outputPath)) throw new ArgumentException("Output path must be absolute.", nameof(outputPath));
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var command = new CliCommand("get-attachment", CliArgument.Plain("get"), CliArgument.Plain("attachment"), CliArgument.Plain(attachmentId), CliArgument.Plain("--itemid"), CliArgument.Plain(itemId), CliArgument.Plain("--output"), CliArgument.Plain(outputPath));
        return CliResultFactory.FromProcess(await _context.RunAsync(command, true, false, cancellationToken));
    }

    public Task<CliResult> DeleteAsync(string itemId, string attachmentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId); ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId);
        return MutateAsync(new CliCommand("delete-attachment", CliArgument.Plain("delete"), CliArgument.Plain("attachment"), CliArgument.Plain(attachmentId), CliArgument.Plain("--itemid"), CliArgument.Plain(itemId)), cancellationToken);
    }

    private async Task<CliResult> MutateAsync(CliCommand command, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        return CliResultFactory.FromProcess(await _context.RunAsync(command, true, true, cancellationToken));
    }
}
#pragma warning restore CS1591
