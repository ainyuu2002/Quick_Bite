using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Models;

/// <summary>
/// Một ca làm việc = khoảng thời gian tài khoản có kết nối SignalR sống tới khu quản trị.
/// <para>
/// Cố ý KHÔNG chấm công theo hành động "bấm đăng xuất": phần lớn người dùng đóng tab
/// chứ không bấm logout, ca làm sẽ bị treo sai. Nguồn sự thật là kết nối SignalR
/// (ConnectionTracker) — mất kết nối cuối cùng thì đóng ca.
/// </para>
/// </summary>
public class WorkSession
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public DateTime CheckInAt { get; set; }

    /// <summary>Null nghĩa là đang trong ca.</summary>
    public DateTime? CheckOutAt { get; set; }

    /// <summary>Thời lượng ca; ca đang mở thì tính tới thời điểm hiện tại.</summary>
    [NotMapped]
    public TimeSpan Duration => (CheckOutAt ?? DateTime.Now) - CheckInAt;
}
