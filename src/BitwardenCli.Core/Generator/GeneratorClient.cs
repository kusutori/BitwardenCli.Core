using System.Globalization;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Results;

namespace BitwardenCli.Core.Generator;

#pragma warning disable CS1591
public sealed record PasswordGenerationOptions
{
    public int Length { get; init; } = 14;
    public bool Uppercase { get; init; } = true;
    public bool Lowercase { get; init; } = true;
    public bool Numbers { get; init; } = true;
    public bool Special { get; init; }
    public bool AvoidAmbiguous { get; init; }
    public int? MinimumNumbers { get; init; }
    public int? MinimumSpecial { get; init; }
}

public sealed record PassphraseGenerationOptions
{
    public int Words { get; init; } = 3;
    public string Separator { get; init; } = "-";
    public bool Capitalize { get; init; }
    public bool IncludeNumber { get; init; }
}

public sealed class GeneratorClient
{
    private readonly AccountCommandContext _context;
    internal GeneratorClient(AccountCommandContext context) => _context = context;

    public Task<CliResult<string>> GeneratePasswordAsync(PasswordGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new PasswordGenerationOptions();
        if (options.Length < 5) throw new ArgumentOutOfRangeException(nameof(options), "Password length must be at least 5.");
        var args = new List<CliArgument> { CliArgument.Plain("generate") };
        if (options.Uppercase) args.Add(CliArgument.Plain("--uppercase")); if (options.Lowercase) args.Add(CliArgument.Plain("--lowercase"));
        if (options.Numbers) args.Add(CliArgument.Plain("--number")); if (options.Special) args.Add(CliArgument.Plain("--special"));
        args.Add(CliArgument.Plain("--length")); args.Add(CliArgument.Plain(options.Length.ToString(CultureInfo.InvariantCulture)));
        if (options.AvoidAmbiguous) args.Add(CliArgument.Plain("--ambiguous"));
        AddCount(args, "--minNumber", options.MinimumNumbers); AddCount(args, "--minSpecial", options.MinimumSpecial);
        return RunAsync("generate-password", args, cancellationToken);
    }

    public Task<CliResult<string>> GeneratePassphraseAsync(PassphraseGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new PassphraseGenerationOptions();
        if (options.Words < 3) throw new ArgumentOutOfRangeException(nameof(options), "Passphrase word count must be at least 3.");
        var args = new List<CliArgument> { CliArgument.Plain("generate"), CliArgument.Plain("--passphrase"), CliArgument.Plain("--words"), CliArgument.Plain(options.Words.ToString(CultureInfo.InvariantCulture)), CliArgument.Plain("--separator"), CliArgument.Plain(options.Separator) };
        if (options.Capitalize) args.Add(CliArgument.Plain("--capitalize")); if (options.IncludeNumber) args.Add(CliArgument.Plain("--includeNumber"));
        return RunAsync("generate-passphrase", args, cancellationToken);
    }

    private async Task<CliResult<string>> RunAsync(string operation, List<CliArgument> args, CancellationToken cancellationToken)
    {
        var process = await _context.RunAsync(new CliCommand(operation, [.. args]), false, false, cancellationToken);
        return process.IsSuccess ? CliResultFactory.Success(process.StandardOutput.Trim(), process) : CliResultFactory.Failure<string>(process);
    }

    private static void AddCount(List<CliArgument> args, string name, int? count)
    {
        if (count is null) return; if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        args.Add(CliArgument.Plain(name)); args.Add(CliArgument.Plain(count.Value.ToString(CultureInfo.InvariantCulture)));
    }
}
#pragma warning restore CS1591
