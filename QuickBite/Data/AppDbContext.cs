using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
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
    public DbSet<StoreSetting> StoreSettings => Set<StoreSetting>();
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

        mb.Entity<StoreSetting>()
          .ToTable(table =>
              table.HasCheckConstraint("CK_StoreSettings_Singleton", "[Id] = 1"));

        mb.Entity<StoreSetting>()
          .HasOne<Account>()
          .WithMany()
          .HasForeignKey(s => s.UpdatedByAccountId)
          .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Order>().HasIndex(o => o.Status);
        mb.Entity<Order>().HasIndex(o => o.CreatedAt);
        mb.Entity<Order>()
          .Property(o => o.PaymentMethod)
          .HasDefaultValue(PaymentMethod.Cash);
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
