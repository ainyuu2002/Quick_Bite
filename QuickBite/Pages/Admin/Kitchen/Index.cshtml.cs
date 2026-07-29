using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Admin.Kitchen;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly OrderService _orderService;

    public IndexModel(AppDbContext context, OrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    public IReadOnlyList<Order> Queue { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Queue = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.MenuItem)
            .Where(order => order.Status == OrderStatus.Confirmed
                || order.Status == OrderStatus.Preparing)
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAdvanceAsync(
        int orderId,
        OrderStatus nextStatus,
        CancellationToken cancellationToken)
    {
        if (nextStatus is not (OrderStatus.Preparing or OrderStatus.Ready))
        {
            TempData["ErrorMessage"] = "Thao tác không hợp lệ cho bếp.";
            return RedirectToPage();
        }

        var actorAccountId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId)
            ? accountId
            : (int?)null;

        try
        {
            await _orderService.ChangeStatusAsync(orderId, nextStatus, actorAccountId, null, cancellationToken);
            TempData["SuccessMessage"] = $"Đơn #{orderId} đã cập nhật.";
        }
        catch (Exception exception) when (exception is OrderValidationException or KeyNotFoundException)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToPage();
    }
}
