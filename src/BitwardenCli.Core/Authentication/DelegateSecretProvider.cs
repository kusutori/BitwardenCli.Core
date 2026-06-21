using BitwardenCli.Core.Accounts;

namespace BitwardenCli.Core.Authentication;

/// <summary>Adapts a callback to <see cref="ISecretProvider"/>.</summary>
public sealed class DelegateSecretProvider : ISecretProvider
{
    private readonly Func<BitwardenAccountProfile, SecretPurpose, CancellationToken, ValueTask<string?>> _provider;

    /// <summary>Creates a callback-backed secret provider.</summary>
    public DelegateSecretProvider(
        Func<BitwardenAccountProfile, SecretPurpose, CancellationToken, ValueTask<string?>> provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <inheritdoc />
    public ValueTask<string?> GetSecretAsync(
        BitwardenAccountProfile profile,
        SecretPurpose purpose,
        CancellationToken cancellationToken = default) =>
        _provider(profile, purpose, cancellationToken);

    /// <summary>Creates a provider for a single master password value.</summary>
    public static DelegateSecretProvider FromMasterPassword(string masterPassword)
    {
        ArgumentNullException.ThrowIfNull(masterPassword);
        return new DelegateSecretProvider((_, purpose, _) =>
            ValueTask.FromResult<string?>(purpose == SecretPurpose.MasterPassword ? masterPassword : null));
    }
}
