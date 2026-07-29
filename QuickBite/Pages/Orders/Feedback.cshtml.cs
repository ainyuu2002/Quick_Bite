using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Orders;

public sealed class FeedbackModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ComplaintService _complaints;

    public FeedbackModel(
        AppDbContext db,
        ComplaintService complaints)
    {
        _db = db;
        _complaints = complaints;
    }

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    public Order Order { get; private set; } = null!;

    public InternalRating? ExistingRating { get; private set; }

    public Complaint? ExistingComplaint { get; private set; }

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(Code, cancellationToken)
            ? Page()
            : RedirectToPage("/Orders/Track");
    }

    public async Task<IActionResult> OnPostRateAsync(
        int stars,
        CancellationToken cancellationToken)
    {
        if (!await LoadAsync(Code, cancellationToken))
        {
            return RedirectToPage("/Orders/Track");
        }

        try
        {
            await _complaints.RateAsync(
                Order.Id,
                Order.Phone,
                stars,
                cancellationToken);
            Message = "Cảm ơn bạn đã đánh giá!";
        }
        catch (CustomerFlowException exception)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage(new { code = Code });
    }

    public async Task<IActionResult> OnPostComplainAsync(
        ComplaintCategory category,
        string description,
        CancellationToken cancellationToken)
    {
        if (!await LoadAsync(Code, cancellationToken))
        {
            return RedirectToPage("/Orders/Track");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ErrorMessage = "Vui lòng mô tả vấn đề bạn gặp phải.";
            return RedirectToPage(new { code = Code });
        }

        try
        {
            await _complaints.SubmitAsync(
                Order.Id,
                Order.Phone,
                category,
                description,
                cancellationToken);
            Message = "Đã gửi phản ánh. Quán sẽ liên hệ lại với bạn sớm nhất.";
        }
        catch (CustomerFlowException exception)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage(new { code = Code });
    }

    private async Task<bool> LoadAsync(
        string? code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var order = await _db.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.OrderCode == normalizedCode,
                cancellationToken);
        if (order is null)
        {
            return false;
        }

        Code = order.OrderCode;
        Order = order;
        ExistingRating = await _db.InternalRatings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                rating => rating.OrderId == order.Id,
                cancellationToken);
        ExistingComplaint = await _db.Complaints
            .AsNoTracking()
            .SingleOrDefaultAsync(
                complaint => complaint.OrderId == order.Id,
                cancellationToken);
        return true;
    }
}
