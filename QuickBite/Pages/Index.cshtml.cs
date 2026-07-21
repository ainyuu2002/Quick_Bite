using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using QuickBite.Hubs;

namespace QuickBite.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHubContext<OrderHub> _hub;


        public IndexModel(ILogger<IndexModel> logger, IHubContext<OrderHub> hub)
        {
            _logger = logger;
            _hub = hub;
        }

        public void OnGet()
        {

        }
        public async Task<IActionResult> OnPostTestAsync()
        {
            // Payload đúng SDS 3.3 — đóng thế cho OrderService.CreateOrderAsync (Dev C)
            await _hub.Clients.Group("staff").SendAsync("NewOrder", new
            {
                id = 1,
                customerName = "Nguyễn Văn A",
                total = 50000,
                createdAt = DateTime.Now,
                items = new[] { new { name = "Gà rán", quantity = 2 } }
            });
            return RedirectToPage();

        }
    }
}
