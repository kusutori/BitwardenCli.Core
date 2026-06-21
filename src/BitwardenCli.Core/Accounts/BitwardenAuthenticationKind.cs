namespace BitwardenCli.Core.Accounts;

/// <summary>Identifies how an account authenticates with Bitwarden.</summary>
public enum BitwardenAuthenticationKind
{
    /// <summary>No authentication method has been selected.</summary>
    Unknown,

    /// <summary>Email and master password authentication.</summary>
    Password,

    /// <summary>Personal API key authentication.</summary>
    ApiKey,

    /// <summary>Single sign-on authentication.</summary>
    Sso
}
