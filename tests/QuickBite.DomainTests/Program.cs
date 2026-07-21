using QuickBite.Models;

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

foreach (var status in new[]
{
    OrderStatus.Pending,
    OrderStatus.Accepted,
    OrderStatus.Preparing,
    OrderStatus.Ready
})
{
    Assert(
        status.CanBeCancelledByStaff(),
        $"Admin phải hủy được đơn ở trạng thái {status}.");
}

Assert(
    !OrderStatus.Completed.CanBeCancelledByStaff(),
    "Admin không được hủy đơn Completed.");
Assert(
    !OrderStatus.Cancelled.CanBeCancelledByStaff(),
    "Admin không được hủy lại đơn Cancelled.");

Console.WriteLine("Admin order domain tests passed.");
