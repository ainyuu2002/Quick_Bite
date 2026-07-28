using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728100000_TungCustomerRetention")]
public sealed class TungCustomerRetention : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Customers]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Customers] (
                    [Id]              int IDENTITY(1,1) NOT NULL
                                      CONSTRAINT [PK_Customers] PRIMARY KEY,
                    [Phone]           nvarchar(11) NOT NULL,
                    [PasswordHash]    nvarchar(max) NOT NULL,
                    [FullName]        nvarchar(100) NOT NULL,
                    [SavedAddress]    nvarchar(500) NULL,
                    [IsPhoneVerified] bit NOT NULL CONSTRAINT [DF_Customers_IsPhoneVerified] DEFAULT (0),
                    [CreatedAt]       datetime2 NOT NULL
                );

                CREATE UNIQUE INDEX [IX_Customers_Phone] ON [dbo].[Customers]([Phone]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[OtpVerifications]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OtpVerifications] (
                    [Id]             int IDENTITY(1,1) NOT NULL
                                     CONSTRAINT [PK_OtpVerifications] PRIMARY KEY,
                    [Phone]          nvarchar(11) NOT NULL,
                    [Code]           nvarchar(6) NOT NULL,
                    [Purpose]        int NOT NULL,
                    [CreatedAt]      datetime2 NOT NULL,
                    [ExpiresAt]      datetime2 NOT NULL,
                    [ConsumedAt]     datetime2 NULL,
                    [FailedAttempts] int NOT NULL CONSTRAINT [DF_OtpVerifications_FailedAttempts] DEFAULT (0)
                );

                CREATE INDEX [IX_OtpVerifications_Phone_Purpose_CreatedAt]
                    ON [dbo].[OtpVerifications]([Phone], [Purpose], [CreatedAt]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Promotions]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Promotions] (
                    [Id]                int IDENTITY(1,1) NOT NULL
                                        CONSTRAINT [PK_Promotions] PRIMARY KEY,
                    [Code]              nvarchar(30) NOT NULL,
                    [Description]       nvarchar(200) NULL,
                    [DiscountType]      int NOT NULL,
                    [DiscountValue]     decimal(18,0) NOT NULL,
                    [MaxDiscountAmount] decimal(18,0) NULL,
                    [MinOrderTotal]     decimal(18,0) NOT NULL CONSTRAINT [DF_Promotions_MinOrderTotal] DEFAULT (0),
                    [StartsAt]          datetime2 NOT NULL,
                    [EndsAt]            datetime2 NOT NULL,
                    [TotalUsageLimit]   int NULL,
                    [PerPhoneLimit]     int NOT NULL CONSTRAINT [DF_Promotions_PerPhoneLimit] DEFAULT (1),
                    [IsActive]          bit NOT NULL CONSTRAINT [DF_Promotions_IsActive] DEFAULT (1),
                    [CreatedAt]         datetime2 NOT NULL
                );

                CREATE UNIQUE INDEX [IX_Promotions_Code] ON [dbo].[Promotions]([Code]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[PromotionUsages]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PromotionUsages] (
                    [Id]          int IDENTITY(1,1) NOT NULL
                                  CONSTRAINT [PK_PromotionUsages] PRIMARY KEY,
                    [PromotionId] int NOT NULL,
                    [Phone]       nvarchar(11) NOT NULL,
                    [OrderId]     int NOT NULL,
                    [UsedAt]      datetime2 NOT NULL,
                    [RefundedAt]  datetime2 NULL,
                    CONSTRAINT [FK_PromotionUsages_Promotions_PromotionId]
                        FOREIGN KEY ([PromotionId]) REFERENCES [dbo].[Promotions]([Id])
                );

                CREATE INDEX [IX_PromotionUsages_PromotionId_Phone]
                    ON [dbo].[PromotionUsages]([PromotionId], [Phone]);
                CREATE INDEX [IX_PromotionUsages_OrderId]
                    ON [dbo].[PromotionUsages]([OrderId]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Vouchers]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Vouchers] (
                    [Id]                int IDENTITY(1,1) NOT NULL
                                        CONSTRAINT [PK_Vouchers] PRIMARY KEY,
                    [Code]              nvarchar(20) NOT NULL,
                    [CustomerId]        int NOT NULL,
                    [DiscountPercent]   int NOT NULL,
                    [MaxDiscountAmount] decimal(18,0) NOT NULL,
                    [PointsSpent]       int NOT NULL,
                    [CreatedAt]         datetime2 NOT NULL,
                    [ExpiresAt]         datetime2 NOT NULL,
                    [UsedAt]            datetime2 NULL,
                    [UsedOrderId]       int NULL,
                    CONSTRAINT [FK_Vouchers_Customers_CustomerId]
                        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id])
                );

                CREATE UNIQUE INDEX [IX_Vouchers_Code] ON [dbo].[Vouchers]([Code]);
                CREATE INDEX [IX_Vouchers_CustomerId] ON [dbo].[Vouchers]([CustomerId]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[PointLedgers]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PointLedgers] (
                    [Id]         int IDENTITY(1,1) NOT NULL
                                 CONSTRAINT [PK_PointLedgers] PRIMARY KEY,
                    [CustomerId] int NOT NULL,
                    [Points]     int NOT NULL,
                    [Type]       int NOT NULL,
                    [OrderId]    int NULL,
                    [VoucherId]  int NULL,
                    [Note]       nvarchar(200) NULL,
                    [CreatedAt]  datetime2 NOT NULL,
                    CONSTRAINT [FK_PointLedgers_Customers_CustomerId]
                        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id])
                );

                CREATE INDEX [IX_PointLedgers_CustomerId_CreatedAt]
                    ON [dbo].[PointLedgers]([CustomerId], [CreatedAt]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Complaints]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Complaints] (
                    [Id]          int IDENTITY(1,1) NOT NULL
                                  CONSTRAINT [PK_Complaints] PRIMARY KEY,
                    [OrderId]     int NOT NULL,
                    [Phone]       nvarchar(11) NOT NULL,
                    [Category]    int NOT NULL,
                    [Description] nvarchar(1000) NOT NULL,
                    [Status]      int NOT NULL CONSTRAINT [DF_Complaints_Status] DEFAULT (0),
                    [Resolution]  nvarchar(1000) NULL,
                    [CreatedAt]   datetime2 NOT NULL,
                    [ResolvedAt]  datetime2 NULL,
                    CONSTRAINT [FK_Complaints_Orders_OrderId]
                        FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id])
                );

                CREATE UNIQUE INDEX [IX_Complaints_OrderId] ON [dbo].[Complaints]([OrderId]);
                CREATE INDEX [IX_Complaints_Status] ON [dbo].[Complaints]([Status]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[InternalRatings]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[InternalRatings] (
                    [Id]        int IDENTITY(1,1) NOT NULL
                                CONSTRAINT [PK_InternalRatings] PRIMARY KEY,
                    [OrderId]   int NOT NULL,
                    [Phone]     nvarchar(11) NOT NULL,
                    [Stars]     int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [FK_InternalRatings_Orders_OrderId]
                        FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id])
                );

                CREATE UNIQUE INDEX [IX_InternalRatings_OrderId] ON [dbo].[InternalRatings]([OrderId]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'DiscountAmount') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders]
                ADD [DiscountAmount] decimal(18,0) NOT NULL
                    CONSTRAINT [DF_Orders_DiscountAmount] DEFAULT (0);
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'CustomerId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] ADD [CustomerId] int NULL;
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'PromotionId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] ADD [PromotionId] int NULL;
            END
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'VoucherId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] ADD [VoucherId] int NULL;
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = N'FK_Orders_Customers_CustomerId')
            BEGIN
                ALTER TABLE [dbo].[Orders]
                ADD CONSTRAINT [FK_Orders_Customers_CustomerId]
                    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = N'FK_Orders_Promotions_PromotionId')
            BEGIN
                ALTER TABLE [dbo].[Orders]
                ADD CONSTRAINT [FK_Orders_Promotions_PromotionId]
                    FOREIGN KEY ([PromotionId]) REFERENCES [dbo].[Promotions]([Id]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                           WHERE name = N'FK_Orders_Vouchers_VoucherId')
            BEGIN
                ALTER TABLE [dbo].[Orders]
                ADD CONSTRAINT [FK_Orders_Vouchers_VoucherId]
                    FOREIGN KEY ([VoucherId]) REFERENCES [dbo].[Vouchers]([Id]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = N'IX_Orders_CustomerId'
                             AND object_id = OBJECT_ID(N'[dbo].[Orders]'))
            BEGIN
                CREATE INDEX [IX_Orders_CustomerId] ON [dbo].[Orders]([CustomerId]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Orders_Vouchers_VoucherId')
                ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [FK_Orders_Vouchers_VoucherId];
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Orders_Promotions_PromotionId')
                ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [FK_Orders_Promotions_PromotionId];
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Orders_Customers_CustomerId')
                ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [FK_Orders_Customers_CustomerId];
            IF EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = N'IX_Orders_CustomerId'
                         AND object_id = OBJECT_ID(N'[dbo].[Orders]'))
                DROP INDEX [IX_Orders_CustomerId] ON [dbo].[Orders];
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'[dbo].[Orders]', N'VoucherId') IS NOT NULL
                ALTER TABLE [dbo].[Orders] DROP COLUMN [VoucherId];
            IF COL_LENGTH(N'[dbo].[Orders]', N'PromotionId') IS NOT NULL
                ALTER TABLE [dbo].[Orders] DROP COLUMN [PromotionId];
            IF COL_LENGTH(N'[dbo].[Orders]', N'CustomerId') IS NOT NULL
                ALTER TABLE [dbo].[Orders] DROP COLUMN [CustomerId];
            IF COL_LENGTH(N'[dbo].[Orders]', N'DiscountAmount') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [DF_Orders_DiscountAmount];
                ALTER TABLE [dbo].[Orders] DROP COLUMN [DiscountAmount];
            END
            """);

        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[InternalRatings]') IS NOT NULL DROP TABLE [dbo].[InternalRatings];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[Complaints]') IS NOT NULL DROP TABLE [dbo].[Complaints];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[PointLedgers]') IS NOT NULL DROP TABLE [dbo].[PointLedgers];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[Vouchers]') IS NOT NULL DROP TABLE [dbo].[Vouchers];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[PromotionUsages]') IS NOT NULL DROP TABLE [dbo].[PromotionUsages];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[Promotions]') IS NOT NULL DROP TABLE [dbo].[Promotions];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[OtpVerifications]') IS NOT NULL DROP TABLE [dbo].[OtpVerifications];");
        migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[Customers]') IS NOT NULL DROP TABLE [dbo].[Customers];");
    }
}
