using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Services;

namespace QuickBite.Pages.Staff
{
    public class BoardModel : PageModel
    {
        private readonly ConnectionTracker _tracker;

        public BoardModel(ConnectionTracker tracker)
        {
            _tracker = tracker;
        }
        public int StaffOnline => _tracker.StaffOnline;

        public void OnGet()
        {
        }
    }
}
