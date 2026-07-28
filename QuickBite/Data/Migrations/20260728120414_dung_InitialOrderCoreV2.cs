using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuickBite.Data.Migrations
{
    /// <inheritdoc />
    public partial class dung_InitialOrderCoreV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneBlacklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Phone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneBlacklists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReasonCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    AcceptedByAccountId = table.Column<int>(type: "int", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Accounts_AcceptedByAccountId",
                        column: x => x.AcceptedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CheckInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSessions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ChangedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Accounts_ChangedByAccountId",
                        column: x => x.ChangedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "FullName", "IsActive", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "Chủ quán QuickBite", true, "AQAAAAEAACcQAAAAEJFpZ+ufTGXPh6BKlRvKjXzXADutxFQ/qwL568hK7uH0t/S5bWeEtmfzhiKXcqSkmQ==", 0, "admin" },
                    { 2, "Quản lý ca", true, "AQAAAAIAAYagAAAAEGoFiVYx+Wgwqn9m1YSLrJInS2af1iMHtb562Y6KK6PBi8Kvcx4O+jkzHjmuTrMHtQ==", 4, "manager" },
                    { 3, "Nhân viên nhận đơn", true, "AQAAAAIAAYagAAAAEHV+SCr2GoiocJowWlig57BqENDkrmrIWMuoiEouQVJPh8bbpDB8OCom/sEpf+35QA==", 1, "staff" },
                    { 4, "Bếp", true, "AQAAAAIAAYagAAAAEK9k5yyJ+GUBVAWApffAO0aTAaX3TdQPaLkwKkbTK6tDKVVrMIvVLZQwpvccTnj7lw==", 2, "kitchen" },
                    { 5, "Shipper", true, "AQAAAAIAAYagAAAAEHWoZzeZv3ASHpz5XL84pUUBylB35RExme6xcmoUjnRoftIvgEx90xcppMehskjnqg==", 3, "shipper" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "DisplayOrder", "Name" },
                values: new object[,]
                {
                    { 1, "Các món cơm phần đầy đặn", 1, "Cơm" },
                    { 2, "Món nước truyền thống", 2, "Phở & Bún" },
                    { 3, "Món ăn chơi, ăn kèm", 3, "Ăn vặt" },
                    { 4, "Giải khát, cà phê, trà", 4, "Đồ uống" },
                    { 5, "Chè, bánh ngọt, kem", 5, "Tráng miệng" },
                    { 6, "Thanh đạm, phù hợp ngày rằm - mùng 1", 6, "Món chay" }
                });

            migrationBuilder.InsertData(
                table: "ReasonCatalogs",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "Kind", "Text" },
                values: new object[,]
                {
                    { 1, 1, true, 0, "Hết nguyên liệu" },
                    { 2, 2, true, 0, "Ngoài phạm vi giao" },
                    { 3, 3, true, 0, "Quán quá tải" },
                    { 4, 4, true, 0, "Nghi ngờ đơn ảo" },
                    { 5, 1, true, 1, "Đặt nhầm" },
                    { 6, 2, true, 1, "Đổi ý không đặt nữa" },
                    { 7, 3, true, 1, "Chờ quá lâu" },
                    { 8, 1, true, 2, "Khách không nghe máy" },
                    { 9, 2, true, 2, "Địa chỉ sai hoặc không tìm thấy" },
                    { 10, 3, true, 2, "Khách từ chối nhận hàng" },
                    { 11, 1, true, 3, "Khách không đến lấy" },
                    { 12, 2, true, 3, "Không liên lạc được với khách" }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "ImageUrl", "IsAvailable", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sườn nướng than, bì, chả trứng, mỡ hành, nước mắm chua ngọt", "https://picsum.photos/seed/comtam/400/300", true, "Cơm tấm sườn bì chả", 45000m },
                    { 2, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đùi gà da giòn xối mỡ tỏi, cơm chiên nghệ, dưa leo", "https://picsum.photos/seed/comga/400/300", true, "Cơm gà xối mỡ", 40000m },
                    { 3, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cơm chiên trứng, lạp xưởng, đậu que, cà rốt", "https://picsum.photos/seed/comchien/400/300", true, "Cơm chiên dương châu", 35000m },
                    { 4, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bò mềm xào lúc lắc ớt chuông, khoai tây chiên ăn kèm", "https://picsum.photos/seed/boluclac/400/300", true, "Cơm bò lúc lắc", 55000m },
                    { 5, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sườn cốt lết ướp mật ong nướng, kim chi cải thảo", "https://picsum.photos/seed/suonmatong/400/300", true, "Cơm sườn nướng mật ong", 48000m },
                    { 6, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nước dùng hầm xương 8 tiếng, bò tái mềm, bánh phở tươi", "https://picsum.photos/seed/phobo/400/300", true, "Phở bò tái", 40000m },
                    { 7, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Gà ta xé, nước dùng thanh, hành lá gừng thái sợi", "https://picsum.photos/seed/phoga/400/300", true, "Phở gà", 35000m },
                    { 8, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cay chuẩn vị Huế, giò heo, chả cua, rau sống đầy đủ", "https://picsum.photos/seed/bunbo/400/300", true, "Bún bò Huế", 45000m },
                    { 9, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Chả nướng than hoa, bún rối, nước chấm đu đủ xanh", "https://picsum.photos/seed/buncha/400/300", false, "Bún chả Hà Nội", 40000m },
                    { 10, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Thịt nướng sả, chả giò, đồ chua, đậu phộng rang", "https://picsum.photos/seed/bunthitnuong/400/300", true, "Bún thịt nướng", 35000m },
                    { 11, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Da giòn rụm, ướp 12 loại gia vị, kèm tương ớt", "https://picsum.photos/seed/garan/400/300", true, "Gà rán giòn (2 miếng)", 35000m },
                    { 12, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Khoai chiên hai lửa giòn lâu, rắc phô mai tùy chọn", "https://picsum.photos/seed/khoaitay/400/300", true, "Khoai tây chiên", 20000m },
                    { 13, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bánh mì nóng, thịt nướng, pate, đồ chua, rau thơm", "https://picsum.photos/seed/banhmi/400/300", true, "Bánh mì thịt nướng", 25000m },
                    { 14, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nem truyền thống nhân thịt mộc nhĩ, chấm mắm chua ngọt", "https://picsum.photos/seed/nemran/400/300", true, "Nem rán (5 cái)", 30000m },
                    { 15, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bò viên, cá viên, đậu bắp cuộn — 6 xiên nướng sốt me", "https://picsum.photos/seed/xienque/400/300", true, "Xiên que thập cẩm", 25000m },
                    { 16, 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Trà đen ủ lạnh, đào ngâm, cam vàng, sả tươi", "https://picsum.photos/seed/tradao/400/300", true, "Trà đào cam sả", 25000m },
                    { 17, 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cà phê phin robusta đậm, sữa đặc", "https://picsum.photos/seed/caphe/400/300", true, "Cà phê sữa đá", 20000m },
                    { 18, 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cam sành vắt nguyên chất, không đường tùy chọn", "https://picsum.photos/seed/nuoccam/400/300", true, "Nước cam ép", 30000m },
                    { 19, 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Trân châu đường đen nấu mỗi 2 giờ, trà ô long", "https://picsum.photos/seed/trasua/400/300", true, "Trà sữa trân châu", 30000m },
                    { 20, 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Lon 330ml ướp lạnh", "https://picsum.photos/seed/coca/400/300", true, "Coca-Cola", 15000m },
                    { 21, 5, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Khúc bạch phô mai, nhãn, hạnh nhân lát", "https://picsum.photos/seed/chekhucbach/400/300", true, "Chè khúc bạch", 25000m },
                    { 22, 5, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Flan trứng sữa mềm mịn, caramel đắng nhẹ", "https://picsum.photos/seed/banhflan/400/300", true, "Bánh flan", 15000m },
                    { 23, 5, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Rau câu nước dừa tươi, lớp cốt dừa béo", "https://picsum.photos/seed/raucau/400/300", true, "Rau câu dừa", 15000m },
                    { 24, 5, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nếp cẩm dẻo thơm, sữa chua nhà làm", "https://picsum.photos/seed/nepcam/400/300", true, "Sữa chua nếp cẩm", 20000m },
                    { 25, 5, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kem dừa trong trái dừa tươi, đậu phộng, mứt", "https://picsum.photos/seed/kemdua/400/300", false, "Kem dừa", 30000m },
                    { 26, 6, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đậu hũ, nấm, rau củ kho, canh rong biển", "https://picsum.photos/seed/comchay/400/300", true, "Cơm chay thập cẩm", 35000m },
                    { 27, 6, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Riêu đậu hũ nấm, cà chua, đậu rán", "https://picsum.photos/seed/bunrieuchay/400/300", true, "Bún riêu chay", 35000m },
                    { 28, 6, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đậu hũ non chiên sốt cà chua, hành lá", "https://picsum.photos/seed/dauhusotca/400/300", true, "Đậu hũ sốt cà", 25000m },
                    { 29, 6, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cuốn rau củ, bún, đậu hũ; chấm tương đậu phộng", "https://picsum.photos/seed/goicuonchay/400/300", true, "Gỏi cuốn chay (3 cuốn)", 25000m },
                    { 30, 6, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nấm bào ngư xào sả ớt cay nhẹ, ăn kèm cơm trắng", "https://picsum.photos/seed/namxao/400/300", true, "Nấm xào sả ớt", 30000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AcceptedByAccountId",
                table: "Orders",
                column: "AcceptedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ChangedByAccountId",
                table: "OrderStatusHistories",
                column: "ChangedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                table: "OrderStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBlacklists_Phone",
                table: "PhoneBlacklists",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSessions_AccountId_CheckInAt",
                table: "WorkSessions",
                columns: new[] { "AccountId", "CheckInAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropTable(
                name: "PhoneBlacklists");

            migrationBuilder.DropTable(
                name: "ReasonCatalogs");

            migrationBuilder.DropTable(
                name: "WorkSessions");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
