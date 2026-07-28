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
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<WorkSession> WorkSessions => Set<WorkSession>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<ReasonCatalog> ReasonCatalogs => Set<ReasonCatalog>();

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

        SeedReferenceData(mb);
    }

    private static void SeedReferenceData(ModelBuilder mb)
    {
        var seedDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Unspecified);

        mb.Entity<Account>().HasData(
            new Account
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "AQAAAAEAACcQAAAAEJFpZ+ufTGXPh6BKlRvKjXzXADutxFQ/qwL568hK7uH0t/S5bWeEtmfzhiKXcqSkmQ==",
                FullName = "Chủ quán QuickBite",
                Role = AccountRole.Admin,
                IsActive = true
            },
            new Account
            {
                Id = 2,
                Username = "manager",
                PasswordHash = "AQAAAAIAAYagAAAAEGoFiVYx+Wgwqn9m1YSLrJInS2af1iMHtb562Y6KK6PBi8Kvcx4O+jkzHjmuTrMHtQ==",
                FullName = "Quản lý ca",
                Role = AccountRole.Manager,
                IsActive = true
            },
            new Account
            {
                Id = 3,
                Username = "staff",
                PasswordHash = "AQAAAAIAAYagAAAAEHV+SCr2GoiocJowWlig57BqENDkrmrIWMuoiEouQVJPh8bbpDB8OCom/sEpf+35QA==",
                FullName = "Nhân viên nhận đơn",
                Role = AccountRole.Staff,
                IsActive = true
            },
            new Account
            {
                Id = 4,
                Username = "kitchen",
                PasswordHash = "AQAAAAIAAYagAAAAEK9k5yyJ+GUBVAWApffAO0aTAaX3TdQPaLkwKkbTK6tDKVVrMIvVLZQwpvccTnj7lw==",
                FullName = "Bếp",
                Role = AccountRole.Kitchen,
                IsActive = true
            },
            new Account
            {
                Id = 5,
                Username = "shipper",
                PasswordHash = "AQAAAAIAAYagAAAAEHWoZzeZv3ASHpz5XL84pUUBylB35RExme6xcmoUjnRoftIvgEx90xcppMehskjnqg==",
                FullName = "Shipper",
                Role = AccountRole.Shipper,
                IsActive = true
            });

        mb.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Cơm", Description = "Các món cơm phần đầy đặn", DisplayOrder = 1 },
            new Category { Id = 2, Name = "Phở & Bún", Description = "Món nước truyền thống", DisplayOrder = 2 },
            new Category { Id = 3, Name = "Ăn vặt", Description = "Món ăn chơi, ăn kèm", DisplayOrder = 3 },
            new Category { Id = 4, Name = "Đồ uống", Description = "Giải khát, cà phê, trà", DisplayOrder = 4 },
            new Category { Id = 5, Name = "Tráng miệng", Description = "Chè, bánh ngọt, kem", DisplayOrder = 5 },
            new Category { Id = 6, Name = "Món chay", Description = "Thanh đạm, phù hợp ngày rằm - mùng 1", DisplayOrder = 6 });

        mb.Entity<MenuItem>().HasData(
            new MenuItem { Id = 1, CategoryId = 1, Name = "Cơm tấm sườn bì chả", Price = 45000, Description = "Sườn nướng than, bì, chả trứng, mỡ hành, nước mắm chua ngọt", ImageUrl = "https://picsum.photos/seed/comtam/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 2, CategoryId = 1, Name = "Cơm gà xối mỡ", Price = 40000, Description = "Đùi gà da giòn xối mỡ tỏi, cơm chiên nghệ, dưa leo", ImageUrl = "https://picsum.photos/seed/comga/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 3, CategoryId = 1, Name = "Cơm chiên dương châu", Price = 35000, Description = "Cơm chiên trứng, lạp xưởng, đậu que, cà rốt", ImageUrl = "https://picsum.photos/seed/comchien/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 4, CategoryId = 1, Name = "Cơm bò lúc lắc", Price = 55000, Description = "Bò mềm xào lúc lắc ớt chuông, khoai tây chiên ăn kèm", ImageUrl = "https://picsum.photos/seed/boluclac/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 5, CategoryId = 1, Name = "Cơm sườn nướng mật ong", Price = 48000, Description = "Sườn cốt lết ướp mật ong nướng, kim chi cải thảo", ImageUrl = "https://picsum.photos/seed/suonmatong/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 6, CategoryId = 2, Name = "Phở bò tái", Price = 40000, Description = "Nước dùng hầm xương 8 tiếng, bò tái mềm, bánh phở tươi", ImageUrl = "https://picsum.photos/seed/phobo/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 7, CategoryId = 2, Name = "Phở gà", Price = 35000, Description = "Gà ta xé, nước dùng thanh, hành lá gừng thái sợi", ImageUrl = "https://picsum.photos/seed/phoga/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 8, CategoryId = 2, Name = "Bún bò Huế", Price = 45000, Description = "Cay chuẩn vị Huế, giò heo, chả cua, rau sống đầy đủ", ImageUrl = "https://picsum.photos/seed/bunbo/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 9, CategoryId = 2, Name = "Bún chả Hà Nội", Price = 40000, Description = "Chả nướng than hoa, bún rối, nước chấm đu đủ xanh", ImageUrl = "https://picsum.photos/seed/buncha/400/300", IsAvailable = false, CreatedAt = seedDate },
            new MenuItem { Id = 10, CategoryId = 2, Name = "Bún thịt nướng", Price = 35000, Description = "Thịt nướng sả, chả giò, đồ chua, đậu phộng rang", ImageUrl = "https://picsum.photos/seed/bunthitnuong/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 11, CategoryId = 3, Name = "Gà rán giòn (2 miếng)", Price = 35000, Description = "Da giòn rụm, ướp 12 loại gia vị, kèm tương ớt", ImageUrl = "https://picsum.photos/seed/garan/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 12, CategoryId = 3, Name = "Khoai tây chiên", Price = 20000, Description = "Khoai chiên hai lửa giòn lâu, rắc phô mai tùy chọn", ImageUrl = "https://picsum.photos/seed/khoaitay/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 13, CategoryId = 3, Name = "Bánh mì thịt nướng", Price = 25000, Description = "Bánh mì nóng, thịt nướng, pate, đồ chua, rau thơm", ImageUrl = "https://picsum.photos/seed/banhmi/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 14, CategoryId = 3, Name = "Nem rán (5 cái)", Price = 30000, Description = "Nem truyền thống nhân thịt mộc nhĩ, chấm mắm chua ngọt", ImageUrl = "https://picsum.photos/seed/nemran/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 15, CategoryId = 3, Name = "Xiên que thập cẩm", Price = 25000, Description = "Bò viên, cá viên, đậu bắp cuộn — 6 xiên nướng sốt me", ImageUrl = "https://picsum.photos/seed/xienque/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 16, CategoryId = 4, Name = "Trà đào cam sả", Price = 25000, Description = "Trà đen ủ lạnh, đào ngâm, cam vàng, sả tươi", ImageUrl = "https://picsum.photos/seed/tradao/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 17, CategoryId = 4, Name = "Cà phê sữa đá", Price = 20000, Description = "Cà phê phin robusta đậm, sữa đặc", ImageUrl = "https://picsum.photos/seed/caphe/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 18, CategoryId = 4, Name = "Nước cam ép", Price = 30000, Description = "Cam sành vắt nguyên chất, không đường tùy chọn", ImageUrl = "https://picsum.photos/seed/nuoccam/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 19, CategoryId = 4, Name = "Trà sữa trân châu", Price = 30000, Description = "Trân châu đường đen nấu mỗi 2 giờ, trà ô long", ImageUrl = "https://picsum.photos/seed/trasua/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 20, CategoryId = 4, Name = "Coca-Cola", Price = 15000, Description = "Lon 330ml ướp lạnh", ImageUrl = "https://picsum.photos/seed/coca/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 21, CategoryId = 5, Name = "Chè khúc bạch", Price = 25000, Description = "Khúc bạch phô mai, nhãn, hạnh nhân lát", ImageUrl = "https://picsum.photos/seed/chekhucbach/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 22, CategoryId = 5, Name = "Bánh flan", Price = 15000, Description = "Flan trứng sữa mềm mịn, caramel đắng nhẹ", ImageUrl = "https://picsum.photos/seed/banhflan/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 23, CategoryId = 5, Name = "Rau câu dừa", Price = 15000, Description = "Rau câu nước dừa tươi, lớp cốt dừa béo", ImageUrl = "https://picsum.photos/seed/raucau/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 24, CategoryId = 5, Name = "Sữa chua nếp cẩm", Price = 20000, Description = "Nếp cẩm dẻo thơm, sữa chua nhà làm", ImageUrl = "https://picsum.photos/seed/nepcam/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 25, CategoryId = 5, Name = "Kem dừa", Price = 30000, Description = "Kem dừa trong trái dừa tươi, đậu phộng, mứt", ImageUrl = "https://picsum.photos/seed/kemdua/400/300", IsAvailable = false, CreatedAt = seedDate },
            new MenuItem { Id = 26, CategoryId = 6, Name = "Cơm chay thập cẩm", Price = 35000, Description = "Đậu hũ, nấm, rau củ kho, canh rong biển", ImageUrl = "https://picsum.photos/seed/comchay/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 27, CategoryId = 6, Name = "Bún riêu chay", Price = 35000, Description = "Riêu đậu hũ nấm, cà chua, đậu rán", ImageUrl = "https://picsum.photos/seed/bunrieuchay/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 28, CategoryId = 6, Name = "Đậu hũ sốt cà", Price = 25000, Description = "Đậu hũ non chiên sốt cà chua, hành lá", ImageUrl = "https://picsum.photos/seed/dauhusotca/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 29, CategoryId = 6, Name = "Gỏi cuốn chay (3 cuốn)", Price = 25000, Description = "Cuốn rau củ, bún, đậu hũ; chấm tương đậu phộng", ImageUrl = "https://picsum.photos/seed/goicuonchay/400/300", IsAvailable = true, CreatedAt = seedDate },
            new MenuItem { Id = 30, CategoryId = 6, Name = "Nấm xào sả ớt", Price = 30000, Description = "Nấm bào ngư xào sả ớt cay nhẹ, ăn kèm cơm trắng", ImageUrl = "https://picsum.photos/seed/namxao/400/300", IsAvailable = true, CreatedAt = seedDate });

        mb.Entity<ReasonCatalog>().HasData(
            new ReasonCatalog { Id = 1, Kind = ReasonKind.Reject, Text = "Hết nguyên liệu", DisplayOrder = 1, IsActive = true },
            new ReasonCatalog { Id = 2, Kind = ReasonKind.Reject, Text = "Ngoài phạm vi giao", DisplayOrder = 2, IsActive = true },
            new ReasonCatalog { Id = 3, Kind = ReasonKind.Reject, Text = "Quán quá tải", DisplayOrder = 3, IsActive = true },
            new ReasonCatalog { Id = 4, Kind = ReasonKind.Reject, Text = "Nghi ngờ đơn ảo", DisplayOrder = 4, IsActive = true },
            new ReasonCatalog { Id = 5, Kind = ReasonKind.Cancel, Text = "Đặt nhầm", DisplayOrder = 1, IsActive = true },
            new ReasonCatalog { Id = 6, Kind = ReasonKind.Cancel, Text = "Đổi ý không đặt nữa", DisplayOrder = 2, IsActive = true },
            new ReasonCatalog { Id = 7, Kind = ReasonKind.Cancel, Text = "Chờ quá lâu", DisplayOrder = 3, IsActive = true },
            new ReasonCatalog { Id = 8, Kind = ReasonKind.DeliveryFailed, Text = "Khách không nghe máy", DisplayOrder = 1, IsActive = true },
            new ReasonCatalog { Id = 9, Kind = ReasonKind.DeliveryFailed, Text = "Địa chỉ sai hoặc không tìm thấy", DisplayOrder = 2, IsActive = true },
            new ReasonCatalog { Id = 10, Kind = ReasonKind.DeliveryFailed, Text = "Khách từ chối nhận hàng", DisplayOrder = 3, IsActive = true },
            new ReasonCatalog { Id = 11, Kind = ReasonKind.NoShow, Text = "Khách không đến lấy", DisplayOrder = 1, IsActive = true },
            new ReasonCatalog { Id = 12, Kind = ReasonKind.NoShow, Text = "Không liên lạc được với khách", DisplayOrder = 2, IsActive = true });
    }
}
