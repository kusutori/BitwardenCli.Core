using System.Globalization;
using System.Text.Json;
using BitwardenCli.Core.Execution;
using BitwardenCli.Core.Internal;
using BitwardenCli.Core.Models;
using BitwardenCli.Core.Results;
using BitwardenCli.Core.Serialization;
using BitwardenCli.Core.Accounts;

namespace BitwardenCli.Core.Authentication;

/// <summary>Provides account authentication, status and server configuration commands.</summary>
public sealed class AuthenticationClient
{
    private readonly AccountCommandContext _context;

    internal AuthenticationClient(AccountCommandContext context)
    {
        _context = context;
    }

    /// <summary>Gets the current account status.</summary>
    public async Task<CliResult<BitwardenStatus>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var process = await _context.RunAsync(
            new CliCommand("status", CliArgument.Plain("status")),
            includeSession: true,
            serializeMutation: false,
            cancellationToken).ConfigureAwait(false);
        if (!process.IsSuccess)
        {
            return CliResultFactory.Failure<BitwardenStatus>(process);
        }

        try
        {
            var status = JsonSerializer.Deserialize(
                process.StandardOutput,
                BitwardenJsonContext.Default.BitwardenStatus);
            if (status is null)
            {
                return CliResultFactory.InvalidResponse<BitwardenStatus>(process, "Bitwarden CLI returned an empty status.");
            }

            _context.UpdateProfile(profile => profile with
            {
                Email = status.UserEmail ?? profile.Email,
                UserId = status.UserId ?? profile.UserId,
                ServerUrl = status.ServerUrl ?? profile.ServerUrl,
                LastUsedAt = DateTimeOffset.UtcNow
            });
            return CliResultFactory.Success(status, process);
        }
        catch (JsonException exception)
        {
            return CliResultFactory.InvalidResponse<BitwardenStatus>(process, exception.Message);
        }
    }

    /// <summary>Logs in using the selected authentication method.</summary>
    public async Task<CliResult<LoginResult>> LoginAsync(
        LoginRequest request,
        ISecretProvider? secretProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var process = request switch
        {
            PasswordLoginRequest password => await LoginWithPasswordAsync(
                password,
                secretProvider,
                cancellationToken).ConfigureAwait(false),
            ApiKeyLoginRequest apiKey => await LoginWithApiKeyAsync(
                apiKey,
                secretProvider,
                cancellationToken).ConfigureAwait(false),
            SsoLoginRequest sso => await LoginWithSsoAsync(sso, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
        if (!process.IsSuccess)
        {
            return CliResultFactory.Failure<LoginResult>(process);
        }

        if (request is PasswordLoginRequest)
        {
            var sessionKey = process.StandardOutput.Trim();
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                return CliResultFactory.InvalidResponse<LoginResult>(
                    process,
                    "Bitwarden CLI did not return a session key after password login.");
            }

            _context.Session.SetSessionKey(sessionKey);
        }

        var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        _context.UpdateProfile(profile => profile with
        {
            AuthenticationKind = request switch
            {
                PasswordLoginRequest => BitwardenAuthenticationKind.Password,
                ApiKeyLoginRequest => BitwardenAuthenticationKind.ApiKey,
                SsoLoginRequest => BitwardenAuthenticationKind.Sso,
                _ => profile.AuthenticationKind
            }
        });
        return CliResultFactory.Success(
            new LoginResult(status.IsSuccess ? status.Value : null, _context.Session.IsUnlocked),
            process);
    }

    /// <summary>Unlocks the vault using a master password from a secret provider.</summary>
    public async Task<CliResult<UnlockResult>> UnlockAsync(
        ISecretProvider secretProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secretProvider);
        var password = await secretProvider.GetSecretAsync(
            _context.Profile,
            SecretPurpose.MasterPassword,
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(password))
        {
            return AccountResultFactory.MissingSecret<UnlockResult>(SecretPurpose.MasterPassword);
        }

        var passwordVariable = $"BW_PASSWORD_{Guid.NewGuid():N}";
        var command = new CliCommand(
            "unlock",
            CliArgument.Plain("unlock"),
            CliArgument.Plain("--passwordenv"),
            CliArgument.Plain(passwordVariable),
            CliArgument.Plain("--raw"))
        {
            Environment = [CliEnvironmentVariable.Secret(passwordVariable, password)]
        };
        var process = await _context.RunAsync(
            command,
            includeSession: false,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
        if (!process.IsSuccess)
        {
            return CliResultFactory.Failure<UnlockResult>(process);
        }

        var sessionKey = process.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            return CliResultFactory.InvalidResponse<UnlockResult>(
                process,
                "Bitwarden CLI did not return a session key after unlock.");
        }

        _context.Session.SetSessionKey(sessionKey);
        return CliResultFactory.Success(new UnlockResult(true), process);
    }

    /// <summary>Locks the account vault and clears the in-memory session.</summary>
    public async Task<CliResult> LockAsync(CancellationToken cancellationToken = default)
    {
        var process = await _context.RunAsync(
            new CliCommand("lock", CliArgument.Plain("lock")),
            includeSession: false,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
        var result = CliResultFactory.FromProcess(process);
        if (result.IsSuccess)
        {
            _context.Session.Clear();
        }

        return result;
    }

    /// <summary>Logs out the account and clears the in-memory session.</summary>
    public async Task<CliResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var process = await _context.RunAsync(
            new CliCommand("logout", CliArgument.Plain("logout")),
            includeSession: false,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
        var result = CliResultFactory.FromProcess(process);
        if (result.IsSuccess)
        {
            _context.Session.Clear();
        }

        return result;
    }

    /// <summary>Configures the cloud or self-hosted server for this profile.</summary>
    public async Task<CliResult> ConfigureServerAsync(
        string serverUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return CliResult.Failure(
                new CliError(CliErrorCode.InvalidArguments, "Server URL must be an absolute HTTP or HTTPS URL."),
                -1,
                string.Empty,
                TimeSpan.Zero);
        }

        var process = await _context.RunAsync(
            new CliCommand(
                "config-server",
                CliArgument.Plain("config"),
                CliArgument.Plain("server"),
                CliArgument.Plain(uri.AbsoluteUri.TrimEnd('/'))),
            includeSession: false,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
        return CliResultFactory.FromProcess(process);
    }

    private async Task<CliProcessResult> LoginWithPasswordAsync(
        PasswordLoginRequest request,
        ISecretProvider? secretProvider,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        if (secretProvider is null)
        {
            return MissingSecretProcess(SecretPurpose.MasterPassword);
        }

        var password = await secretProvider.GetSecretAsync(
            _context.Profile,
            SecretPurpose.MasterPassword,
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(password))
        {
            return MissingSecretProcess(SecretPurpose.MasterPassword);
        }

        var arguments = new List<CliArgument>
        {
            CliArgument.Plain("login"),
            CliArgument.Plain(request.Email),
            CliArgument.Plain("--passwordenv")
        };
        var passwordVariable = $"BW_PASSWORD_{Guid.NewGuid():N}";
        arguments.Add(CliArgument.Plain(passwordVariable));
        arguments.Add(CliArgument.Plain("--raw"));
        if (request.TwoFactorMethod is { } method)
        {
            arguments.Add(CliArgument.Plain("--method"));
            arguments.Add(CliArgument.Plain(method.ToString(CultureInfo.InvariantCulture)));
        }

        if (request.IncludeTwoFactorCode)
        {
            var code = await secretProvider.GetSecretAsync(
                _context.Profile,
                SecretPurpose.TwoFactorCode,
                cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(code))
            {
                return MissingSecretProcess(SecretPurpose.TwoFactorCode);
            }

            arguments.Add(CliArgument.Plain("--code"));
            arguments.Add(CliArgument.Secret(code));
        }

        return await _context.RunAsync(
            new CliCommand("login-password", arguments.ToArray())
            {
                Environment = [CliEnvironmentVariable.Secret(passwordVariable, password)],
                NoInteraction = true
            },
            includeSession: false,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CliProcessResult> LoginWithApiKeyAsync(
        ApiKeyLoginRequest request,
        ISecretProvider? secretProvider,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ClientId);
        if (secretProvider is null)
        {
            return MissingSecretProcess(SecretPurpose.ApiClientSecret);
        }

        var secret = await secretProvider.GetSecretAsync(
            _context.Profile,
            SecretPurpose.ApiClientSecret,
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return MissingSecretProcess(SecretPurpose.ApiClientSecret);
        }

        return await _context.RunAsync(
            new CliCommand(
                "login-apikey",
                CliArgument.Plain("login"),
                CliArgument.Plain("--apikey"))
            {
                Environment =
                [
                    CliEnvironmentVariable.Secret("BW_CLIENTID", request.ClientId),
                    CliEnvironmentVariable.Secret("BW_CLIENTSECRET", secret)
                ]
            },
            includeSession: false,
            serializeMutation: true,
            cancellationToken).ConfigureAwait(false);
    }

    private Task<CliProcessResult> LoginWithSsoAsync(
        SsoLoginRequest request,
        CancellationToken cancellationToken)
    {
        var arguments = new List<CliArgument>
        {
            CliArgument.Plain("login"),
            CliArgument.Plain("--sso")
        };
        if (!string.IsNullOrWhiteSpace(request.OrganizationIdentifier))
        {
            arguments.Add(CliArgument.Plain(request.OrganizationIdentifier));
        }

        return _context.RunAsync(
            new CliCommand("login-sso", arguments.ToArray()) { NoInteraction = false },
            includeSession: false,
            serializeMutation: true,
            cancellationToken);
    }

    private static CliProcessResult MissingSecretProcess(SecretPurpose purpose) => new(
        CliProcessOutcome.Completed,
        -1,
        string.Empty,
        $"A secret for {purpose} was not provided.",
        TimeSpan.Zero);
}
