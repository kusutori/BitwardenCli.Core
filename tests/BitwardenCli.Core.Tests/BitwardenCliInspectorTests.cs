using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Tests;

public sealed class BitwardenCliInspectorTests
{
    [Fact]
    public async Task Parses_calendar_version()
    {
        var inspector = new BitwardenCliInspector(new StubRunner(
            new CliProcessResult(CliProcessOutcome.Completed, 0, "2026.5.0", string.Empty, TimeSpan.Zero)));

        var result = await inspector.GetVersionAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new Version(2026, 5, 0), result.Value);
    }

    [Fact]
    public async Task Rejects_invalid_version_output()
    {
        var inspector = new BitwardenCliInspector(new StubRunner(
            new CliProcessResult(CliProcessOutcome.Completed, 0, "not-a-version", string.Empty, TimeSpan.Zero)));

        var result = await inspector.GetVersionAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.InvalidResponse, result.Error?.Code);
    }

    [Fact]
    public async Task Rejects_version_below_minimum()
    {
        var inspector = new BitwardenCliInspector(new StubRunner(
            new CliProcessResult(CliProcessOutcome.Completed, 0, "2024.1.0", string.Empty, TimeSpan.Zero)));

        var result = await inspector.CheckMinimumVersionAsync(new Version(2025, 1, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(CliErrorCode.UnsupportedVersion, result.Error?.Code);
    }

    private sealed class StubRunner(CliProcessResult result) : IBitwardenCliRunner
    {
        public Task<CliProcessResult> RunAsync(CliCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }
}
