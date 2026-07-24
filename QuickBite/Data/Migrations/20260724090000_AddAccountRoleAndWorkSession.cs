using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

/// <summary>
/// Đổi tên AdminUsers → Accounts, thêm phân quyền + chấm công + người nhận đơn.
/// <para>
/// Viết tay (không dùng Add-Migration) vì project chưa có ModelSnapshot —
/// Add-Migration sẽ tưởng DB rỗng và sinh CreateTable cho toàn bộ bảng.
/// Mọi câu lệnh đều idempotent để chạy được cả trên DB cũ lẫn DB mới tạo từ script.sql.
/// </para>
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260724090000_AddAccountRoleAndWorkSession")]
public sealed class AddAccountRoleAndWorkSession : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. AdminUsers -> Accounts (bỏ qua nếu script.sql đã tạo sẵn bảng Accounts)
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[AdminUsers]') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[Accounts]') IS NULL
            BEGIN
                EXEC sp_rename N'[dbo].[AdminUsers]', N'Accounts';
            END
            """);

        // 2. Phân quyền. Mặc định DB là Staff (1) — tài khoản tạo sai sót sẽ là
        //    quyền thấp nhất, không phải quyền cao nhất.
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Accounts]', N'Role') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Accounts]
                ADD [Role] int NOT NULL
                    CONSTRAINT [DF_Accounts_Role] DEFAULT (1);
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Accounts]', N'IsActive') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Accounts]
                ADD [IsActive] bit NOT NULL
                    CONSTRAINT [DF_Accounts_IsActive] DEFAULT (1);
            END
            """);

        // Tài khoản seed sẵn là chủ quán.
        migrationBuilder.Sql(
            "UPDATE [dbo].[Accounts] SET [Role] = 0 WHERE [Username] = N'admin';");

        // 3. Ai nhận đơn
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'AcceptedByAccountId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] ADD [AcceptedByAccountId] int NULL;
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'AcceptedAt') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] ADD [AcceptedAt] datetime2 NULL;
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = N'FK_Orders_Accounts_AcceptedByAccountId')
            BEGIN
                ALTER TABLE [dbo].[Orders]
                ADD CONSTRAINT [FK_Orders_Accounts_AcceptedByAccountId]
                    FOREIGN KEY ([AcceptedByAccountId])
                    REFERENCES [dbo].[Accounts]([Id]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = N'IX_Orders_AcceptedByAccountId'
                             AND object_id = OBJECT_ID(N'[dbo].[Orders]'))
            BEGIN
                CREATE INDEX [IX_Orders_AcceptedByAccountId]
                    ON [dbo].[Orders]([AcceptedByAccountId]);
            END
            """);

        // 4. Chấm công
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[WorkSessions]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[WorkSessions] (
                    [Id]         int IDENTITY(1,1) NOT NULL
                                 CONSTRAINT [PK_WorkSessions] PRIMARY KEY,
                    [AccountId]  int NOT NULL,
                    [CheckInAt]  datetime2 NOT NULL,
                    [CheckOutAt] datetime2 NULL,
                    CONSTRAINT [FK_WorkSessions_Accounts_AccountId]
                        FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts]([Id])
                );

                CREATE INDEX [IX_WorkSessions_AccountId_CheckInAt]
                    ON [dbo].[WorkSessions]([AccountId], [CheckInAt]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[WorkSessions]') IS NOT NULL
                DROP TABLE [dbo].[WorkSessions];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.foreign_keys
                       WHERE name = N'FK_Orders_Accounts_AcceptedByAccountId')
                ALTER TABLE [dbo].[Orders]
                    DROP CONSTRAINT [FK_Orders_Accounts_AcceptedByAccountId];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = N'IX_Orders_AcceptedByAccountId'
                         AND object_id = OBJECT_ID(N'[dbo].[Orders]'))
                DROP INDEX [IX_Orders_AcceptedByAccountId] ON [dbo].[Orders];
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'AcceptedByAccountId') IS NOT NULL
                ALTER TABLE [dbo].[Orders] DROP COLUMN [AcceptedByAccountId];
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'AcceptedAt') IS NOT NULL
                ALTER TABLE [dbo].[Orders] DROP COLUMN [AcceptedAt];
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Accounts]', N'Role') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Accounts] DROP CONSTRAINT [DF_Accounts_Role];
                ALTER TABLE [dbo].[Accounts] DROP COLUMN [Role];
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Accounts]', N'IsActive') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Accounts] DROP CONSTRAINT [DF_Accounts_IsActive];
                ALTER TABLE [dbo].[Accounts] DROP COLUMN [IsActive];
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Accounts]') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[AdminUsers]') IS NULL
            BEGIN
                EXEC sp_rename N'[dbo].[Accounts]', N'AdminUsers';
            END
            """);
    }
}
