using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728200000_truong_AddMenuReportSettings")]
public sealed class truong_AddMenuReportSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[StoreSettings]', N'SlowItemThreshold') IS NULL
            BEGIN
                ALTER TABLE [dbo].[StoreSettings] ADD
                    [SlowItemThreshold] int NOT NULL
                        CONSTRAINT [DF_StoreSettings_SlowItemThreshold] DEFAULT (5),
                    [BestSellerTopCount] int NOT NULL
                        CONSTRAINT [DF_StoreSettings_BestSellerTopCount] DEFAULT (3);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = N'CK_StoreSettings_SlowItemThreshold')
                ALTER TABLE [dbo].[StoreSettings]
                    ADD CONSTRAINT [CK_StoreSettings_SlowItemThreshold]
                        CHECK ([SlowItemThreshold] BETWEEN 1 AND 1000);

            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = N'CK_StoreSettings_BestSellerTopCount')
                ALTER TABLE [dbo].[StoreSettings]
                    ADD CONSTRAINT [CK_StoreSettings_BestSellerTopCount]
                        CHECK ([BestSellerTopCount] BETWEEN 1 AND 20);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.check_constraints
                       WHERE name = N'CK_StoreSettings_SlowItemThreshold')
                ALTER TABLE [dbo].[StoreSettings]
                    DROP CONSTRAINT [CK_StoreSettings_SlowItemThreshold];
            IF EXISTS (SELECT 1 FROM sys.check_constraints
                       WHERE name = N'CK_StoreSettings_BestSellerTopCount')
                ALTER TABLE [dbo].[StoreSettings]
                    DROP CONSTRAINT [CK_StoreSettings_BestSellerTopCount];

            IF EXISTS (SELECT 1 FROM sys.default_constraints
                       WHERE name = N'DF_StoreSettings_SlowItemThreshold')
                ALTER TABLE [dbo].[StoreSettings]
                    DROP CONSTRAINT [DF_StoreSettings_SlowItemThreshold];
            IF EXISTS (SELECT 1 FROM sys.default_constraints
                       WHERE name = N'DF_StoreSettings_BestSellerTopCount')
                ALTER TABLE [dbo].[StoreSettings]
                    DROP CONSTRAINT [DF_StoreSettings_BestSellerTopCount];

            IF COL_LENGTH(N'[dbo].[StoreSettings]', N'SlowItemThreshold') IS NOT NULL
                ALTER TABLE [dbo].[StoreSettings] DROP COLUMN [SlowItemThreshold];
            IF COL_LENGTH(N'[dbo].[StoreSettings]', N'BestSellerTopCount') IS NOT NULL
                ALTER TABLE [dbo].[StoreSettings] DROP COLUMN [BestSellerTopCount];
            """);
    }
}
