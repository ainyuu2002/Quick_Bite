namespace QuickBite.Models;

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1
}

public static class PaymentStatusExtensions
{
    public static string ToDisplayText(this PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        _ => status.ToString()
    };
}
