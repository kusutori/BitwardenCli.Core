using BitwardenCli.Core.Accounts;

namespace BitwardenCli.Core.Authentication;

/// <summary>Provides secrets transiently without requiring the Core package to persist them.</summary>
public interface ISecretProvider
{
    /// <summary>Gets a secret for one account and purpose.</summary>
    ValueTask<string?> GetSecretAsync(
        BitwardenAccountProfile profile,
        SecretPurpose purpose,
        CancellationToken cancellationToken = default);
}
