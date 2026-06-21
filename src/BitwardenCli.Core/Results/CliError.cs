namespace BitwardenCli.Core.Results;

/// <summary>Contains a stable error category and a diagnostic message.</summary>
public sealed record CliError(CliErrorCode Code, string Message, bool IsTransient = false);
