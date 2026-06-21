namespace BitwardenCli.Core.Authentication;

/// <summary>Confirms that an account session was created.</summary>
public sealed record UnlockResult(bool HasSession);
