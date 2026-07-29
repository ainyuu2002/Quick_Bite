namespace QuickBite.Services;

public class ConnectionTracker
{
    public sealed record OnlineAccount(int AccountId, string Username, int ConnectionCount);

    private readonly object _gate = new();

    private readonly Dictionary<string, int> _connectionAccount = new();

    private readonly Dictionary<int, OnlineAccount> _online = new();

    public bool Connect(string connectionId, int accountId, string username)
    {
        lock (_gate)
        {
            _connectionAccount[connectionId] = accountId;

            if (_online.TryGetValue(accountId, out var acc))
            {
                _online[accountId] = acc with { ConnectionCount = acc.ConnectionCount + 1 };
                return false;
            }

            _online[accountId] = new OnlineAccount(accountId, username, 1);
            return true;
        }
    }

    public int? Disconnect(string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionAccount.Remove(connectionId, out var accountId))
            {
                return null;
            }

            if (!_online.TryGetValue(accountId, out var acc))
            {
                return null;
            }

            if (acc.ConnectionCount <= 1)
            {
                _online.Remove(accountId);
                return accountId;
            }

            _online[accountId] = acc with { ConnectionCount = acc.ConnectionCount - 1 };
            return null;
        }
    }

    public int StaffOnline
    {
        get { lock (_gate) { return _online.Count; } }
    }

    public IReadOnlyList<OnlineAccount> OnlineAccounts
    {
        get { lock (_gate) { return _online.Values.ToList(); } }
    }
}
