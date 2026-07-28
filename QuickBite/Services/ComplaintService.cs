using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed class ComplaintService
{
    private const int FeedbackWindowDays = 7;
    private readonly AppDbContext _db;
    private readonly IHubContext<OrderHub> _hub;

    public ComplaintService(AppDbContext db, IHubContext<OrderHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<Complaint> SubmitAsync(
        int orderId,
        string phone,
        ComplaintCategory category,
        string description,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadEligibleOrderAsync(orderId, phone, cancellationToken);

        var exists = await _db.Complaints
            .AnyAsync(c => c.OrderId == order.Id, cancellationToken);
        if (exists)
        {
            throw new CustomerFlowException("Đơn này đã có phản ánh. Mỗi đơn chỉ gửi được một phản ánh.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new CustomerFlowException("Danh mục phản ánh không hợp lệ.");
        }

        var complaint = new Complaint
        {
            OrderId = order.Id,
            Phone = order.Phone,
            Category = category,
            Description = description.Trim(),
            Status = ComplaintStatus.New,
            CreatedAt = DateTime.Now
        };
        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Group("staff").SendAsync("NewComplaint", new
        {
            complaintId = complaint.Id,
            orderId = complaint.OrderId,
            category = complaint.Category.ToDisplayText(),
            createdAt = complaint.CreatedAt
        }, cancellationToken);

        return complaint;
    }

    public async Task RateAsync(
        int orderId,
        string phone,
        int stars,
        CancellationToken cancellationToken = default)
    {
        if (stars is < 1 or > 5)
        {
            throw new CustomerFlowException("Số sao phải từ 1 đến 5.");
        }

        var order = await LoadEligibleOrderAsync(orderId, phone, cancellationToken);

        var exists = await _db.InternalRatings
            .AnyAsync(r => r.OrderId == order.Id, cancellationToken);
        if (exists)
        {
            throw new CustomerFlowException("Đơn này đã được chấm điểm.");
        }

        _db.InternalRatings.Add(new InternalRating
        {
            OrderId = order.Id,
            Phone = order.Phone,
            Stars = stars,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Order> LoadEligibleOrderAsync(
        int orderId,
        string phone,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = phone.Trim();
        var order = await _db.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                o => o.Id == orderId && o.Phone == normalizedPhone,
                cancellationToken)
            ?? throw new CustomerFlowException("Không tìm thấy đơn hàng.");

        if (order.Status != OrderStatus.Completed)
        {
            throw new CustomerFlowException("Chỉ gửi được phản hồi cho đơn đã hoàn tất.");
        }

        if (order.CreatedAt < DateTime.Now.AddDays(-FeedbackWindowDays))
        {
            throw new CustomerFlowException(
                $"Chỉ nhận phản hồi trong vòng {FeedbackWindowDays} ngày kể từ khi đặt đơn.");
        }

        return order;
    }
}
