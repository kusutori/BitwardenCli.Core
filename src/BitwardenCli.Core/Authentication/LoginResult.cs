using BitwardenCli.Core.Models;

namespace BitwardenCli.Core.Authentication;

/// <summary>Contains the account state after a successful login.</summary>
public sealed record LoginResult(BitwardenStatus? Status, bool HasSession);
