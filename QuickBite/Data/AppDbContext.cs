using Microsoft.EntityFrameworkCore;
using QuickBite.Models;

namespace QuickBite.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

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

        mb.Entity<AdminUser>()
          .HasIndex(u => u.Username)
          .IsUnique();

        mb.Entity<Order>().HasIndex(o => o.Status);
        mb.Entity<Order>().HasIndex(o => o.CreatedAt);
    }
}
