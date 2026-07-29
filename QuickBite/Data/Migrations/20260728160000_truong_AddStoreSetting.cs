using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728160000_truong_AddStoreSetting")]
public sealed class truong_AddStoreSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[StoreSettings]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StoreSettings] (
                    [Id]                 int NOT NULL
                                         CONSTRAINT [PK_StoreSettings] PRIMARY KEY,
                    [OpensAt]            time(0) NOT NULL,
                    [ClosesAt]           time(0) NOT NULL,
                    [IsPaused]           bit NOT NULL,
                    [PauseReason]        nvarchar(200) NULL,
                    [UpdatedAt]          datetime2 NOT NULL,
                    [UpdatedByAccountId] int NULL,
                    CONSTRAINT [CK_StoreSettings_Singleton] CHECK ([Id] = 1),
                    CONSTRAINT [FK_StoreSettings_Accounts_UpdatedByAccountId]
                        FOREIGN KEY ([UpdatedByAccountId]) REFERENCES [dbo].[Accounts]([Id])
                );

                CREATE INDEX [IX_StoreSettings_UpdatedByAccountId]
                    ON [dbo].[StoreSettings]([UpdatedByAccountId]);

                INSERT INTO [dbo].[StoreSettings]
                    ([Id], [OpensAt], [ClosesAt], [IsPaused], [PauseReason],
                     [UpdatedAt], [UpdatedByAccountId])
                VALUES
                    (1, '08:00', '22:00', 0, NULL, SYSDATETIME(), NULL);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[StoreSettings]') IS NOT NULL
                DROP TABLE [dbo].[StoreSettings];
            """);
    }
}
