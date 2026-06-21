using System.Collections.Concurrent;
using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Tests.TestSupport;

internal sealed class CapturingRunner(params CliProcessResult[] results) : IBitwardenCliRunner
{
    private readonly ConcurrentQueue<CliProcessResult> _results = new(results);
    private readonly ConcurrentQueue<CliCommand> _commands = new();

    public IReadOnlyList<CliCommand> Commands => _commands.ToArray();

    public Task<CliProcessResult> RunAsync(CliCommand command, CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command);
        if (!_results.TryDequeue(out var result))
        {
            result = Success();
        }

        return Task.FromResult(result);
    }

    public static CliProcessResult Success(string output = "") => new(
        CliProcessOutcome.Completed,
        0,
        output,
        string.Empty,
        TimeSpan.FromMilliseconds(1));
}
