namespace BitwardenCli.Core.Authentication;

/// <summary>Identifies why the CLI client needs a transient secret.</summary>
public enum SecretPurpose
{
    /// <summary>The account master password.</summary>
    MasterPassword,

    /// <summary>The personal API key client secret.</summary>
    ApiClientSecret,

    /// <summary>A two-factor authentication code.</summary>
    TwoFactorCode,

    /// <summary>A Bitwarden Send password.</summary>
    SendPassword,

    /// <summary>An encrypted export password.</summary>
    ExportPassword
}
