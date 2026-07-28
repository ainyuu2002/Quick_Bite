using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Modules.Operations.Reports;
using QuickBite.Modules.Operations.Store;

namespace QuickBite.Pages.Admin.Reports;

public sealed class MenuPerformanceModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IMenuPerformanceService _menuPerformance;

    public MenuPerformanceModel(
        AppDbContext db,
        IMenuPerformanceService menuPerformance)
    {
        _db = db;
        _menuPerformance = menuPerformance;
    }

    public sealed class SettingsInput
    {
        [Range(1, 1_000, ErrorMessage = "Ngưỡng món ế phải từ 1 đến 1.000 phần.")]
        public int SlowItemThreshold { get; set; }

        [Range(1, 20, ErrorMessage = "Số món gắn nhãn phải từ 1 đến 20.")]
        public int BestSellerTopCount { get; set; }
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Time)]
    public TimeOnly? SaleStartsAt { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Time)]
    public TimeOnly? SaleEndsAt { get; set; }

    [BindProperty]
    public SettingsInput Settings { get; set; } = new();

    public MenuPerformanceResult Report { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
        => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostUpdateSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(
                " ",
                ModelState.Values.SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage));
            return RedirectToPage();
        }

        await _menuPerformance.UpdateSettingsAsync(
            Settings.SlowItemThreshold,
            Settings.BestSellerTopCount,
            GetCurrentAccountId(),
            cancellationToken);

        TempData["SuccessMessage"] = "Đã cập nhật ngưỡng báo cáo món.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var from = (From ?? today.AddDays(-29)).Date;
        var to = (To ?? today).Date;
        if (to < from)
        {
            (from, to) = (to, from);
        }

        if ((SaleStartsAt is null) != (SaleEndsAt is null))
        {
            SaleStartsAt = null;
            SaleEndsAt = null;
            TempData["ErrorMessage"] =
                "Khung giờ cần nhập cả giờ bắt đầu và kết thúc; báo cáo đang hiển thị cả ngày.";
        }

        var setting = await _db.StoreSettings
            .AsNoTracking()
            .SingleAsync(
                item => item.Id == StoreSetting.SingletonId,
                cancellationToken);

        Settings = new SettingsInput
        {
            SlowItemThreshold = setting.SlowItemThreshold,
            BestSellerTopCount = setting.BestSellerTopCount
        };
        Report = await _menuPerformance.GetReportAsync(
            from,
            to.AddDays(1),
            new SaleTimeWindow(SaleStartsAt, SaleEndsAt),
            setting.SlowItemThreshold,
            cancellationToken);

        From = from;
        To = to;
    }

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Không xác định được tài khoản hiện tại.");
}
