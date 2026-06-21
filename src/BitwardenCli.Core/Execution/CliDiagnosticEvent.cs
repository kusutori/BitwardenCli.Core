namespace BitwardenCli.Core.Execution;

/// <summary>Contains non-sensitive metadata about a completed CLI invocation.</summary>
public sealed record CliDiagnosticEvent(
    string Operation,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<string> EnvironmentNames,
    bool HadStandardInput,
    bool StandardInputWasSensitive,
    CliProcessOutcome Outcome,
    int ExitCode,
    TimeSpan Duration);
