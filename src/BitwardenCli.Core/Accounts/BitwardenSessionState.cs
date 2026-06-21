namespace BitwardenCli.Core.Accounts;

/// <summary>Tracks an account session in memory without exposing its key publicly.</summary>
public sealed class BitwardenSessionState
{
    private readonly object _sync = new();
    private string? _sessionKey;

    /// <summary>Gets whether this client currently holds an unlock session.</summary>
    public bool IsUnlocked
    {
        get
        {
            lock (_sync)
            {
                return !string.IsNullOrWhiteSpace(_sessionKey);
            }
        }
    }

    internal string? GetSessionKey()
    {
        lock (_sync)
        {
            return _sessionKey;
        }
    }

    internal void SetSessionKey(string sessionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionKey);
        lock (_sync)
        {
            _sessionKey = sessionKey;
        }
    }

    internal void Clear()
    {
        lock (_sync)
        {
            _sessionKey = null;
        }
    }
}
