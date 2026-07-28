using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;

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

Assert(OrderStatus.Pending.CanTransitionTo(OrderStatus.Confirmed), "Pending -> Confirmed hợp lệ.");
Assert(OrderStatus.Pending.CanTransitionTo(OrderStatus.Rejected), "Pending -> Rejected hợp lệ.");
Assert(OrderStatus.Pending.CanTransitionTo(OrderStatus.Cancelled), "Pending -> Cancelled hợp lệ.");
Assert(OrderStatus.Pending.CanTransitionTo(OrderStatus.Expired), "Pending -> Expired hợp lệ.");
Assert(!OrderStatus.Pending.CanTransitionTo(OrderStatus.Preparing), "Pending không nhảy thẳng Preparing.");
Assert(!OrderStatus.Pending.CanTransitionTo(OrderStatus.Ready), "Pending không nhảy vượt cấp Ready.");

Assert(OrderStatus.Confirmed.CanTransitionTo(OrderStatus.Preparing), "Confirmed -> Preparing.");
Assert(!OrderStatus.Confirmed.CanTransitionTo(OrderStatus.Pending), "Không lùi Confirmed -> Pending.");
Assert(OrderStatus.Preparing.CanTransitionTo(OrderStatus.Ready), "Preparing -> Ready.");

Assert(OrderStatus.Ready.CanTransitionTo(OrderStatus.Delivering), "Ready -> Delivering (đơn giao).");
Assert(OrderStatus.Ready.CanTransitionTo(OrderStatus.Completed), "Ready -> Completed (đơn Pickup).");
Assert(OrderStatus.Ready.CanTransitionTo(OrderStatus.NoShow), "Ready -> NoShow.");
Assert(OrderStatus.Delivering.CanTransitionTo(OrderStatus.Completed), "Delivering -> Completed.");
Assert(OrderStatus.Delivering.CanTransitionTo(OrderStatus.DeliveryFailed), "Delivering -> DeliveryFailed.");

foreach (var terminal in new[]
{
    OrderStatus.Completed,
    OrderStatus.Cancelled,
    OrderStatus.Rejected,
    OrderStatus.Expired,
    OrderStatus.DeliveryFailed,
    OrderStatus.NoShow
})
{
    Assert(terminal.IsTerminal(), $"{terminal} phải là trạng thái kết thúc.");
    foreach (var next in Enum.GetValues<OrderStatus>())
    {
        Assert(!terminal.CanTransitionTo(next), $"{terminal} là terminal, không được chuyển sang {next}.");
    }
}

Assert(OrderStatus.Rejected.RequiresReason(), "Rejected bắt buộc lý do.");
Assert(OrderStatus.Cancelled.RequiresReason(), "Cancelled bắt buộc lý do.");
Assert(OrderStatus.DeliveryFailed.RequiresReason(), "DeliveryFailed bắt buộc lý do.");
Assert(OrderStatus.NoShow.RequiresReason(), "NoShow bắt buộc lý do.");
Assert(!OrderStatus.Confirmed.RequiresReason(), "Confirmed không cần lý do.");
Assert(!OrderStatus.Completed.RequiresReason(), "Completed không cần lý do.");

Assert(OrderStatus.Pending.CanBeCancelledByCustomer(), "Khách hủy được đơn Pending.");
Assert(!OrderStatus.Confirmed.CanBeCancelledByCustomer(), "Khách không hủy được sau khi quán xác nhận.");

Assert(OrderStatus.Ready.CanTransitionTo(OrderStatus.Delivering, OrderType.Delivery), "Đơn giao: Ready -> Delivering.");
Assert(!OrderStatus.Ready.CanTransitionTo(OrderStatus.Completed, OrderType.Delivery), "Đơn giao không nhảy thẳng Ready -> Completed.");
Assert(OrderStatus.Delivering.CanTransitionTo(OrderStatus.Completed, OrderType.Delivery), "Đơn giao: Delivering -> Completed.");
Assert(OrderStatus.Ready.CanTransitionTo(OrderStatus.Completed, OrderType.Pickup), "Đơn Pickup: Ready -> Completed.");
Assert(!OrderStatus.Ready.CanTransitionTo(OrderStatus.Delivering, OrderType.Pickup), "Đơn Pickup không có bước Delivering.");
Assert(OrderStatus.Ready.CanTransitionTo(OrderStatus.NoShow, OrderType.Pickup), "Đơn Pickup: Ready -> NoShow.");

Console.WriteLine("Order state machine v2 tests passed.");

const string codeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
var sampleCode = OrderCodeGenerator.Generate();
Assert(sampleCode.StartsWith("QB-"), "Mã tra cứu phải bắt đầu bằng QB-.");
Assert(sampleCode.Length == 9, "Mã tra cứu mặc định dài 9 ký tự (QB- + 6).");
Assert(sampleCode[3..].All(character => codeAlphabet.Contains(character)),
    "Mã chỉ dùng ký tự không nhập nhằng (bỏ I, O, 0, 1).");

var generatedCodes = new HashSet<string>();
for (var index = 0; index < 500; index++)
{
    generatedCodes.Add(OrderCodeGenerator.Generate());
}
Assert(generatedCodes.Count > 450, "Mã tra cứu phải đủ ngẫu nhiên (ít trùng).");

Console.WriteLine("Order code generator tests passed.");
