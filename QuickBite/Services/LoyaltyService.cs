using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed record RedeemOption(string Key, int Points, int Percent, decimal MaxDiscount);

public sealed class LoyaltyService
{
    public const int PointUnit = 10_000;
    private const int VoucherLifetimeDays = 30;
    private const string CodeAlphabet = "ACDEFGHJKLMNPQRTUVWXY34679";

    public static readonly IReadOnlyList<RedeemOption> RedeemOptions =
    [
        new("t50", 50, 5, 15_000m),
        new("t100", 100, 10, 30_000m),
        new("t200", 200, 15, 50_000m)
    ];

    private readonly AppDbContext _db;

    public LoyaltyService(AppDbContext db)
    {
        _db = db;
    }

    public static int PointsFor(decimal amount) => (int)(amount / PointUnit);

    public Task<int> GetBalanceAsync(int customerId, CancellationToken cancellationToken = default)
        => _db.PointLedgers
            .Where(p => p.CustomerId == customerId)
            .SumAsync(p => p.Points, cancellationToken);

    public async Task<IReadOnlyList<PointLedger>> GetLedgerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
        => await _db.PointLedgers
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Voucher>> GetVouchersAsync(
        int customerId,
        CancellationToken cancellationToken = default)
        => await _db.Vouchers
            .AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AccrueAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.CustomerId is null)
        {
            return;
        }

        var points = PointsFor(order.Total);
        if (points <= 0)
        {
            return;
        }

        var alreadyEarned = await _db.PointLedgers.AnyAsync(
            p => p.OrderId == order.Id && p.Type == PointEntryType.Earn,
            cancellationToken);
        if (alreadyEarned)
        {
            return;
        }

        _db.PointLedgers.Add(new PointLedger
        {
            CustomerId = order.CustomerId.Value,
            Points = points,
            Type = PointEntryType.Earn,
            OrderId = order.Id,
            Note = $"Đơn #{order.Id} hoàn tất",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Voucher> RedeemAsync(
        int customerId,
        string optionKey,
        CancellationToken cancellationToken = default)
    {
        var option = RedeemOptions.SingleOrDefault(o => o.Key == optionKey)
            ?? throw new CustomerFlowException("Mức đổi điểm không hợp lệ.");

        var balance = await GetBalanceAsync(customerId, cancellationToken);
        if (balance < option.Points)
        {
            throw new CustomerFlowException(
                $"Bạn cần {option.Points} điểm để đổi mức này (hiện có {balance} điểm).");
        }

        var voucher = new Voucher
        {
            Code = await GenerateCodeAsync(cancellationToken),
            CustomerId = customerId,
            DiscountPercent = option.Percent,
            MaxDiscountAmount = option.MaxDiscount,
            PointsSpent = option.Points,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddDays(VoucherLifetimeDays)
        };
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(cancellationToken);

        _db.PointLedgers.Add(new PointLedger
        {
            CustomerId = customerId,
            Points = -option.Points,
            Type = PointEntryType.Redeem,
            VoucherId = voucher.Id,
            Note = $"Đổi voucher {voucher.Code}",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync(cancellationToken);

        return voucher;
    }

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var chars = new char[6];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)];
            }

            var code = $"QV-{new string(chars)}";
            if (!await _db.Vouchers.AnyAsync(v => v.Code == code, cancellationToken))
            {
                return code;
            }
        }
    }
}
