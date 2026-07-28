namespace QuickBite.Models;

public enum AccountRole
{
    Admin = 0,
    Staff = 1,
    Kitchen = 2,
    Shipper = 3,
    Manager = 4
}

public static class AccountRoleExtensions
{
    public static string ToDisplayText(this AccountRole role) => role switch
    {
        AccountRole.Admin => "Chủ quán",
        AccountRole.Staff => "Nhân viên",
        AccountRole.Kitchen => "Bếp",
        AccountRole.Shipper => "Shipper",
        AccountRole.Manager => "Quản lý",
        _ => role.ToString()
    };
}
