using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed class OtpService
{
    private const int CodeLifetimeMinutes = 5;
    private const int ResendCooldownSeconds = 60;
    private const int MaxFailedAttempts = 5;
    private readonly AppDbContext _db;

    public OtpService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OtpVerification> IssueAsync(
        string phone,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        var now = DateTime.Now;

        var latest = await _db.OtpVerifications
            .Where(o => o.Phone == normalizedPhone && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null)
        {
            var elapsed = now - latest.CreatedAt;
            if (elapsed.TotalSeconds < ResendCooldownSeconds)
            {
                var wait = ResendCooldownSeconds - (int)elapsed.TotalSeconds;
                throw new CustomerFlowException(
                    $"Vui lòng chờ {wait} giây trước khi gửi lại mã xác thực.");
            }
        }

        var otp = new OtpVerification
        {
            Phone = normalizedPhone,
            Code = Random.Shared.Next(0, 1_000_000).ToString("D6"),
            Purpose = purpose,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(CodeLifetimeMinutes)
        };

        _db.OtpVerifications.Add(otp);
        await _db.SaveChangesAsync(cancellationToken);
        return otp;
    }

    public async Task VerifyAsync(
        string phone,
        OtpPurpose purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        var now = DateTime.Now;

        var otp = await _db.OtpVerifications
            .Where(o => o.Phone == normalizedPhone
                && o.Purpose == purpose
                && o.ConsumedAt == null
                && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new CustomerFlowException(
                "Mã xác thực đã hết hạn hoặc chưa được gửi. Vui lòng gửi lại mã.");

        if (otp.Code != code.Trim())
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= MaxFailedAttempts)
            {
                otp.ConsumedAt = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new CustomerFlowException(
                otp.ConsumedAt is null
                    ? "Mã xác thực không đúng. Vui lòng thử lại."
                    : "Nhập sai quá nhiều lần. Vui lòng gửi lại mã mới.");
        }

        otp.ConsumedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
