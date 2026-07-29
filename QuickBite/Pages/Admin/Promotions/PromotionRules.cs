using Microsoft.AspNetCore.Mvc.ModelBinding;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Promotions;

public static class PromotionRules
{
    public static void Validate(Promotion promotion, ModelStateDictionary modelState)
    {
        promotion.Code = promotion.Code.Trim().ToUpperInvariant();

        if (promotion.EndsAt <= promotion.StartsAt)
        {
            modelState.AddModelError("Promotion.EndsAt",
                "Thời điểm kết thúc phải sau thời điểm bắt đầu.");
        }

        if (promotion.DiscountType == DiscountType.Percent)
        {
            if (promotion.DiscountValue is < 1 or > 100)
            {
                modelState.AddModelError("Promotion.DiscountValue",
                    "Mã giảm % phải có giá trị từ 1 đến 100.");
            }

            if (promotion.MaxDiscountAmount is null)
            {
                modelState.AddModelError("Promotion.MaxDiscountAmount",
                    "Mã giảm % bắt buộc phải có trần tiền giảm tối đa.");
            }
        }
    }
}
