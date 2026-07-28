using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed class CustomerFlowException : Exception
{
    public CustomerFlowException(string message) : base(message)
    {
    }
}

public sealed record CustomerBadge(int CompletedCount, int CancelledCount)
{
    public bool IsLoyal => CompletedCount >= 5;
}

public sealed class CustomerAccountService
{
    private const int LoyaltyWindowDays = 90;
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Customer> _hasher = new();

    public CustomerAccountService(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> IsPhoneRegisteredAsync(
        string phone,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        return _db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Phone == normalizedPhone, cancellationToken);
    }

    public async Task<Customer> RegisterAsync(
        string phone,
        string fullName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        if (await IsPhoneRegisteredAsync(normalizedPhone, cancellationToken))
        {
            throw new CustomerFlowException(
                "Số điện thoại này đã có tài khoản. Vui lòng đăng nhập.");
        }

        var customer = new Customer
        {
            Phone = normalizedPhone,
            FullName = fullName.Trim(),
            IsPhoneVerified = true,
            CreatedAt = DateTime.Now
        };
        customer.PasswordHash = _hasher.HashPassword(customer, password);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer?> ValidateLoginAsync(
        string phone,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        var customer = await _db.Customers
            .SingleOrDefaultAsync(c => c.Phone == normalizedPhone, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(customer, customer.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : customer;
    }

    public Task<Customer?> GetAsync(int customerId, CancellationToken cancellationToken = default)
        => _db.Customers
            .SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken);

    public async Task UpdateProfileAsync(
        int customerId,
        string fullName,
        string? savedAddress,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new CustomerFlowException("Không tìm thấy tài khoản.");

        customer.FullName = fullName.Trim();
        customer.SavedAddress = string.IsNullOrWhiteSpace(savedAddress)
            ? null
            : savedAddress.Trim();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerBadge> GetBadgeAsync(
        string phone,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        var since = DateTime.Now.AddDays(-LoyaltyWindowDays);

        var counts = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Phone == normalizedPhone && o.CreatedAt >= since)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var completed = counts.SingleOrDefault(c => c.Status == OrderStatus.Completed)?.Count ?? 0;
        var cancelled = counts.SingleOrDefault(c => c.Status == OrderStatus.Cancelled)?.Count ?? 0;
        return new CustomerBadge(completed, cancelled);
    }
}
