using System.Collections.Concurrent;

namespace QuickBite.Services;

/// <summary>
/// Singleton — theo dõi kết nối SignalR đang online (role: "staff" hoặc "customer").
/// Tạm thời do Dev A tạo để không chặn tiến độ; Dev D (chủ trì SignalR) có thể bổ sung/chỉnh sau.
/// </summary>
public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public void Add(string connectionId, string role) => _connections[connectionId] = role;

    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);

    public int StaffOnline => _connections.Count(c => c.Value == "staff");
}
