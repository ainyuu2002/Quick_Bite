using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Orders;

public class IndexModel : PageModel
{
    private const int PageSize = 10;

    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public PagedResult<Order> Result { get; set; } = default!;

    public OrderStatus CurrentStatus { get; private set; }

    public string? Search { get; private set; }

    public string SortBy { get; private set; } = "date";

    public string SortDir { get; private set; } = "desc";

    public async Task OnGetAsync(
        OrderStatus status = OrderStatus.Pending,
        string? search = null,
        string sortBy = "date",
        string sortDir = "desc",
        int page = 1)
    {
        CurrentStatus = status;
        Search = search;
        SortBy = sortBy == "total" ? "total" : "date";
        SortDir = sortDir == "asc" ? "asc" : "desc";

        var query = _context.Orders.Where(o => o.Status == status);

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

        Result = await PagedResult<Order>.CreateAsync(query, page, PageSize);
    }
}