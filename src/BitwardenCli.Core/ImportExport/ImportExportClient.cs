using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.ImportExport;

#pragma warning disable CS1591
public enum VaultExportFormat { Csv, Json, EncryptedJson, Zip }

public sealed record VaultExportOptions
{
    public required string OutputPath { get; init; }
    public VaultExportFormat Format { get; init; } = VaultExportFormat.Csv;
    public string? OrganizationId { get; init; }
}

public sealed class ImportExportClient
{
    private readonly AccountCommandContext _context;
    internal ImportExportClient(AccountCommandContext context) => _context = context;

    public async Task<CliResult<string>> ListImportFormatsAsync(CancellationToken cancellationToken = default)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<string>();
        var process = await _context.RunAsync(new CliCommand("list-import-formats", CliArgument.Plain("import"), CliArgument.Plain("--formats")), true, false, cancellationToken);
        return process.IsSuccess ? CliResultFactory.Success(process.StandardOutput.Trim(), process) : CliResultFactory.Failure<string>(process);
    }

    public async Task<CliResult> ImportAsync(string format, string inputPath, string? organizationId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format); ValidateAbsolutePath(inputPath, nameof(inputPath));
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var args = new List<CliArgument> { CliArgument.Plain("import") };
        if (!string.IsNullOrWhiteSpace(organizationId)) { args.Add(CliArgument.Plain("--organizationid")); args.Add(CliArgument.Plain(organizationId)); }
        args.Add(CliArgument.Plain(format)); args.Add(CliArgument.Plain(inputPath));
        return CliResultFactory.FromProcess(await _context.RunAsync(new CliCommand("import-vault", [.. args]), true, true, cancellationToken));
    }

    public async Task<CliResult> ImportContentAsync(
        string format,
        string content,
        string? organizationId = null,
        string fileExtension = ".txt",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(content);
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();

        var extension = NormalizeExtension(fileExtension);
        var tempPath = Path.Combine(Path.GetTempPath(), $"BitwardenCli.Core.Import-{Guid.NewGuid():N}{extension}");
        try
        {
            await File.WriteAllTextAsync(tempPath, content, cancellationToken).ConfigureAwait(false);
            return await ImportAsync(format, tempPath, organizationId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task<CliResult> ExportAsync(VaultExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options); ValidateAbsolutePath(options.OutputPath, nameof(options.OutputPath));
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var format = options.Format switch { VaultExportFormat.Csv => "csv", VaultExportFormat.Json => "json", VaultExportFormat.EncryptedJson => "encrypted_json", VaultExportFormat.Zip => "zip", _ => throw new ArgumentOutOfRangeException(nameof(options)) };
        var args = new List<CliArgument> { CliArgument.Plain("export"), CliArgument.Plain("--output"), CliArgument.Plain(options.OutputPath), CliArgument.Plain("--format"), CliArgument.Plain(format) };
        if (!string.IsNullOrWhiteSpace(options.OrganizationId)) { args.Add(CliArgument.Plain("--organizationid")); args.Add(CliArgument.Plain(options.OrganizationId)); }
        return CliResultFactory.FromProcess(await _context.RunAsync(new CliCommand("export-vault", [.. args]), true, true, cancellationToken));
    }

    private static void ValidateAbsolutePath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Path must be absolute.", parameterName);
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".txt";
        }

        return extension.StartsWith(".", StringComparison.Ordinal) ? extension : $".{extension}";
    }
}
#pragma warning restore CS1591
