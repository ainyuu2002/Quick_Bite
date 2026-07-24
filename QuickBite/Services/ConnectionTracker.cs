namespace QuickBite.Services;

/// <summary>
/// Theo dõi nhân viên đang online theo kết nối SignalR — Singleton, sống suốt vòng đời app.
/// <para>
/// CỐ Ý chỉ giữ trạng thái trong bộ nhớ, KHÔNG chạm DB: Singleton mà inject AppDbContext
/// (scoped) là một lỗi vòng đời kinh điển. Nơi này chỉ trả lời "đây có phải kết nối
/// đầu tiên / cuối cùng của tài khoản không"; việc ghi WorkSession xuống DB do
/// <see cref="WorkSessionService"/> (scoped) lo, gọi từ OrderHub.
/// </para>
/// <para>
/// Một tài khoản có thể mở nhiều tab = nhiều kết nối. Chỉ khi kết nối ĐẦU TIÊN mở
/// (0 → 1) mới tính là "vào ca", và khi kết nối CUỐI CÙNG đóng (1 → 0) mới là "tan ca".
/// Dùng một lock duy nhất cho mọi thao tác — với quy mô demo thì đơn giản và chắc chắn
/// đúng, hơn là lý luận về tính nguyên tử của nhiều cấu trúc lock-free ghép lại.
/// </para>
/// </summary>
public class ConnectionTracker
{
    public sealed record OnlineAccount(int AccountId, string Username, int ConnectionCount);

    private readonly object _gate = new();

    // connectionId -> accountId. Cần để lúc ngắt kết nối biết connection này thuộc về ai.
    private readonly Dictionary<string, int> _connectionAccount = new();

    // accountId -> thông tin online (kèm số tab đang mở).
    private readonly Dictionary<int, OnlineAccount> _online = new();

    /// <summary>
    /// Ghi nhận một kết nối staff. Trả về <c>true</c> nếu đây là kết nối đầu tiên của
    /// tài khoản (0 → 1) — tức là vừa VÀO CA, bên gọi cần mở WorkSession.
    /// </summary>
    public bool Connect(string connectionId, int accountId, string username)
    {
        lock (_gate)
        {
            _connectionAccount[connectionId] = accountId;

            if (_online.TryGetValue(accountId, out var acc))
            {
                _online[accountId] = acc with { ConnectionCount = acc.ConnectionCount + 1 };
                return false;   // đã online sẵn ở tab khác → không phải vào ca mới
            }

            _online[accountId] = new OnlineAccount(accountId, username, 1);
            return true;        // 0 → 1: vào ca
        }
    }

    /// <summary>
    /// Gỡ một kết nối. Trả về accountId nếu đó là kết nối CUỐI CÙNG của tài khoản
    /// (1 → 0) — tức là vừa TAN CA, bên gọi cần đóng WorkSession. Trả về <c>null</c>
    /// nếu tài khoản vẫn còn tab khác, hoặc connection này không phải của staff
    /// (ví dụ khách vãng lai chỉ gọi WatchOrder, chưa bao giờ Connect).
    /// </summary>
    public int? Disconnect(string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionAccount.Remove(connectionId, out var accountId))
            {
                return null;    // không phải kết nối staff → bỏ qua
            }

            if (!_online.TryGetValue(accountId, out var acc))
            {
                return null;    // phòng hờ trạng thái lệch, không nên xảy ra
            }

            if (acc.ConnectionCount <= 1)
            {
                _online.Remove(accountId);
                return accountId;   // 1 → 0: tan ca
            }

            _online[accountId] = acc with { ConnectionCount = acc.ConnectionCount - 1 };
            return null;            // vẫn còn tab khác đang mở
        }
    }

    /// <summary>Số NGƯỜI (tài khoản riêng biệt) đang trực, không phải số tab.</summary>
    public int StaffOnline
    {
        get { lock (_gate) { return _online.Count; } }
    }

    /// <summary>Danh sách người đang trực — phục vụ hiển thị "ai đang online".</summary>
    public IReadOnlyList<OnlineAccount> OnlineAccounts
    {
        get { lock (_gate) { return _online.Values.ToList(); } }
    }
}
