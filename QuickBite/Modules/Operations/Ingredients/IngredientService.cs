using System.Data;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;

namespace QuickBite.Modules.Operations.Ingredients;

public sealed class IngredientOperationException : Exception
{
    public IngredientOperationException(string message) : base(message) { }
}

public interface IIngredientService
{
    Task CreateAsync(string name, int actorAccountId, CancellationToken cancellationToken = default);

    Task UpdateDishLinksAsync(
        int ingredientId,
        IReadOnlyCollection<int> menuItemIds,
        int actorAccountId,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(
        int ingredientId,
        IngredientStatus status,
        int actorAccountId,
        CancellationToken cancellationToken = default);

    Task MarkRestockedAsync(
        int ingredientId,
        string? note,
        int actorAccountId,
        CancellationToken cancellationToken = default);
}

public sealed class IngredientService : IIngredientService
{
    private readonly AppDbContext _db;

    public IngredientService(AppDbContext db) => _db = db;

    public async Task CreateAsync(
        string name,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new IngredientOperationException("Tên nguyên liệu không được để trống.");
        }
        if (normalizedName.Length > 120)
        {
            throw new IngredientOperationException("Tên nguyên liệu tối đa 120 ký tự.");
        }

        if (await _db.Ingredients.AnyAsync(
                item => item.Name == normalizedName,
                cancellationToken))
        {
            throw new IngredientOperationException("Nguyên liệu này đã tồn tại.");
        }

        _db.Ingredients.Add(new Ingredient
        {
            Name = normalizedName,
            Status = IngredientStatus.Available,
            UpdatedAt = DateTime.Now,
            UpdatedByAccountId = actorAccountId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDishLinksAsync(
        int ingredientId,
        IReadOnlyCollection<int> menuItemIds,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        var ingredient = await _db.Ingredients
            .Include(item => item.Dishes)
            .SingleOrDefaultAsync(item => item.Id == ingredientId, cancellationToken)
            ?? throw new IngredientOperationException("Không tìm thấy nguyên liệu.");

        if (ingredient.Status == IngredientStatus.OutOfStock)
        {
            throw new IngredientOperationException(
                "Hãy đánh dấu nguyên liệu đã nhập trước khi thay đổi món liên quan.");
        }

        var requestedIds = menuItemIds.Where(id => id > 0).Distinct().ToHashSet();
        var validIds = (await _db.MenuItems
            .Where(item => requestedIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();
        if (validIds.Count != requestedIds.Count)
        {
            throw new IngredientOperationException("Danh sách món không hợp lệ.");
        }

        var removed = ingredient.Dishes
            .Where(link => !requestedIds.Contains(link.MenuItemId))
            .ToArray();
        _db.DishIngredients.RemoveRange(removed);

        var currentIds = ingredient.Dishes.Select(link => link.MenuItemId).ToHashSet();
        foreach (var menuItemId in requestedIds.Except(currentIds))
        {
            ingredient.Dishes.Add(new DishIngredient
            {
                MenuItemId = menuItemId,
                IngredientId = ingredientId
            });
        }

        ingredient.UpdatedAt = DateTime.Now;
        ingredient.UpdatedByAccountId = actorAccountId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SetStatusAsync(
        int ingredientId,
        IngredientStatus status,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new IngredientOperationException("Trạng thái nguyên liệu không hợp lệ.");
        }

        return ChangeStatusAsync(
            ingredientId,
            status,
            IngredientLogAction.StatusChanged,
            null,
            actorAccountId,
            cancellationToken);
    }

    public Task MarkRestockedAsync(
        int ingredientId,
        string? note,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        if (note?.Length > 500)
        {
            throw new IngredientOperationException("Ghi chú tối đa 500 ký tự.");
        }

        return ChangeStatusAsync(
            ingredientId,
            IngredientStatus.Available,
            IngredientLogAction.Restocked,
            note,
            actorAccountId,
            cancellationToken);
    }

    private async Task ChangeStatusAsync(
        int ingredientId,
        IngredientStatus newStatus,
        IngredientLogAction action,
        string? note,
        int actorAccountId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var ingredient = await _db.Ingredients
            .Include(item => item.Dishes)
            .ThenInclude(link => link.MenuItem)
            .SingleOrDefaultAsync(item => item.Id == ingredientId, cancellationToken)
            ?? throw new IngredientOperationException("Không tìm thấy nguyên liệu.");

        var previousStatus = ingredient.Status;
        ingredient.Status = newStatus;
        ingredient.UpdatedAt = DateTime.Now;
        ingredient.UpdatedByAccountId = actorAccountId;

        if (newStatus == IngredientStatus.OutOfStock)
        {
            foreach (var link in ingredient.Dishes)
            {
                if (link.MenuItem.IsAvailable)
                {
                    link.DisabledMenuItem = true;
                    link.MenuItem.IsAvailable = false;
                }
            }
        }
        else if (previousStatus == IngredientStatus.OutOfStock)
        {
            await RestoreEligibleMenuItemsAsync(
                ingredient.Dishes.Select(link => link.MenuItemId).ToArray(),
                cancellationToken);
        }

        _db.RestockLogs.Add(new RestockLog
        {
            IngredientId = ingredient.Id,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = DateTime.Now,
            ActorAccountId = actorAccountId
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RestoreEligibleMenuItemsAsync(
        IReadOnlyCollection<int> menuItemIds,
        CancellationToken cancellationToken)
    {
        if (menuItemIds.Count == 0)
        {
            return;
        }

        var links = await _db.DishIngredients
            .Include(link => link.Ingredient)
            .Include(link => link.MenuItem)
            .Where(link => menuItemIds.Contains(link.MenuItemId))
            .ToListAsync(cancellationToken);

        foreach (var group in links.GroupBy(link => link.MenuItemId))
        {
            if (group.Any(link => link.Ingredient.Status == IngredientStatus.OutOfStock))
            {
                continue;
            }

            if (group.Any(link => link.DisabledMenuItem))
            {
                group.First().MenuItem.IsAvailable = true;
                foreach (var link in group)
                {
                    link.DisabledMenuItem = false;
                }
            }
        }
    }
}
