using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Tests;

public sealed class ProcessBitwardenCliRunnerTests
{
    [Fact]
    public void Build_start_info_uses_argument_list_and_environment_overrides()
    {
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = "bw",
            AdditionalEnvironment = new Dictionary<string, string>
            {
                ["SAMPLE"] = "global"
            }
        });
        var command = new CliCommand("status", CliArgument.Plain("status"))
        {
            Environment = [CliEnvironmentVariable.Plain("SAMPLE", "command")]
        };

        var startInfo = runner.BuildStartInfo(command);

        Assert.Equal("bw", startInfo.FileName);
        Assert.Equal(["--nointeraction", "status"], startInfo.ArgumentList);
        Assert.Equal("command", startInfo.Environment["SAMPLE"]);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
    }

    [Fact]
    public async Task Run_returns_start_failed_for_missing_executable()
    {
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = $"missing-bw-{Guid.NewGuid():N}"
        });

        var result = await runner.RunAsync(new CliCommand("status", CliArgument.Plain("status")));

        Assert.Equal(CliProcessOutcome.StartFailed, result.Outcome);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.FailureMessage ?? string.Empty);
    }

    [Fact]
    public async Task Run_executes_a_real_process_without_nointeraction()
    {
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = "dotnet"
        });
        var command = new CliCommand("dotnet-version", CliArgument.Plain("--version"))
        {
            NoInteraction = false
        };

        var result = await runner.RunAsync(command);

        Assert.True(result.IsSuccess, result.StandardError + result.FailureMessage);
        Assert.Matches(@"^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Diagnostics_never_include_secret_values()
    {
        CliDiagnosticEvent? captured = null;
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = "dotnet",
            DiagnosticSink = value => captured = value
        });
        var command = new CliCommand("dotnet-version", CliArgument.Secret("--version"))
        {
            NoInteraction = false,
            Environment = [CliEnvironmentVariable.Secret("TEST_SECRET", "hidden-value")]
        };

        await runner.RunAsync(command);

        Assert.NotNull(captured);
        Assert.Equal("[REDACTED]", Assert.Single(captured.Arguments));
        Assert.Contains("TEST_SECRET", captured.EnvironmentNames);
        Assert.DoesNotContain("hidden-value", string.Join(' ', captured.EnvironmentNames));
    }

    [Fact]
    public async Task Run_writes_standard_input_without_exposing_it_to_diagnostics()
    {
        CliDiagnosticEvent? captured = null;
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = "pwsh",
            DiagnosticSink = value => captured = value
        });
        var command = new CliCommand(
            "stdin",
            CliArgument.Plain("-NoProfile"),
            CliArgument.Plain("-Command"),
            CliArgument.Plain("[Console]::Out.Write([Console]::In.ReadToEnd())"))
        {
            NoInteraction = false,
            StandardInput = "sensitive-input",
            IsStandardInputSensitive = true
        };

        var result = await runner.RunAsync(command);

        Assert.True(result.IsSuccess, result.StandardError + result.FailureMessage);
        Assert.Equal("sensitive-input", result.StandardOutput);
        Assert.NotNull(captured);
        Assert.True(captured.HadStandardInput);
        Assert.True(captured.StandardInputWasSensitive);
        Assert.DoesNotContain("sensitive-input", string.Join(' ', captured.Arguments));
    }

    [Fact]
    public async Task Run_times_out_and_kills_process()
    {
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = "pwsh"
        });
        var command = new CliCommand(
            "sleep",
            CliArgument.Plain("-NoProfile"),
            CliArgument.Plain("-Command"),
            CliArgument.Plain("Start-Sleep -Seconds 30"))
        {
            NoInteraction = false,
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        var result = await runner.RunAsync(command);

        Assert.Equal(CliProcessOutcome.TimedOut, result.Outcome);
        Assert.True(result.Duration < TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Run_honors_caller_cancellation()
    {
        var runner = new ProcessBitwardenCliRunner(new BitwardenCliOptions
        {
            ExecutablePath = "pwsh"
        });
        var command = new CliCommand(
            "sleep",
            CliArgument.Plain("-NoProfile"),
            CliArgument.Plain("-Command"),
            CliArgument.Plain("Start-Sleep -Seconds 30"))
        {
            NoInteraction = false,
            Timeout = TimeSpan.FromMinutes(1)
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var result = await runner.RunAsync(command, cancellation.Token);

        Assert.Equal(CliProcessOutcome.Cancelled, result.Outcome);
        Assert.True(result.Duration < TimeSpan.FromSeconds(10));
    }
}
