using QuickBite.Models;
using QuickBite.Modules.Operations.Authorization;
using QuickBite.Modules.Operations.MenuAvailability;
using QuickBite.Modules.Operations.Store;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

Assert(
    OrderStatus.Pending.CanTransitionTo(OrderStatus.Accepted),
    "Pending phải chuyển được sang Accepted.");
Assert(
    !OrderStatus.Pending.CanTransitionTo(OrderStatus.Ready),
    "Pending không được chuyển vượt cấp sang Ready.");
Assert(
    !OrderStatus.Accepted.CanTransitionTo(OrderStatus.Pending),
    "Không được chuyển lùi trạng thái.");

Assert(
    OrderStatus.Pending.CanBeCancelledByStaff(),
    "Admin phải được hủy đơn Pending.");

foreach (var status in new[]
{
    OrderStatus.Accepted,
    OrderStatus.Preparing,
    OrderStatus.Ready,
    OrderStatus.Completed,
    OrderStatus.Cancelled
})
{
    Assert(
        !status.CanBeCancelledByStaff(),
        $"Admin không được hủy đơn ở trạng thái {status}.");
}

Assert((int)AccountRole.Manager == 0, "Manager phải giữ giá trị 0 để tương thích dữ liệu Admin cũ.");
Assert((int)AccountRole.Staff == 1, "Staff phải giữ giá trị 1 để tương thích dữ liệu cũ.");
Assert(InternalRoles.Manager == nameof(AccountRole.Manager), "Role claim Manager không đồng bộ.");
Assert(InternalRoles.Staff == nameof(AccountRole.Staff), "Role claim Staff không đồng bộ.");
Assert(InternalRoles.Kitchen == nameof(AccountRole.Kitchen), "Role claim Kitchen không đồng bộ.");
Assert(InternalRoles.Shipper == nameof(AccountRole.Shipper), "Role claim Shipper không đồng bộ.");

var daytimeStore = new StoreSetting
{
    OpensAt = new TimeOnly(8, 0),
    ClosesAt = new TimeOnly(22, 0)
};
Assert(daytimeStore.IsWithinBusinessHours(new TimeOnly(8, 0)), "Phải mở đúng giờ bắt đầu.");
Assert(!daytimeStore.IsWithinBusinessHours(new TimeOnly(22, 0)), "Phải đóng đúng giờ kết thúc.");

var overnightStore = new StoreSetting
{
    OpensAt = new TimeOnly(18, 0),
    ClosesAt = new TimeOnly(2, 0)
};
Assert(overnightStore.IsWithinBusinessHours(new TimeOnly(23, 0)), "Ca qua đêm phải mở trước nửa đêm.");
Assert(overnightStore.IsWithinBusinessHours(new TimeOnly(1, 0)), "Ca qua đêm phải mở sau nửa đêm.");
Assert(!overnightStore.IsWithinBusinessHours(new TimeOnly(12, 0)), "Ca qua đêm phải đóng ngoài khung.");

var alwaysOpenStore = new StoreSetting
{
    OpensAt = new TimeOnly(0, 0),
    ClosesAt = new TimeOnly(0, 0)
};
Assert(alwaysOpenStore.IsWithinBusinessHours(new TimeOnly(12, 0)), "Giờ bằng nhau phải là mở 24 giờ.");

var dailyQuota = new DailyQuota
{
    DailyLimit = 30,
    ReservedQuantity = 28,
    QuotaDate = new DateOnly(2026, 7, 28),
    SaleStartsAt = new TimeOnly(6, 0),
    SaleEndsAt = new TimeOnly(10, 0)
};
Assert(dailyQuota.RemainingQuantity == 2, "Quota còn lại phải bằng giới hạn trừ số đã giữ.");
Assert(dailyQuota.IsWithinSaleWindow(new TimeOnly(8, 0)), "Món phải bán trong khung giờ.");
Assert(!dailyQuota.IsWithinSaleWindow(new TimeOnly(12, 0)), "Món phải ngừng bán ngoài khung giờ.");
dailyQuota.ResetFor(new DateOnly(2026, 7, 29));
Assert(dailyQuota.ReservedQuantity == 0, "Quota phải reset khi sang ngày mới.");

dailyQuota.SaleStartsAt = new TimeOnly(18, 0);
dailyQuota.SaleEndsAt = new TimeOnly(2, 0);
Assert(dailyQuota.IsWithinSaleWindow(new TimeOnly(23, 0)), "Khung bán qua đêm phải hỗ trợ trước nửa đêm.");
Assert(dailyQuota.IsWithinSaleWindow(new TimeOnly(1, 0)), "Khung bán qua đêm phải hỗ trợ sau nửa đêm.");

var approvedSession = new WorkSession
{
    ApprovedCheckInAt = new DateTime(2026, 7, 28, 8, 0, 0),
    ApprovedCheckOutAt = new DateTime(2026, 7, 28, 12, 30, 0),
    ApprovedHourlyRate = 25_000m
};
Assert(
    approvedSession.ApprovedDuration == TimeSpan.FromHours(4.5),
    "Thời gian duyệt phải tính từ hai mốc đã chốt.");
Assert(
    approvedSession.ApprovedSalary == 112_500m,
    "Lương phải bằng giờ đã duyệt nhân snapshot đơn giá.");

Console.WriteLine("Admin order domain tests passed.");
