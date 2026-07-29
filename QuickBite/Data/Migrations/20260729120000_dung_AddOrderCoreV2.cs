using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

/// <summary>
/// Order Core v2: bổ sung cột đặt-hàng/đặt-tiệc + mã tra cứu trên Orders và ba bảng
/// OrderStatusHistories / ReasonCatalogs / PhoneBlacklists.
/// Viết tay theo phong cách Database First của nhánh (guard idempotent, không dùng snapshot),
/// chỉ CỘNG THÊM phần Order Core lên trên chuỗi migration sẵn có — chạy Update-Database như thường.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260729120000_dung_AddOrderCoreV2")]
public sealed class dung_AddOrderCoreV2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1) Các cột Order Core trên Orders (guard theo từng cột để chạy được trên DB đã có sẵn).
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'OrderType') IS NULL
                ALTER TABLE [dbo].[Orders]
                    ADD [OrderType] int NOT NULL CONSTRAINT [DF_Orders_OrderType] DEFAULT (0);

            IF COL_LENGTH(N'[dbo].[Orders]', N'PaymentStatus') IS NULL
                ALTER TABLE [dbo].[Orders]
                    ADD [PaymentStatus] int NOT NULL CONSTRAINT [DF_Orders_PaymentStatus] DEFAULT (0);

            IF COL_LENGTH(N'[dbo].[Orders]', N'DeliveryFee') IS NULL
                ALTER TABLE [dbo].[Orders]
                    ADD [DeliveryFee] decimal(18,0) NOT NULL CONSTRAINT [DF_Orders_DeliveryFee] DEFAULT (0);

            IF COL_LENGTH(N'[dbo].[Orders]', N'IsPartyOrder') IS NULL
                ALTER TABLE [dbo].[Orders]
                    ADD [IsPartyOrder] bit NOT NULL CONSTRAINT [DF_Orders_IsPartyOrder] DEFAULT (0);

            IF COL_LENGTH(N'[dbo].[Orders]', N'ScheduledFor') IS NULL
                ALTER TABLE [dbo].[Orders] ADD [ScheduledFor] datetime2 NULL;

            IF COL_LENGTH(N'[dbo].[Orders]', N'DepositAmount') IS NULL
                ALTER TABLE [dbo].[Orders]
                    ADD [DepositAmount] decimal(18,0) NOT NULL CONSTRAINT [DF_Orders_DepositAmount] DEFAULT (0);

            IF COL_LENGTH(N'[dbo].[Orders]', N'DepositPaid') IS NULL
                ALTER TABLE [dbo].[Orders]
                    ADD [DepositPaid] bit NOT NULL CONSTRAINT [DF_Orders_DepositPaid] DEFAULT (0);

            IF COL_LENGTH(N'[dbo].[Orders]', N'ApprovedAt') IS NULL
                ALTER TABLE [dbo].[Orders] ADD [ApprovedAt] datetime2 NULL;
            """);

        // 2) Đơn Pickup lưu Address = NULL, nên nới cột Address (base đang NOT NULL nvarchar(300)).
        migrationBuilder.Sql(
            """
            ALTER TABLE [dbo].[Orders] ALTER COLUMN [Address] nvarchar(500) NULL;
            """);

        // 3) OrderCode: thêm nullable -> backfill mã duy nhất cho đơn cũ -> NOT NULL -> unique index.
        //    (Dùng EXEC để cột mới có hiệu lực ở batch con trước khi backfill/alter.)
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'OrderCode') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] ADD [OrderCode] nvarchar(20) NULL;
                EXEC(N'UPDATE [dbo].[Orders]
                       SET [OrderCode] = ''QB-'' + RIGHT(''000000'' + CAST([Id] AS varchar(6)), 6)
                       WHERE [OrderCode] IS NULL;');
                EXEC(N'ALTER TABLE [dbo].[Orders] ALTER COLUMN [OrderCode] nvarchar(20) NOT NULL;');
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_Orders_OrderCode' AND object_id = OBJECT_ID(N'[dbo].[Orders]'))
                CREATE UNIQUE INDEX [IX_Orders_OrderCode] ON [dbo].[Orders]([OrderCode]);
            """);

        // 4) Bảng lịch sử chuyển trạng thái đơn.
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[OrderStatusHistories]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OrderStatusHistories] (
                    [Id]                 int IDENTITY(1,1) NOT NULL
                                         CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY,
                    [OrderId]            int NOT NULL,
                    [FromStatus]         int NULL,
                    [ToStatus]           int NOT NULL,
                    [Reason]             nvarchar(300) NULL,
                    [ChangedByAccountId] int NULL,
                    [ChangedAt]          datetime2 NOT NULL,
                    CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId]
                        FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_OrderStatusHistories_Accounts_ChangedByAccountId]
                        FOREIGN KEY ([ChangedByAccountId]) REFERENCES [dbo].[Accounts]([Id])
                );
                CREATE INDEX [IX_OrderStatusHistories_OrderId]
                    ON [dbo].[OrderStatusHistories]([OrderId]);
                CREATE INDEX [IX_OrderStatusHistories_ChangedByAccountId]
                    ON [dbo].[OrderStatusHistories]([ChangedByAccountId]);
            END
            """);

        // 5) Danh mục lý do (từ chối/hủy/giao thất bại/không đến lấy) + seed dữ liệu chuẩn.
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[ReasonCatalogs]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ReasonCatalogs] (
                    [Id]           int IDENTITY(1,1) NOT NULL
                                   CONSTRAINT [PK_ReasonCatalogs] PRIMARY KEY,
                    [Kind]         int NOT NULL,
                    [Text]         nvarchar(200) NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsActive]     bit NOT NULL
                );

                SET IDENTITY_INSERT [dbo].[ReasonCatalogs] ON;
                INSERT INTO [dbo].[ReasonCatalogs] ([Id],[Kind],[Text],[DisplayOrder],[IsActive]) VALUES
                    (1,  0, N'Hết nguyên liệu',                 1, 1),
                    (2,  0, N'Ngoài phạm vi giao',              2, 1),
                    (3,  0, N'Quán quá tải',                    3, 1),
                    (4,  0, N'Nghi ngờ đơn ảo',                 4, 1),
                    (5,  1, N'Đặt nhầm',                        1, 1),
                    (6,  1, N'Đổi ý không đặt nữa',             2, 1),
                    (7,  1, N'Chờ quá lâu',                     3, 1),
                    (8,  2, N'Khách không nghe máy',            1, 1),
                    (9,  2, N'Địa chỉ sai hoặc không tìm thấy', 2, 1),
                    (10, 2, N'Khách từ chối nhận hàng',         3, 1),
                    (11, 3, N'Khách không đến lấy',             1, 1),
                    (12, 3, N'Không liên lạc được với khách',   2, 1);
                SET IDENTITY_INSERT [dbo].[ReasonCatalogs] OFF;
            END
            """);

        // 6) Danh sách SĐT cảnh báo.
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[PhoneBlacklists]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PhoneBlacklists] (
                    [Id]        int IDENTITY(1,1) NOT NULL
                                CONSTRAINT [PK_PhoneBlacklists] PRIMARY KEY,
                    [Phone]     nvarchar(11) NOT NULL,
                    [Reason]    nvarchar(300) NULL,
                    [CreatedAt] datetime2 NOT NULL
                );
                CREATE UNIQUE INDEX [IX_PhoneBlacklists_Phone]
                    ON [dbo].[PhoneBlacklists]([Phone]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[OrderStatusHistories]') IS NOT NULL DROP TABLE [dbo].[OrderStatusHistories];
            IF OBJECT_ID(N'[dbo].[ReasonCatalogs]')       IS NOT NULL DROP TABLE [dbo].[ReasonCatalogs];
            IF OBJECT_ID(N'[dbo].[PhoneBlacklists]')      IS NOT NULL DROP TABLE [dbo].[PhoneBlacklists];

            IF EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = N'IX_Orders_OrderCode' AND object_id = OBJECT_ID(N'[dbo].[Orders]'))
                DROP INDEX [IX_Orders_OrderCode] ON [dbo].[Orders];

            IF COL_LENGTH(N'[dbo].[Orders]', N'OrderCode')    IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [OrderCode];
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Orders_OrderType')    ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_OrderType];
            IF COL_LENGTH(N'[dbo].[Orders]', N'OrderType')    IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [OrderType];
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Orders_PaymentStatus') ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_PaymentStatus];
            IF COL_LENGTH(N'[dbo].[Orders]', N'PaymentStatus') IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [PaymentStatus];
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Orders_DeliveryFee')  ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_DeliveryFee];
            IF COL_LENGTH(N'[dbo].[Orders]', N'DeliveryFee')  IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [DeliveryFee];
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Orders_IsPartyOrder') ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_IsPartyOrder];
            IF COL_LENGTH(N'[dbo].[Orders]', N'IsPartyOrder') IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [IsPartyOrder];
            IF COL_LENGTH(N'[dbo].[Orders]', N'ScheduledFor') IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [ScheduledFor];
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Orders_DepositAmount') ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_DepositAmount];
            IF COL_LENGTH(N'[dbo].[Orders]', N'DepositAmount') IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [DepositAmount];
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Orders_DepositPaid')  ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_DepositPaid];
            IF COL_LENGTH(N'[dbo].[Orders]', N'DepositPaid')  IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [DepositPaid];
            IF COL_LENGTH(N'[dbo].[Orders]', N'ApprovedAt')   IS NOT NULL ALTER TABLE [dbo].[Orders] DROP COLUMN [ApprovedAt];
            """);
    }
}
