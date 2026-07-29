using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728170000_truong_AddDailyQuota")]
public sealed class truong_AddDailyQuota : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[DailyQuotas]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[DailyQuotas] (
                    [Id]                 int IDENTITY(1,1) NOT NULL
                                         CONSTRAINT [PK_DailyQuotas] PRIMARY KEY,
                    [MenuItemId]         int NOT NULL,
                    [DailyLimit]         int NOT NULL,
                    [ReservedQuantity]   int NOT NULL,
                    [QuotaDate]          date NOT NULL,
                    [SaleStartsAt]       time(0) NULL,
                    [SaleEndsAt]         time(0) NULL,
                    [UpdatedAt]          datetime2 NOT NULL,
                    [UpdatedByAccountId] int NULL,
                    CONSTRAINT [CK_DailyQuotas_DailyLimit] CHECK ([DailyLimit] > 0),
                    CONSTRAINT [CK_DailyQuotas_ReservedQuantity] CHECK ([ReservedQuantity] >= 0),
                    CONSTRAINT [CK_DailyQuotas_SaleWindow]
                        CHECK (([SaleStartsAt] IS NULL AND [SaleEndsAt] IS NULL)
                            OR ([SaleStartsAt] IS NOT NULL AND [SaleEndsAt] IS NOT NULL)),
                    CONSTRAINT [FK_DailyQuotas_MenuItems_MenuItemId]
                        FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems]([Id])
                        ON DELETE CASCADE,
                    CONSTRAINT [FK_DailyQuotas_Accounts_UpdatedByAccountId]
                        FOREIGN KEY ([UpdatedByAccountId]) REFERENCES [dbo].[Accounts]([Id])
                );

                CREATE UNIQUE INDEX [IX_DailyQuotas_MenuItemId]
                    ON [dbo].[DailyQuotas]([MenuItemId]);
                CREATE INDEX [IX_DailyQuotas_UpdatedByAccountId]
                    ON [dbo].[DailyQuotas]([UpdatedByAccountId]);

                INSERT INTO [dbo].[DailyQuotas]
                    ([MenuItemId], [DailyLimit], [ReservedQuantity], [QuotaDate],
                     [SaleStartsAt], [SaleEndsAt], [UpdatedAt], [UpdatedByAccountId])
                SELECT [Id], 30, 0, CONVERT(date, SYSDATETIME()),
                       NULL, NULL, SYSDATETIME(), NULL
                FROM [dbo].[MenuItems];
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[DailyQuotas]') IS NOT NULL
                DROP TABLE [dbo].[DailyQuotas];
            """);
    }
}
