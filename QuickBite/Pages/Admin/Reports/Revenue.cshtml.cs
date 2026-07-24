using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Reports;

public class RevenueModel : PageModel
{
    private readonly AppDbContext _context;

    public RevenueModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public decimal TotalRevenue { get; private set; }

    public List<Order> Orders { get; private set; } = new();

    public List<DailyRevenue> ByDay { get; private set; } = new();

    public List<ItemRevenue> ByItem { get; private set; } = new();

    public record DailyRevenue(DateTime Day, int Count, decimal Total);

    public record ItemRevenue(string Name, int Quantity, decimal Total);

    public async Task OnGetAsync()
    {
        var from = (From ?? DateTime.Today).Date;
        var to = (To ?? from).Date;
        if (to < from)
        {
            (from, to) = (to, from);

            ModelState.Remove(nameof(From));
            ModelState.Remove(nameof(To));
        }

        var toExclusive = to.AddDays(1);

        Orders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed
                     && o.CreatedAt >= from
                     && o.CreatedAt < toExclusive)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        TotalRevenue = Orders.Sum(o => o.Total);

        ByDay = Orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyRevenue(g.Key, g.Count(), g.Sum(o => o.Total)))
            .ToList();

        var byItemRaw = await _context.OrderItems
            .Where(i => i.Order!.Status == OrderStatus.Completed
                     && i.Order.CreatedAt >= from
                     && i.Order.CreatedAt < toExclusive)
            .GroupBy(i => new { i.MenuItemId, i.MenuItem!.Name })
            .Select(g => new
            {
                g.Key.Name,
                Quantity = g.Sum(i => i.Quantity),
                Total = g.Sum(i => i.Quantity * i.UnitPrice)
            })
            .OrderByDescending(x => x.Quantity)
            .ToListAsync();

        ByItem = byItemRaw
            .Select(x => new ItemRevenue(x.Name, x.Quantity, x.Total))
            .ToList();

        From = from;
        To = to;
    }
}
