using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Complaints;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Complaint> Complaints { get; private set; } = [];

    public string StatusFilter { get; private set; } = "open";

    public int RatingCount { get; private set; }

    public double RatingAverage { get; private set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? status, CancellationToken cancellationToken)
    {
        StatusFilter = status ?? "open";

        var query = _db.Complaints
            .AsNoTracking()
            .Include(c => c.Order)
            .AsQueryable();

        query = StatusFilter switch
        {
            "open" => query.Where(c => c.Status != ComplaintStatus.Resolved),
            "resolved" => query.Where(c => c.Status == ComplaintStatus.Resolved),
            _ => query
        };

        Complaints = await query
            .OrderBy(c => c.Status)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var since = DateTime.Now.AddDays(-30);
        var ratings = await _db.InternalRatings
            .AsNoTracking()
            .Where(r => r.CreatedAt >= since)
            .Select(r => r.Stars)
            .ToListAsync(cancellationToken);
        RatingCount = ratings.Count;
        RatingAverage = ratings.Count == 0 ? 0 : ratings.Average();
    }

    public async Task<IActionResult> OnPostStartAsync(int id, CancellationToken cancellationToken)
    {
        var complaint = await _db.Complaints
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (complaint is not null && complaint.Status == ComplaintStatus.New)
        {
            complaint.Status = ComplaintStatus.InProgress;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(
        int id,
        string resolution,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resolution))
        {
            ErrorMessage = "Vui lòng nhập nội dung phản hồi cho khách trước khi đóng phản ánh.";
            return RedirectToPage();
        }

        var complaint = await _db.Complaints
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (complaint is not null && complaint.Status != ComplaintStatus.Resolved)
        {
            complaint.Status = ComplaintStatus.Resolved;
            complaint.Resolution = resolution.Trim();
            complaint.ResolvedAt = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage();
    }
}
