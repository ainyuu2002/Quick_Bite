using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;
using System.Security.Claims;

namespace QuickBite.Pages.Admin.Orders;

public class IndexModel : PageModel
{
    private const int PageSize = 10;

    private readonly AppDbContext _context;
    private readonly OrderService _orderService;
    private readonly ConnectionTracker _connectionTracker;

    public IndexModel(
        AppDbContext context,
        OrderService orderService,
        ConnectionTracker connectionTracker)
    {
        _context = context;
        _orderService = orderService;
        _connectionTracker = connectionTracker;
    }

    public int StaffOnline => _connectionTracker.StaffOnline;

    public PagedResult<Order> Result { get; set; } = default!;

    public OrderStatus CurrentStatus { get; private set; }

    public string? Search { get; private set; }

    public string SortBy { get; private set; } = "date";

    public string SortDir { get; private set; } = "desc";

    public IReadOnlyList<ReasonCatalog> Reasons { get; private set; } = [];

    public static IReadOnlyList<OrderStatus> AdminNextStatuses(OrderStatus current, OrderType orderType)
        => Enum.GetValues<OrderStatus>()
            .Where(next => current.CanTransitionTo(next, orderType)
                && next != OrderStatus.Expired
                && next != OrderStatus.Cancelled)
            .ToList();

    public IReadOnlyList<ReasonCatalog> ReasonsFor(OrderStatus status)
    {
        var kind = ReasonKindExtensions.ForStatus(status);
        return kind is null
            ? []
            : Reasons.Where(reason => reason.Kind == kind).ToList();
    }

    public async Task OnGetAsync(
        OrderStatus status = OrderStatus.Pending,
        string? search = null,
        string sortBy = "date",
        string sortDir = "desc",
        int pageNumber = 1)
    {
        CurrentStatus = status;
        Search = search;
        SortBy = sortBy == "total" ? "total" : "date";
        SortDir = sortDir == "asc" ? "asc" : "desc";

        Reasons = await _context.ReasonCatalogs
            .AsNoTracking()
            .Where(reason => reason.IsActive)
            .OrderBy(reason => reason.Kind)
            .ThenBy(reason => reason.DisplayOrder)
            .ToListAsync();

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(item => item.MenuItem)
            .Include(o => o.AcceptedByAccount)
            .Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(o =>
                o.CustomerName.Contains(keyword) ||
                o.Phone.Contains(keyword) ||
                o.Id.ToString() == keyword);
        }

        query = (SortBy, SortDir) switch
        {
            ("total", "asc") => query.OrderBy(o => o.Total),
            ("total", "desc") => query.OrderByDescending(o => o.Total),
            ("date", "asc") => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        Result = await PagedResult<Order>.CreateAsync(query, pageNumber, PageSize);
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(
        int orderId,
        OrderStatus nextStatus,
        string? reason = null,
        OrderStatus currentStatus = OrderStatus.Pending,
        string? search = null,
        string sortBy = "date",
        string sortDir = "desc",
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        currentStatus = Enum.IsDefined(currentStatus)
            ? currentStatus
            : OrderStatus.Pending;
        sortBy = sortBy == "total" ? "total" : "date";
        sortDir = sortDir == "asc" ? "asc" : "desc";
        pageNumber = Math.Max(1, pageNumber);

        try
        {
            var actorAccountId = int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId)
                ? accountId
                : (int?)null;

            var order = await _orderService.ChangeStatusAsync(
                orderId,
                nextStatus,
                actorAccountId,
                reason,
                cancellationToken);

            TempData["SuccessMessage"] =
                $"Đơn #{order.Id} đã chuyển sang {order.Status.ToDisplayText()}.";

            return RedirectToPage(new
            {
                status = order.Status,
                search,
                sortBy,
                sortDir,
                pageNumber = 1
            });
        }
        catch (OrderValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (KeyNotFoundException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToPage(new
        {
            status = currentStatus,
            search,
            sortBy,
            sortDir,
            pageNumber
        });
    }

    public async Task<IActionResult> OnPostUpdateContactAsync(
        int orderId,
        string customerName,
        string phone,
        string? address,
        OrderStatus currentStatus = OrderStatus.Pending,
        string? search = null,
        string sortBy = "date",
        string sortDir = "desc",
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        currentStatus = Enum.IsDefined(currentStatus) ? currentStatus : OrderStatus.Pending;
        sortBy = sortBy == "total" ? "total" : "date";
        sortDir = sortDir == "asc" ? "asc" : "desc";
        pageNumber = Math.Max(1, pageNumber);

        try
        {
            await _orderService.UpdateContactAsync(orderId, customerName, phone, address, cancellationToken);
            TempData["SuccessMessage"] = $"Đã cập nhật thông tin liên hệ đơn #{orderId}.";
        }
        catch (OrderValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (KeyNotFoundException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToPage(new
        {
            status = currentStatus,
            search,
            sortBy,
            sortDir,
            pageNumber
        });
    }
}
