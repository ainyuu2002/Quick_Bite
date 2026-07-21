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

Console.WriteLine("Admin order domain tests passed.");
