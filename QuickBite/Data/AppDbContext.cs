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
    }
}
