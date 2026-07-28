using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer("Server=(localdb)\\QuickBiteModelCheck;Database=QuickBiteModelCheck;Trusted_Connection=True;TrustServerCertificate=True")
    .Options;

using (var context = new AppDbContext(options))
{
    var model = context.Model;

    Assert(model.FindEntityType(typeof(Account)) is not null, "Model phải có Account.");
    Assert(model.FindEntityType(typeof(Category)) is not null, "Model phải có Category.");
    Assert(model.FindEntityType(typeof(MenuItem)) is not null, "Model phải có MenuItem.");
    Assert(model.FindEntityType(typeof(Order)) is not null, "Model phải có Order.");
    Assert(model.FindEntityType(typeof(OrderItem)) is not null, "Model phải có OrderItem.");
}

Console.WriteLine("EF model + seed validation passed.");

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
