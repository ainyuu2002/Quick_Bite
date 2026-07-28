using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Modules.Operations.Authorization;
using QuickBite.Modules.Operations.Ingredients;

namespace QuickBite.Pages.Admin.Ingredients;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IIngredientService _ingredientService;

    public IndexModel(AppDbContext db, IIngredientService ingredientService)
    {
        _db = db;
        _ingredientService = ingredientService;
    }

    public sealed record LogRow(
        string IngredientName,
        IngredientLogAction Action,
        IngredientStatus PreviousStatus,
        IngredientStatus NewStatus,
        string? Note,
        DateTime CreatedAt,
        string ActorName);

    public sealed class CreateInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên nguyên liệu.")]
        [StringLength(120, ErrorMessage = "Tên nguyên liệu tối đa 120 ký tự.")]
        public string Name { get; set; } = string.Empty;
    }

    public sealed class LinkInput
    {
        [Range(1, int.MaxValue)]
        public int IngredientId { get; set; }

        public int[] MenuItemIds { get; set; } = Array.Empty<int>();
    }

    [BindProperty]
    public CreateInput Create { get; set; } = new();

    [BindProperty]
    public LinkInput Links { get; set; } = new();

    public IReadOnlyList<Ingredient> Ingredients { get; private set; }
        = Array.Empty<Ingredient>();

    public IReadOnlyList<MenuItem> MenuItems { get; private set; }
        = Array.Empty<MenuItem>();

    public IReadOnlyList<LogRow> RecentLogs { get; private set; }
        = Array.Empty<LogRow>();

    public bool CanConfigure => User.IsInRole(InternalRoles.Manager);

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
        => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostCreateAsync(
        CancellationToken cancellationToken = default)
    {
        if (!CanConfigure)
        {
            return Forbid();
        }

        ModelState.Remove($"{nameof(Links)}.{nameof(LinkInput.IngredientId)}");
        ModelState.Remove($"{nameof(Links)}.{nameof(LinkInput.MenuItemIds)}");
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        return await ExecuteAsync(
            () => _ingredientService.CreateAsync(
                Create.Name,
                GetCurrentAccountId(),
                cancellationToken),
            "Đã thêm nguyên liệu.");
    }

    public async Task<IActionResult> OnPostUpdateLinksAsync(
        CancellationToken cancellationToken = default)
    {
        if (!CanConfigure)
        {
            return Forbid();
        }

        return await ExecuteAsync(
            () => _ingredientService.UpdateDishLinksAsync(
                Links.IngredientId,
                Links.MenuItemIds,
                GetCurrentAccountId(),
                cancellationToken),
            "Đã cập nhật các món sử dụng nguyên liệu.");
    }

    public async Task<IActionResult> OnPostSetStatusAsync(
        int ingredientId,
        IngredientStatus status,
        CancellationToken cancellationToken = default)
        => await ExecuteAsync(
            () => _ingredientService.SetStatusAsync(
                ingredientId,
                status,
                GetCurrentAccountId(),
                cancellationToken),
            $"Đã chuyển trạng thái sang {status.ToDisplayText()}.");

    public async Task<IActionResult> OnPostRestockedAsync(
        int ingredientId,
        string? note,
        CancellationToken cancellationToken = default)
        => await ExecuteAsync(
            () => _ingredientService.MarkRestockedAsync(
                ingredientId,
                note,
                GetCurrentAccountId(),
                cancellationToken),
            "Đã ghi nhận nhập nguyên liệu.");

    private async Task<IActionResult> ExecuteAsync(
        Func<Task> action,
        string successMessage)
    {
        try
        {
            await action();
            TempData["SuccessMessage"] = successMessage;
        }
        catch (IngredientOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Ingredients = await _db.Ingredients
            .AsNoTracking()
            .Include(item => item.Dishes)
            .ThenInclude(link => link.MenuItem)
            .OrderByDescending(item => item.Status)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        MenuItems = await _db.MenuItems
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        RecentLogs = await (
            from log in _db.RestockLogs.AsNoTracking()
            join ingredient in _db.Ingredients.AsNoTracking()
                on log.IngredientId equals ingredient.Id
            join account in _db.Accounts.AsNoTracking()
                on log.ActorAccountId equals account.Id
            orderby log.CreatedAt descending
            select new LogRow(
                ingredient.Name,
                log.Action,
                log.PreviousStatus,
                log.NewStatus,
                log.Note,
                log.CreatedAt,
                account.FullName ?? account.Username))
            .Take(30)
            .ToListAsync(cancellationToken);
    }

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Không xác định được tài khoản hiện tại.");
}
