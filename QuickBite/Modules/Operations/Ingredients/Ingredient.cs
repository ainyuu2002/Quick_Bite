using System.ComponentModel.DataAnnotations;
using QuickBite.Models;

namespace QuickBite.Modules.Operations.Ingredients;

public enum IngredientStatus
{
    Available = 0,
    Low = 1,
    OutOfStock = 2
}

public static class IngredientStatusExtensions
{
    public static string ToDisplayText(this IngredientStatus status) => status switch
    {
        IngredientStatus.Available => "Còn",
        IngredientStatus.Low => "Sắp hết",
        IngredientStatus.OutOfStock => "Hết",
        _ => status.ToString()
    };
}

public sealed class Ingredient
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = null!;

    public IngredientStatus Status { get; set; } = IngredientStatus.Available;

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByAccountId { get; set; }

    public List<DishIngredient> Dishes { get; set; } = new();

    public List<RestockLog> Logs { get; set; } = new();
}

public sealed class DishIngredient
{
    public int IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    public int MenuItemId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;

    /// <summary>
    /// True khi hệ thống tắt món đang bán vì nguyên liệu hết. Cờ này ngăn việc
    /// tự mở lại món vốn đã được Manager ẩn thủ công.
    /// </summary>
    public bool DisabledMenuItem { get; set; }
}

public enum IngredientLogAction
{
    StatusChanged = 0,
    Restocked = 1
}

public sealed class RestockLog
{
    public int Id { get; set; }

    public int IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    public IngredientLogAction Action { get; set; }

    public IngredientStatus PreviousStatus { get; set; }

    public IngredientStatus NewStatus { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ActorAccountId { get; set; }
}
