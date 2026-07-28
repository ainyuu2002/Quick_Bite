using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public enum WorkSessionApprovalStatus
{
    Draft = 0,
    Approved = 1
}

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

    public WorkSessionApprovalStatus ApprovalStatus { get; set; }
        = WorkSessionApprovalStatus.Draft;

    public DateTime? ApprovedCheckInAt { get; set; }

    public DateTime? ApprovedCheckOutAt { get; set; }

    [StringLength(500)]
    public string? ApprovalNote { get; set; }

    /// <summary>Snapshot đơn giá để thay đổi lương sau này không sửa bảng lương cũ.</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal? ApprovedHourlyRate { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? ApprovedByAccountId { get; set; }

    /// <summary>Thời lượng ca; ca đang mở thì tính tới thời điểm hiện tại.</summary>
    [NotMapped]
    public TimeSpan Duration => (CheckOutAt ?? DateTime.Now) - CheckInAt;

    [NotMapped]
    public TimeSpan? ApprovedDuration
        => ApprovedCheckInAt is not null && ApprovedCheckOutAt is not null
            ? ApprovedCheckOutAt.Value - ApprovedCheckInAt.Value
            : null;

    [NotMapped]
    public decimal? ApprovedSalary
        => ApprovedDuration is { } duration && ApprovedHourlyRate is { } rate
            ? Math.Round(
                (decimal)duration.TotalHours * rate,
                0,
                MidpointRounding.AwayFromZero)
            : null;
}
