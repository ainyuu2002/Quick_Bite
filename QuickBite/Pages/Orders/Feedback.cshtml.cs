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
    private readonly CustomerAccountService _accounts;

    public FeedbackModel(
        AppDbContext db,
        ComplaintService complaints,
        CustomerAccountService accounts)
    {
        _db = db;
        _complaints = complaints;
        _accounts = accounts;
    }

    public Order Order { get; private set; } = null!;

    public InternalRating? ExistingRating { get; private set; }

    public Complaint? ExistingComplaint { get; private set; }

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int orderId, CancellationToken cancellationToken)
    {
        return await LoadAsync(orderId, cancellationToken)
            ? Page()
            : RedirectToPage("/Orders/Track");
    }

    public async Task<IActionResult> OnPostRateAsync(
        int orderId,
        int stars,
        CancellationToken cancellationToken)
    {
        var phone = await ResolvePhoneAsync();
        if (phone is null)
        {
            return RedirectToPage("/Orders/Track");
        }

        try
        {
            await _complaints.RateAsync(orderId, phone, stars, cancellationToken);
            Message = "Cảm ơn bạn đã đánh giá!";
        }
        catch (CustomerFlowException exception)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage(new { orderId });
    }

    public async Task<IActionResult> OnPostComplainAsync(
        int orderId,
        ComplaintCategory category,
        string description,
        CancellationToken cancellationToken)
    {
        var phone = await ResolvePhoneAsync();
        if (phone is null)
        {
            return RedirectToPage("/Orders/Track");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ErrorMessage = "Vui lòng mô tả vấn đề bạn gặp phải.";
            return RedirectToPage(new { orderId });
        }

        try
        {
            await _complaints.SubmitAsync(orderId, phone, category, description, cancellationToken);
            Message = "Đã gửi phản ánh. Quán sẽ liên hệ lại với bạn sớm nhất.";
        }
        catch (CustomerFlowException exception)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage(new { orderId });
    }

    private async Task<bool> LoadAsync(int orderId, CancellationToken cancellationToken)
    {
        var phone = await ResolvePhoneAsync();
        if (phone is null)
        {
            return false;
        }

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .SingleOrDefaultAsync(
                o => o.Id == orderId && o.Phone == phone,
                cancellationToken);
        if (order is null)
        {
            return false;
        }

        Order = order;
        ExistingRating = await _db.InternalRatings
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
        ExistingComplaint = await _db.Complaints
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.OrderId == orderId, cancellationToken);
        return true;
    }

    private async Task<string?> ResolvePhoneAsync()
    {
        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        if (customerId is not null)
        {
            var customer = await _accounts.GetAsync(customerId.Value);
            if (customer is not null)
            {
                return customer.Phone;
            }
        }

        return OrderTrackingSession.GetTrackedPhone(HttpContext.Session);
    }
}
