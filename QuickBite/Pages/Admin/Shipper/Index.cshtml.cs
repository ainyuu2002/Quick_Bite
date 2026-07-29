using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Admin.Shipper;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly OrderService _orderService;

    public IndexModel(AppDbContext context, OrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public IReadOnlyList<Order> Board { get; private set; } = [];

    public IReadOnlyList<ReasonCatalog> FailReasons { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Board = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.MenuItem)
            .Where(order => order.Status == OrderStatus.Delivering
                || (order.Status == OrderStatus.Ready && order.OrderType == OrderType.Delivery))
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        FailReasons = await _context.ReasonCatalogs
            .AsNoTracking()
            .Where(reason => reason.IsActive && reason.Kind == ReasonKind.DeliveryFailed)
            .OrderBy(reason => reason.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAdvanceAsync(
        int orderId,
        OrderStatus nextStatus,
        string? reason,
        string? reasonOther,
        CancellationToken cancellationToken)
    {
        if (nextStatus is not (OrderStatus.Delivering or OrderStatus.Completed or OrderStatus.DeliveryFailed))
        {
            TempData["ErrorMessage"] = "Thao tác không hợp lệ cho shipper.";
            return RedirectToPage();
        }

        var actorAccountId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId)
            ? accountId
            : (int?)null;

        var effectiveReason = reason == "__other__" ? reasonOther : reason;

        try
        {
            await _orderService.ChangeStatusAsync(orderId, nextStatus, actorAccountId, effectiveReason, cancellationToken);
            TempData["SuccessMessage"] = $"Đơn #{orderId} đã cập nhật.";
        }
        catch (Exception exception) when (exception is OrderValidationException or KeyNotFoundException)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToPage();
    }
}
