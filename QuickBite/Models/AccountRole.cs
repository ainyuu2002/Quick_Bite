namespace QuickBite.Models;

public enum AccountRole
{
    /// <summary>Chủ quán: toàn quyền — quản lý món ăn, quản lý tài khoản, xử lý đơn.</summary>
    Admin = 0,

    /// <summary>Nhân viên: chỉ xử lý đơn hàng.</summary>
    Staff = 1
}

public static class AccountRoleExtensions
{
    public static string ToDisplayText(this AccountRole role) => role switch
    {
        AccountRole.Admin => "Chủ quán",
        AccountRole.Staff => "Nhân viên",
        _ => role.ToString()
    };
}
