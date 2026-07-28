using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed record DiscountQuote(
    decimal DiscountAmount,
    Promotion? Promotion,
    Voucher? Voucher)
{
    public static readonly DiscountQuote None = new(0m, null, null);
}

public interface IDiscountService
{
    Task<DiscountQuote> QuoteAsync(
        string phone,
        int? customerId,
        decimal itemsTotal,
        string? promotionCode,
        string? voucherCode,
        CancellationToken cancellationToken = default);

    Task CommitAsync(Order order, DiscountQuote quote, CancellationToken cancellationToken = default);

    Task RefundAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class DiscountService : IDiscountService
{
    private readonly AppDbContext _db;

    public DiscountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DiscountQuote> QuoteAsync(
        string phone,
        int? customerId,
        decimal itemsTotal,
        string? promotionCode,
        string? voucherCode,
        CancellationToken cancellationToken = default)
    {
        var hasPromotion = !string.IsNullOrWhiteSpace(promotionCode);
        var hasVoucher = !string.IsNullOrWhiteSpace(voucherCode);

        if (!hasPromotion && !hasVoucher)
        {
            return DiscountQuote.None;
        }

        if (hasPromotion && hasVoucher)
        {
            throw new OrderValidationException(
                "Mỗi đơn chỉ áp dụng được một mã: mã khuyến mãi hoặc voucher.");
        }

        return hasPromotion
            ? await QuotePromotionAsync(phone, itemsTotal, promotionCode!, cancellationToken)
            : await QuoteVoucherAsync(customerId, itemsTotal, voucherCode!, cancellationToken);
    }

    public async Task CommitAsync(
        Order order,
        DiscountQuote quote,
        CancellationToken cancellationToken = default)
    {
        if (quote.Promotion is not null)
        {
            _db.PromotionUsages.Add(new PromotionUsage
            {
                PromotionId = quote.Promotion.Id,
                Phone = order.Phone,
                OrderId = order.Id,
                UsedAt = DateTime.Now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (quote.Voucher is not null)
        {
            quote.Voucher.UsedAt = DateTime.Now;
            quote.Voucher.UsedOrderId = order.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RefundAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.PromotionId is not null)
        {
            var usage = await _db.PromotionUsages
                .SingleOrDefaultAsync(
                    u => u.OrderId == order.Id && u.RefundedAt == null,
                    cancellationToken);
            if (usage is not null)
            {
                usage.RefundedAt = DateTime.Now;
            }
        }

        if (order.VoucherId is not null)
        {
            var voucher = await _db.Vouchers
                .SingleOrDefaultAsync(
                    v => v.Id == order.VoucherId && v.UsedOrderId == order.Id,
                    cancellationToken);
            if (voucher is not null)
            {
                voucher.UsedAt = null;
                voucher.UsedOrderId = null;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<DiscountQuote> QuotePromotionAsync(
        string phone,
        decimal itemsTotal,
        string promotionCode,
        CancellationToken cancellationToken)
    {
        var normalizedCode = promotionCode.Trim().ToUpperInvariant();
        var normalizedPhone = phone.Trim();
        var now = DateTime.Now;

        var promotion = await _db.Promotions
            .SingleOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken)
            ?? throw new OrderValidationException("Mã khuyến mãi không tồn tại.");

        if (!promotion.IsActive || now < promotion.StartsAt || now > promotion.EndsAt)
        {
            throw new OrderValidationException(
                "Mã khuyến mãi đã hết hiệu lực hoặc chưa bắt đầu.");
        }

        if (itemsTotal < promotion.MinOrderTotal)
        {
            throw new OrderValidationException(
                $"Mã này chỉ áp dụng cho đơn từ {promotion.MinOrderTotal:N0} đ.");
        }

        if (promotion.TotalUsageLimit is not null)
        {
            var totalUsed = await _db.PromotionUsages
                .CountAsync(
                    u => u.PromotionId == promotion.Id && u.RefundedAt == null,
                    cancellationToken);
            if (totalUsed >= promotion.TotalUsageLimit)
            {
                throw new OrderValidationException("Mã khuyến mãi đã hết lượt sử dụng.");
            }
        }

        var usedByPhone = await _db.PromotionUsages
            .CountAsync(
                u => u.PromotionId == promotion.Id
                    && u.Phone == normalizedPhone
                    && u.RefundedAt == null,
                cancellationToken);
        if (usedByPhone >= promotion.PerPhoneLimit)
        {
            throw new OrderValidationException(
                "Số điện thoại này đã dùng hết lượt của mã khuyến mãi.");
        }

        var discount = promotion.DiscountType == DiscountType.Percent
            ? Math.Floor(itemsTotal * promotion.DiscountValue / 100m)
            : promotion.DiscountValue;

        if (promotion.DiscountType == DiscountType.Percent
            && promotion.MaxDiscountAmount is not null)
        {
            discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);
        }

        discount = Math.Min(discount, itemsTotal);
        return new DiscountQuote(discount, promotion, null);
    }

    private async Task<DiscountQuote> QuoteVoucherAsync(
        int? customerId,
        decimal itemsTotal,
        string voucherCode,
        CancellationToken cancellationToken)
    {
        if (customerId is null)
        {
            throw new OrderValidationException(
                "Vui lòng đăng nhập tài khoản thành viên để dùng voucher.");
        }

        var normalizedCode = voucherCode.Trim().ToUpperInvariant();
        var voucher = await _db.Vouchers
            .SingleOrDefaultAsync(
                v => v.Code == normalizedCode && v.CustomerId == customerId,
                cancellationToken)
            ?? throw new OrderValidationException("Voucher không tồn tại hoặc không thuộc tài khoản này.");

        if (voucher.UsedAt is not null)
        {
            throw new OrderValidationException("Voucher này đã được sử dụng.");
        }

        if (voucher.ExpiresAt < DateTime.Now)
        {
            throw new OrderValidationException("Voucher đã hết hạn.");
        }

        var discount = Math.Floor(itemsTotal * voucher.DiscountPercent / 100m);
        discount = Math.Min(discount, voucher.MaxDiscountAmount);
        discount = Math.Min(discount, itemsTotal);
        return new DiscountQuote(discount, null, voucher);
    }
}
