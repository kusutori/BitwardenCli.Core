using System.Text.Json;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Serialization;

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
