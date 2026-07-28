using System.ComponentModel.DataAnnotations;

namespace QuickBite.Modules.Operations.Store;

/// <summary>
/// Cấu hình vận hành dùng chung của một cửa hàng QuickBite.
/// Bản ghi duy nhất luôn có Id = 1.
/// </summary>
public sealed class StoreSetting
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public TimeOnly OpensAt { get; set; } = new(8, 0);

    public TimeOnly ClosesAt { get; set; } = new(22, 0);

    public bool IsPaused { get; set; }

    [StringLength(200)]
    public string? PauseReason { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByAccountId { get; set; }

    /// <summary>
    /// Kiểm tra giờ hoạt động, hỗ trợ cả ca qua đêm (ví dụ 18:00–02:00).
    /// Giờ mở bằng giờ đóng được hiểu là mở cửa 24 giờ.
    /// </summary>
    public bool IsWithinBusinessHours(TimeOnly currentTime)
    {
        if (OpensAt == ClosesAt)
        {
            return true;
        }

        return OpensAt < ClosesAt
            ? currentTime >= OpensAt && currentTime < ClosesAt
            : currentTime >= OpensAt || currentTime < ClosesAt;
    }
}
