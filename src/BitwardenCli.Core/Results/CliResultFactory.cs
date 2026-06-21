using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Results;

internal static class CliResultFactory
{
    public static CliResult FromProcess(CliProcessResult process) =>
        process.IsSuccess
            ? CliResult.Success(process.ExitCode, process.StandardError, process.Duration)
            : CliResult.Failure(
                CliErrorClassifier.Classify(process),
                process.ExitCode,
                process.StandardError,
                process.Duration);

    public static CliResult<T> Success<T>(T value, CliProcessResult process) =>
        CliResult<T>.Success(value, process.ExitCode, process.StandardError, process.Duration);

    public static CliResult<T> Failure<T>(CliProcessResult process) =>
        CliResult<T>.Failure(
            CliErrorClassifier.Classify(process),
            process.ExitCode,
            process.StandardError,
            process.Duration);

    public static CliResult<T> InvalidResponse<T>(CliProcessResult process, string message) =>
        CliResult<T>.Failure(
            new CliError(CliErrorCode.InvalidResponse, message),
            process.ExitCode,
            process.StandardError,
            process.Duration);
}
