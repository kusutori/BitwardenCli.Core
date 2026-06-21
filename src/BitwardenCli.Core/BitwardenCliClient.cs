using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Synchronization;
using BitwardenCli.Core.Vault;
using BitwardenCli.Core.Attachments;
using BitwardenCli.Core.Folders;
using BitwardenCli.Core.Generator;
using BitwardenCli.Core.Organizations;

namespace BitwardenCli.Core;

/// <summary>Provides account-scoped access to Bitwarden CLI command groups.</summary>
public sealed class BitwardenCliClient
{
    private readonly AccountCommandContext _context;

    internal BitwardenCliClient(AccountCommandContext context)
    {
        _context = context;
        Authentication = new AuthenticationClient(context);
        Synchronization = new SynchronizationClient(context);
        Vault = new VaultClient(context);
        Folders = new FolderClient(context);
        Attachments = new AttachmentClient(context);
        Organizations = new OrganizationClient(context);
        Generator = new GeneratorClient(context);
    }

    /// <summary>Gets the isolated account profile.</summary>
    public BitwardenAccountProfile Profile => _context.Profile;

    /// <summary>Gets the account's memory-only session state.</summary>
    public BitwardenSessionState Session => _context.Session;

    /// <summary>Gets authentication and account configuration commands.</summary>
    public AuthenticationClient Authentication { get; }

    /// <summary>Gets synchronization commands.</summary>
    public SynchronizationClient Synchronization { get; }

    /// <summary>Gets vault item commands.</summary>
    public VaultClient Vault { get; }

    /// <summary>Gets folder commands.</summary>
    public FolderClient Folders { get; }
    /// <summary>Gets attachment commands.</summary>
    public AttachmentClient Attachments { get; }
    /// <summary>Gets organization and collection commands.</summary>
    public OrganizationClient Organizations { get; }
    /// <summary>Gets password and passphrase generation commands.</summary>
    public GeneratorClient Generator { get; }

    /// <summary>Gets the current account status.</summary>
    public Task<CliResult<BitwardenStatus>> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Authentication.GetStatusAsync(cancellationToken);

    /// <summary>Logs in to the account.</summary>
    public Task<CliResult<LoginResult>> LoginAsync(
        LoginRequest request,
        ISecretProvider? secretProvider = null,
        CancellationToken cancellationToken = default) =>
        Authentication.LoginAsync(request, secretProvider, cancellationToken);

    /// <summary>Unlocks the account vault.</summary>
    public Task<CliResult<UnlockResult>> UnlockAsync(
        ISecretProvider secretProvider,
        CancellationToken cancellationToken = default) =>
        Authentication.UnlockAsync(secretProvider, cancellationToken);

    /// <summary>Locks the account vault.</summary>
    public Task<CliResult> LockAsync(CancellationToken cancellationToken = default) =>
        Authentication.LockAsync(cancellationToken);

    /// <summary>Logs out of the account.</summary>
    public Task<CliResult> LogoutAsync(CancellationToken cancellationToken = default) =>
        Authentication.LogoutAsync(cancellationToken);
}
