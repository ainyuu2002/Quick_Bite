using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Modules.Operations.Ingredients;
using QuickBite.Modules.Operations.MenuAvailability;
using QuickBite.Modules.Operations.Store;

namespace QuickBite.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<WorkSession> WorkSessions => Set<WorkSession>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<ReasonCatalog> ReasonCatalogs => Set<ReasonCatalog>();
    public DbSet<PhoneBlacklist> PhoneBlacklists => Set<PhoneBlacklist>();
    public DbSet<StoreSetting> StoreSettings => Set<StoreSetting>();
    public DbSet<DailyQuota> DailyQuotas => Set<DailyQuota>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<DishIngredient> DishIngredients => Set<DishIngredient>();
    public DbSet<RestockLog> RestockLogs => Set<RestockLog>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionUsage> PromotionUsages => Set<PromotionUsage>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<PointLedger> PointLedgers => Set<PointLedger>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<InternalRating> InternalRatings => Set<InternalRating>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<MenuItem>()
          .HasOne(m => m.Category)
          .WithMany(c => c.MenuItems)
          .HasForeignKey(m => m.CategoryId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<OrderItem>()
          .HasOne(oi => oi.Order)
          .WithMany(o => o.Items)
          .HasForeignKey(oi => oi.OrderId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<OrderItem>()
          .HasOne(oi => oi.MenuItem)
          .WithMany(m => m.OrderItems)
          .HasForeignKey(oi => oi.MenuItemId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Account>()
          .HasIndex(u => u.Username)
          .IsUnique();

        mb.Entity<Account>()
          .ToTable(table =>
              table.HasCheckConstraint(
                  "CK_Accounts_HourlyRate",
                  "[HourlyRate] >= 0"));

        // Người nhận đơn: Restrict để không bao giờ mất dấu ai đã xử lý đơn cũ.
        // Muốn "xoá" nhân viên thì tắt Account.IsActive, không xoá cứng.
        mb.Entity<Order>()
          .HasOne(o => o.AcceptedByAccount)
          .WithMany()
          .HasForeignKey(o => o.AcceptedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<WorkSession>()
          .HasOne(w => w.Account)
          .WithMany(a => a.WorkSessions)
          .HasForeignKey(w => w.AccountId)
          .OnDelete(DeleteBehavior.Restrict);

        // Phục vụ báo cáo chấm công theo người + theo ngày.
        mb.Entity<WorkSession>()
          .HasIndex(w => new { w.AccountId, w.CheckInAt });

        mb.Entity<WorkSession>()
          .HasIndex(w => w.ApprovalStatus);

        mb.Entity<WorkSession>()
          .ToTable(table =>
              table.HasCheckConstraint(
                  "CK_WorkSessions_ApprovalStatus",
                  "[ApprovalStatus] IN (0, 1)"));

        mb.Entity<WorkSession>()
          .HasOne<Account>()
          .WithMany()
          .HasForeignKey(w => w.ApprovedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<StoreSetting>()
          .ToTable(table =>
          {
              table.HasCheckConstraint("CK_StoreSettings_Singleton", "[Id] = 1");
              table.HasCheckConstraint(
                  "CK_StoreSettings_SlowItemThreshold",
                  "[SlowItemThreshold] BETWEEN 1 AND 1000");
              table.HasCheckConstraint(
                  "CK_StoreSettings_BestSellerTopCount",
                  "[BestSellerTopCount] BETWEEN 1 AND 20");
          });

        mb.Entity<StoreSetting>()
          .HasOne<Account>()
          .WithMany()
          .HasForeignKey(s => s.UpdatedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<DailyQuota>()
          .ToTable(table =>
          {
              table.HasCheckConstraint(
                  "CK_DailyQuotas_DailyLimit",
                  "[DailyLimit] > 0");
              table.HasCheckConstraint(
                  "CK_DailyQuotas_ReservedQuantity",
                  "[ReservedQuantity] >= 0");
              table.HasCheckConstraint(
                  "CK_DailyQuotas_SaleWindow",
                  "([SaleStartsAt] IS NULL AND [SaleEndsAt] IS NULL) OR " +
                  "([SaleStartsAt] IS NOT NULL AND [SaleEndsAt] IS NOT NULL)");
          });

        mb.Entity<DailyQuota>()
          .HasIndex(quota => quota.MenuItemId)
          .IsUnique();

        mb.Entity<DailyQuota>()
          .HasOne(quota => quota.MenuItem)
          .WithOne()
          .HasForeignKey<DailyQuota>(quota => quota.MenuItemId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<DailyQuota>()
          .HasOne<Account>()
          .WithMany()
          .HasForeignKey(quota => quota.UpdatedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Ingredient>()
          .HasIndex(item => item.Name)
          .IsUnique();

        mb.Entity<Ingredient>()
          .HasIndex(item => item.Status);

        mb.Entity<Ingredient>()
          .ToTable(table =>
              table.HasCheckConstraint(
                  "CK_Ingredients_Status",
                  "[Status] IN (0, 1, 2)"));

        mb.Entity<Ingredient>()
          .HasOne<Account>()
          .WithMany()
          .HasForeignKey(item => item.UpdatedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<DishIngredient>()
          .HasKey(link => new { link.IngredientId, link.MenuItemId });

        mb.Entity<DishIngredient>()
          .HasOne(link => link.Ingredient)
          .WithMany(item => item.Dishes)
          .HasForeignKey(link => link.IngredientId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<DishIngredient>()
          .HasOne(link => link.MenuItem)
          .WithMany()
          .HasForeignKey(link => link.MenuItemId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<RestockLog>()
          .ToTable(table =>
          {
              table.HasCheckConstraint(
                  "CK_RestockLogs_Action",
                  "[Action] IN (0, 1)");
              table.HasCheckConstraint(
                  "CK_RestockLogs_PreviousStatus",
                  "[PreviousStatus] IN (0, 1, 2)");
              table.HasCheckConstraint(
                  "CK_RestockLogs_NewStatus",
                  "[NewStatus] IN (0, 1, 2)");
          });

        mb.Entity<RestockLog>()
          .HasOne(log => log.Ingredient)
          .WithMany(item => item.Logs)
          .HasForeignKey(log => log.IngredientId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<RestockLog>()
          .HasOne<Account>()
          .WithMany()
          .HasForeignKey(log => log.ActorAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<RestockLog>()
          .HasIndex(log => new { log.IngredientId, log.CreatedAt });

        mb.Entity<Order>().HasIndex(o => o.Status);
        mb.Entity<Order>().HasIndex(o => o.CreatedAt);
        mb.Entity<Order>()
          .Property(o => o.PaymentMethod)
          .HasDefaultValue(PaymentMethod.Cash);
        mb.Entity<OrderStatusHistory>()
          .HasOne(h => h.Order)
          .WithMany()
          .HasForeignKey(h => h.OrderId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<OrderStatusHistory>()
          .HasOne(h => h.ChangedByAccount)
          .WithMany()
          .HasForeignKey(h => h.ChangedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<OrderStatusHistory>().HasIndex(h => h.OrderId);

        mb.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
        mb.Entity<PhoneBlacklist>().HasIndex(b => b.Phone).IsUnique();

        mb.Entity<Order>()
          .Property(o => o.DiscountAmount)
          .HasDefaultValue(0m);

        mb.Entity<Customer>()
          .HasIndex(c => c.Phone)
          .IsUnique();

        mb.Entity<Order>()
          .HasOne(o => o.Customer)
          .WithMany()
          .HasForeignKey(o => o.CustomerId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Order>()
          .HasOne(o => o.Promotion)
          .WithMany()
          .HasForeignKey(o => o.PromotionId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Order>()
          .HasOne(o => o.Voucher)
          .WithMany()
          .HasForeignKey(o => o.VoucherId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<OtpVerification>()
          .HasIndex(o => new { o.Phone, o.Purpose, o.CreatedAt });

        mb.Entity<Promotion>()
          .HasIndex(p => p.Code)
          .IsUnique();

        mb.Entity<PromotionUsage>()
          .HasOne(u => u.Promotion)
          .WithMany(p => p.Usages)
          .HasForeignKey(u => u.PromotionId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PromotionUsage>()
          .HasIndex(u => new { u.PromotionId, u.Phone });

        mb.Entity<PromotionUsage>()
          .HasIndex(u => u.OrderId);

        mb.Entity<Voucher>()
          .HasIndex(v => v.Code)
          .IsUnique();

        mb.Entity<Voucher>()
          .HasOne(v => v.Customer)
          .WithMany(c => c.Vouchers)
          .HasForeignKey(v => v.CustomerId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PointLedger>()
          .HasOne(p => p.Customer)
          .WithMany(c => c.PointEntries)
          .HasForeignKey(p => p.CustomerId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PointLedger>()
          .HasIndex(p => new { p.CustomerId, p.CreatedAt });

        mb.Entity<Complaint>()
          .HasOne(c => c.Order)
          .WithMany()
          .HasForeignKey(c => c.OrderId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Complaint>()
          .HasIndex(c => c.OrderId)
          .IsUnique();

        mb.Entity<Complaint>()
          .HasIndex(c => c.Status);

        mb.Entity<InternalRating>()
          .HasOne(r => r.Order)
          .WithMany()
          .HasForeignKey(r => r.OrderId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<InternalRating>()
          .HasIndex(r => r.OrderId)
          .IsUnique();
    }
}
