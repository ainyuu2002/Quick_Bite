using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260721173000_AddPaymentMethod")]
public sealed class AddPaymentMethod : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'PaymentMethod') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders]
                ADD [PaymentMethod] int NOT NULL
                    CONSTRAINT [DF_Orders_PaymentMethod] DEFAULT (0);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'PaymentMethod') IS NOT NULL
            BEGIN
                DECLARE @constraintName sysname;
                SELECT @constraintName = dc.name
                FROM sys.default_constraints AS dc
                INNER JOIN sys.columns AS c
                    ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Orders]')
                  AND c.name = N'PaymentMethod';

                IF @constraintName IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @constraintName + N']');

                ALTER TABLE [dbo].[Orders] DROP COLUMN [PaymentMethod];
            END
            """);
    }
}
