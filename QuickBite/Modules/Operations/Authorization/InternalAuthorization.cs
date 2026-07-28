using QuickBite.Models;

namespace QuickBite.Modules.Operations.Authorization;

/// <summary>
/// Tên vai trò dùng trong claim, [Authorize] và SignalR.
/// Các module khác phải dùng constants này thay vì tự viết chuỗi.
/// </summary>
public static class InternalRoles
{
    public const string Manager = nameof(AccountRole.Manager);
    public const string Staff = nameof(AccountRole.Staff);
    public const string Kitchen = nameof(AccountRole.Kitchen);
    public const string Shipper = nameof(AccountRole.Shipper);
}

/// <summary>
/// Policy phản ánh ma trận phân quyền nghiệp vụ.
/// Tên policy là hợp đồng dùng chung; chi tiết quyền được cấu hình tại Program.cs.
/// </summary>
public static class InternalPolicies
{
    public const string ManagerOnly = nameof(ManagerOnly);
    public const string ReceiveOrders = nameof(ReceiveOrders);
    public const string OperateKitchen = nameof(OperateKitchen);
    public const string DeliverOrders = nameof(DeliverOrders);
}
