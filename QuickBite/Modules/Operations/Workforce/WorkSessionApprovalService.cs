using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Modules.Operations.Workforce;

public sealed class WorkSessionApprovalException : Exception
{
    public WorkSessionApprovalException(string message) : base(message)
    {
    }
}

public interface IWorkSessionApprovalService
{
    Task ApproveAsync(
        int workSessionId,
        DateTime approvedCheckInAt,
        DateTime approvedCheckOutAt,
        string? note,
        int managerAccountId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Chốt bản nháp máy ghi thành dữ liệu công đã duyệt. Một ca đã duyệt không được
/// ghi đè; nhờ đó bảng lương có dấu vết ổn định.
/// </summary>
public sealed class WorkSessionApprovalService : IWorkSessionApprovalService
{
    private static readonly TimeSpan MaximumShiftLength = TimeSpan.FromHours(24);
    private readonly AppDbContext _db;

    public WorkSessionApprovalService(AppDbContext db) => _db = db;

    public async Task ApproveAsync(
        int workSessionId,
        DateTime approvedCheckInAt,
        DateTime approvedCheckOutAt,
        string? note,
        int managerAccountId,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.WorkSessions
            .Include(item => item.Account)
            .SingleOrDefaultAsync(item => item.Id == workSessionId, cancellationToken)
            ?? throw new WorkSessionApprovalException("Không tìm thấy ca làm.");

        if (session.CheckOutAt is null)
        {
            throw new WorkSessionApprovalException(
                "Không thể duyệt khi nhân viên vẫn đang trong ca.");
        }

        if (session.ApprovalStatus == WorkSessionApprovalStatus.Approved)
        {
            throw new WorkSessionApprovalException("Ca làm này đã được chốt duyệt.");
        }

        var approvedDuration = approvedCheckOutAt - approvedCheckInAt;
        if (approvedDuration <= TimeSpan.Zero)
        {
            throw new WorkSessionApprovalException("Giờ ra phải sau giờ vào.");
        }

        if (approvedDuration > MaximumShiftLength)
        {
            throw new WorkSessionApprovalException("Một ca được duyệt không thể quá 24 giờ.");
        }

        // input datetime-local chỉ giữ tới phút, còn máy ghi có giây/millisecond.
        // Chỉ coi là điều chỉnh có chủ ý khi lệch từ một phút trở lên.
        var changedFromMachineRecord =
            Math.Abs((approvedCheckInAt - session.CheckInAt).TotalMinutes) >= 1
            || Math.Abs((approvedCheckOutAt - session.CheckOutAt.Value).TotalMinutes) >= 1;
        if (changedFromMachineRecord && string.IsNullOrWhiteSpace(note))
        {
            throw new WorkSessionApprovalException(
                "Phải ghi chú khi điều chỉnh giờ máy ghi.");
        }

        session.ApprovalStatus = WorkSessionApprovalStatus.Approved;
        session.ApprovedCheckInAt = approvedCheckInAt;
        session.ApprovedCheckOutAt = approvedCheckOutAt;
        session.ApprovalNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        session.ApprovedHourlyRate = session.Account.HourlyRate;
        session.ApprovedAt = DateTime.Now;
        session.ApprovedByAccountId = managerAccountId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
