using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Orders;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<Order> Orders { get; set; } = default!;

    public OrderStatus CurrentStatus { get; private set; }

    public async Task OnGetAsync(OrderStatus status = OrderStatus.Pending)
    {
        CurrentStatus = status;

        Orders = await _context.Orders
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}