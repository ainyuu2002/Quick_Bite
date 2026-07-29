using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728180000_truong_AddWorkSessionApprovalPayroll")]
public sealed class truong_AddWorkSessionApprovalPayroll : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Accounts]', N'HourlyRate') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Accounts]
                ADD [HourlyRate] decimal(18,0) NOT NULL
                    CONSTRAINT [DF_Accounts_HourlyRate] DEFAULT (25000);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = N'CK_Accounts_HourlyRate')
                ALTER TABLE [dbo].[Accounts]
                    ADD CONSTRAINT [CK_Accounts_HourlyRate]
                        CHECK ([HourlyRate] >= 0);
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[WorkSessions]', N'ApprovalStatus') IS NULL
            BEGIN
                ALTER TABLE [dbo].[WorkSessions] ADD
                    [ApprovalStatus] int NOT NULL
                        CONSTRAINT [DF_WorkSessions_ApprovalStatus] DEFAULT (0),
                    [ApprovedCheckInAt] datetime2 NULL,
                    [ApprovedCheckOutAt] datetime2 NULL,
                    [ApprovalNote] nvarchar(500) NULL,
                    [ApprovedHourlyRate] decimal(18,0) NULL,
                    [ApprovedAt] datetime2 NULL,
                    [ApprovedByAccountId] int NULL;
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = N'CK_WorkSessions_ApprovalStatus')
                ALTER TABLE [dbo].[WorkSessions]
                    ADD CONSTRAINT [CK_WorkSessions_ApprovalStatus]
                        CHECK ([ApprovalStatus] IN (0, 1));
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = N'FK_WorkSessions_Accounts_ApprovedByAccountId')
            BEGIN
                ALTER TABLE [dbo].[WorkSessions]
                ADD CONSTRAINT [FK_WorkSessions_Accounts_ApprovedByAccountId]
                    FOREIGN KEY ([ApprovedByAccountId])
                    REFERENCES [dbo].[Accounts]([Id]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = N'IX_WorkSessions_ApprovalStatus'
                             AND object_id = OBJECT_ID(N'[dbo].[WorkSessions]'))
            BEGIN
                CREATE INDEX [IX_WorkSessions_ApprovalStatus]
                    ON [dbo].[WorkSessions]([ApprovalStatus]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.check_constraints
                       WHERE name = N'CK_WorkSessions_ApprovalStatus')
                ALTER TABLE [dbo].[WorkSessions]
                    DROP CONSTRAINT [CK_WorkSessions_ApprovalStatus];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.foreign_keys
                       WHERE name = N'FK_WorkSessions_Accounts_ApprovedByAccountId')
                ALTER TABLE [dbo].[WorkSessions]
                    DROP CONSTRAINT [FK_WorkSessions_Accounts_ApprovedByAccountId];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = N'IX_WorkSessions_ApprovalStatus'
                         AND object_id = OBJECT_ID(N'[dbo].[WorkSessions]'))
                DROP INDEX [IX_WorkSessions_ApprovalStatus] ON [dbo].[WorkSessions];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.default_constraints
                       WHERE name = N'DF_WorkSessions_ApprovalStatus')
                ALTER TABLE [dbo].[WorkSessions]
                    DROP CONSTRAINT [DF_WorkSessions_ApprovalStatus];

            DECLARE @columns table ([Name] sysname);
            INSERT INTO @columns ([Name]) VALUES
                (N'ApprovalStatus'), (N'ApprovedCheckInAt'), (N'ApprovedCheckOutAt'),
                (N'ApprovalNote'), (N'ApprovedHourlyRate'), (N'ApprovedAt'),
                (N'ApprovedByAccountId');

            DECLARE @name sysname;
            WHILE EXISTS (SELECT 1 FROM @columns)
            BEGIN
                SELECT TOP (1) @name = [Name] FROM @columns;
                IF COL_LENGTH(N'[dbo].[WorkSessions]', @name) IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[WorkSessions] DROP COLUMN [' + @name + N']');
                DELETE FROM @columns WHERE [Name] = @name;
            END
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.check_constraints
                       WHERE name = N'CK_Accounts_HourlyRate')
                ALTER TABLE [dbo].[Accounts]
                    DROP CONSTRAINT [CK_Accounts_HourlyRate];

            IF COL_LENGTH(N'[dbo].[Accounts]', N'HourlyRate') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Accounts] DROP CONSTRAINT [DF_Accounts_HourlyRate];
                ALTER TABLE [dbo].[Accounts] DROP COLUMN [HourlyRate];
            END
            """);
    }
}
