using System.Text.Json;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Administration;

#pragma warning disable CS1591
public sealed class AdministrationClient
{
    private readonly AccountCommandContext _context;
    internal AdministrationClient(AccountCommandContext context) => _context = context;

    public Task<CliResult<JsonArray>> ListDeviceApprovalsAsync(string organizationId, CancellationToken cancellationToken = default) => RunJsonArrayAsync("list-device-approvals", [CliArgument.Plain("device-approval"), CliArgument.Plain("list"), .. OrganizationOption(organizationId)], false, cancellationToken);
    public Task<CliResult> ApproveDeviceAsync(string organizationId, string requestId, CancellationToken cancellationToken = default) => RunMutationAsync("approve-device", "approve", organizationId, requestId, cancellationToken);
    public Task<CliResult> DenyDeviceAsync(string organizationId, string requestId, CancellationToken cancellationToken = default) => RunMutationAsync("deny-device", "deny", organizationId, requestId, cancellationToken);
    public Task<CliResult> ApproveAllDevicesAsync(string organizationId, CancellationToken cancellationToken = default) => RunMutationAsync("approve-all-devices", "approve-all", organizationId, null, cancellationToken);
    public Task<CliResult> DenyAllDevicesAsync(string organizationId, CancellationToken cancellationToken = default) => RunMutationAsync("deny-all-devices", "deny-all", organizationId, null, cancellationToken);

    public async Task<CliResult> ConfirmOrganizationMemberAsync(string organizationId, string memberId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberId); if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        CliArgument[] args = [CliArgument.Plain("confirm"), CliArgument.Plain("org-member"), CliArgument.Plain(memberId), .. OrganizationOption(organizationId)];
        return CliResultFactory.FromProcess(await _context.RunAsync(new CliCommand("confirm-org-member", args), true, true, cancellationToken));
    }

    private async Task<CliResult> RunMutationAsync(string operation, string verb, string organizationId, string? requestId, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession();
        var args = new List<CliArgument> { CliArgument.Plain("device-approval"), CliArgument.Plain(verb) }; if (requestId is not null) args.Add(CliArgument.Plain(requestId)); args.AddRange(OrganizationOption(organizationId));
        return CliResultFactory.FromProcess(await _context.RunAsync(new CliCommand(operation, [.. args]), true, true, cancellationToken));
    }

    private async Task<CliResult<JsonArray>> RunJsonArrayAsync(string operation, CliArgument[] args, bool mutation, CancellationToken cancellationToken)
    {
        if (!_context.Session.IsUnlocked) return AccountResultFactory.MissingSession<JsonArray>();
        var process = await _context.RunAsync(new CliCommand(operation, args), true, mutation, cancellationToken); if (!process.IsSuccess) return CliResultFactory.Failure<JsonArray>(process);
        try { return JsonNode.Parse(process.StandardOutput) is JsonArray value ? CliResultFactory.Success(value, process) : CliResultFactory.InvalidResponse<JsonArray>(process, "The CLI returned invalid device approval JSON."); }
        catch (JsonException) { return CliResultFactory.InvalidResponse<JsonArray>(process, "The CLI returned invalid device approval JSON."); }
    }

    private static CliArgument[] OrganizationOption(string organizationId) { ArgumentException.ThrowIfNullOrWhiteSpace(organizationId); return [CliArgument.Plain("--organizationid"), CliArgument.Plain(organizationId)]; }
}
#pragma warning restore CS1591
