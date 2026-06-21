using System.Diagnostics;
using System.Text;

namespace BitwardenCli.Core.Execution;

/// <summary>Executes Bitwarden CLI commands using a child process.</summary>
public sealed class ProcessBitwardenCliRunner : IBitwardenCliRunner
{
    private const int NoExitCode = -1;
    private readonly BitwardenCliOptions _options;

    /// <summary>Creates a process runner.</summary>
    public ProcessBitwardenCliRunner(BitwardenCliOptions? options = null)
    {
        _options = options ?? new BitwardenCliOptions();
        _options.Validate();
    }

    /// <inheritdoc />
    public async Task<CliProcessResult> RunAsync(
        CliCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var stopwatch = Stopwatch.StartNew();
        using var process = new Process { StartInfo = BuildStartInfo(command) };

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            var failed = new CliProcessResult(
                CliProcessOutcome.StartFailed,
                NoExitCode,
                string.Empty,
                string.Empty,
                stopwatch.Elapsed,
                exception.Message);
            PublishDiagnostic(command, failed);
            return failed;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        var stdinTask = WriteStandardInputAsync(process, command.StandardInput);
        var timeout = command.Timeout ?? _options.DefaultTimeout;
        using var timeoutSource = timeout == Timeout.InfiniteTimeSpan
            ? new CancellationTokenSource()
            : new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        CliProcessOutcome outcome;
        try
        {
            await stdinTask.ConfigureAwait(false);
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
            outcome = CliProcessOutcome.Completed;
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            outcome = cancellationToken.IsCancellationRequested
                ? CliProcessOutcome.Cancelled
                : CliProcessOutcome.TimedOut;
        }

        var standardOutput = await stdoutTask.ConfigureAwait(false);
        var standardError = await stderrTask.ConfigureAwait(false);
        var result = new CliProcessResult(
            outcome,
            process.HasExited ? process.ExitCode : NoExitCode,
            standardOutput.Trim(),
            standardError.Trim(),
            stopwatch.Elapsed);
        PublishDiagnostic(command, result);
        return result;
    }

    internal ProcessStartInfo BuildStartInfo(CliCommand command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            RedirectStandardInput = command.StandardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (startInfo.RedirectStandardInput)
        {
            startInfo.StandardInputEncoding = Encoding.UTF8;
        }

        if (command.NoInteraction)
        {
            startInfo.ArgumentList.Add("--nointeraction");
        }

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument.Value);
        }

        foreach (var variable in _options.AdditionalEnvironment)
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        foreach (var variable in command.Environment)
        {
            startInfo.Environment[variable.Name] = variable.Value;
        }

        return startInfo;
    }

    private static async Task WriteStandardInputAsync(Process process, string? standardInput)
    {
        if (standardInput is null)
        {
            return;
        }

        await process.StandardInput.WriteAsync(standardInput).ConfigureAwait(false);
        await process.StandardInput.FlushAsync().ConfigureAwait(false);
        process.StandardInput.Close();
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void PublishDiagnostic(CliCommand command, CliProcessResult result)
    {
        if (_options.DiagnosticSink is not { } sink)
        {
            return;
        }

        var diagnostic = new CliDiagnosticEvent(
            command.Operation,
            command.GetSafeArguments(),
            command.GetEnvironmentNames(),
            command.StandardInput is not null,
            command.IsStandardInputSensitive,
            result.Outcome,
            result.ExitCode,
            result.Duration);
        try
        {
            sink(diagnostic);
        }
        catch
        {
            // Diagnostics must never change command behavior.
        }
    }
}
