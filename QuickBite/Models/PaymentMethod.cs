namespace QuickBite.Models;

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1
}

public static class PaymentMethodExtensions
{
    public static string ToDisplayText(this PaymentMethod paymentMethod) => paymentMethod switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        _ => paymentMethod.ToString()
    };
}
