namespace QuickBite.Models;

public enum AccountRole
{
    /// <summary>Chủ quán: toàn quyền vận hành và xem báo cáo.</summary>
    Manager = 0,

    /// <summary>Nhân viên nhận đơn: xác nhận, từ chối và xử lý thanh toán.</summary>
    Staff = 1,

    /// <summary>Bếp: chỉ xem hàng đợi và cập nhật tiến độ chế biến.</summary>
    Kitchen = 2,

    /// <summary>Giao hàng: chỉ xử lý các đơn được giao cho mình.</summary>
    Shipper = 3
}

public static class AccountRoleExtensions
{
    public static string ToDisplayText(this AccountRole role) => role switch
    {
        AccountRole.Manager => "Chủ quán",
        AccountRole.Staff => "Nhân viên nhận đơn",
        AccountRole.Kitchen => "Bếp",
        AccountRole.Shipper => "Nhân viên giao hàng",
        _ => role.ToString()
    };
}
