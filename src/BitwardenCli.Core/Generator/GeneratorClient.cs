using System.Globalization;
using System.Security.Cryptography;
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

public enum UsernameGenerationType
{
    RandomWord,
    EmailPrefix,
    WebsitePrefix
}

public sealed record UsernameGenerationOptions
{
    public UsernameGenerationType Type { get; init; } = UsernameGenerationType.RandomWord;
    public bool Capitalize { get; init; }
    public bool IncludeNumber { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
}

public sealed class GeneratorClient
{
    private static readonly string[] UsernameWords =
    [
        "able", "amber", "anchor", "apex", "atlas", "autumn", "binary", "bright",
        "calm", "cedar", "cinder", "cobalt", "comet", "coral", "delta", "dune",
        "ember", "fable", "frost", "harbor", "hazel", "lunar", "meadow", "nova",
        "onyx", "orbit", "pixel", "quiet", "raven", "river", "sage", "scared",
        "signal", "silver", "solace", "summit", "tidal", "velvet", "winter", "zenith"
    ];

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

    public Task<CliResult<string>> GenerateUsernameAsync(UsernameGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options ??= new UsernameGenerationOptions();
        var username = options.Type switch
        {
            UsernameGenerationType.EmailPrefix => PrefixFromEmail(options.Email),
            UsernameGenerationType.WebsitePrefix => PrefixFromWebsite(options.Website),
            _ => UsernameWords[RandomNumberGenerator.GetInt32(UsernameWords.Length)]
        };

        if (options.Capitalize && username.Length > 0)
        {
            username = char.ToUpperInvariant(username[0]) + username[1..];
        }

        if (options.IncludeNumber)
        {
            username += RandomNumberGenerator.GetInt32(0, 10000).ToString("D4", CultureInfo.InvariantCulture);
        }

        return Task.FromResult(CliResult<string>.Success(username, 0, string.Empty, TimeSpan.Zero));
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

    private static string PrefixFromEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required for email-prefix username generation.", nameof(email));
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        var prefix = at > 0 ? email[..at] : email;
        return SanitizeUsername(prefix);
    }

    private static string PrefixFromWebsite(string? website)
    {
        if (string.IsNullOrWhiteSpace(website))
        {
            throw new ArgumentException("Website is required for website-prefix username generation.", nameof(website));
        }

        var text = website.Trim();
        if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            text = $"https://{text}";
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return SanitizeUsername(website);
        }

        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
        var firstPart = host.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? host;
        return SanitizeUsername(firstPart);
    }

    private static string SanitizeUsername(string value)
    {
        var chars = value.Trim().Where(char.IsLetterOrDigit).ToArray();
        return chars.Length > 0 ? new string(chars).ToLowerInvariant() : UsernameWords[RandomNumberGenerator.GetInt32(UsernameWords.Length)];
    }
}
#pragma warning restore CS1591
