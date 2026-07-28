using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728190000_truong_AddIngredients")]
public sealed class truong_AddIngredients : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Ingredients]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Ingredients] (
                    [Id]                 int IDENTITY(1,1) NOT NULL
                                         CONSTRAINT [PK_Ingredients] PRIMARY KEY,
                    [Name]               nvarchar(120) NOT NULL,
                    [Status]             int NOT NULL,
                    [UpdatedAt]          datetime2 NOT NULL,
                    [UpdatedByAccountId] int NULL,
                    CONSTRAINT [CK_Ingredients_Status] CHECK ([Status] IN (0, 1, 2)),
                    CONSTRAINT [FK_Ingredients_Accounts_UpdatedByAccountId]
                        FOREIGN KEY ([UpdatedByAccountId]) REFERENCES [dbo].[Accounts]([Id])
                );

                CREATE UNIQUE INDEX [IX_Ingredients_Name] ON [dbo].[Ingredients]([Name]);
                CREATE INDEX [IX_Ingredients_Status] ON [dbo].[Ingredients]([Status]);
                CREATE INDEX [IX_Ingredients_UpdatedByAccountId]
                    ON [dbo].[Ingredients]([UpdatedByAccountId]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[DishIngredients]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[DishIngredients] (
                    [IngredientId]     int NOT NULL,
                    [MenuItemId]       int NOT NULL,
                    [DisabledMenuItem] bit NOT NULL
                        CONSTRAINT [DF_DishIngredients_DisabledMenuItem] DEFAULT (0),
                    CONSTRAINT [PK_DishIngredients]
                        PRIMARY KEY ([IngredientId], [MenuItemId]),
                    CONSTRAINT [FK_DishIngredients_Ingredients_IngredientId]
                        FOREIGN KEY ([IngredientId]) REFERENCES [dbo].[Ingredients]([Id])
                        ON DELETE CASCADE,
                    CONSTRAINT [FK_DishIngredients_MenuItems_MenuItemId]
                        FOREIGN KEY ([MenuItemId]) REFERENCES [dbo].[MenuItems]([Id])
                        ON DELETE CASCADE
                );

                CREATE INDEX [IX_DishIngredients_MenuItemId]
                    ON [dbo].[DishIngredients]([MenuItemId]);
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[RestockLogs]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[RestockLogs] (
                    [Id]             int IDENTITY(1,1) NOT NULL
                                     CONSTRAINT [PK_RestockLogs] PRIMARY KEY,
                    [IngredientId]   int NOT NULL,
                    [Action]         int NOT NULL,
                    [PreviousStatus] int NOT NULL,
                    [NewStatus]      int NOT NULL,
                    [Note]           nvarchar(500) NULL,
                    [CreatedAt]      datetime2 NOT NULL,
                    [ActorAccountId] int NOT NULL,
                    CONSTRAINT [CK_RestockLogs_Action] CHECK ([Action] IN (0, 1)),
                    CONSTRAINT [CK_RestockLogs_PreviousStatus]
                        CHECK ([PreviousStatus] IN (0, 1, 2)),
                    CONSTRAINT [CK_RestockLogs_NewStatus]
                        CHECK ([NewStatus] IN (0, 1, 2)),
                    CONSTRAINT [FK_RestockLogs_Ingredients_IngredientId]
                        FOREIGN KEY ([IngredientId]) REFERENCES [dbo].[Ingredients]([Id]),
                    CONSTRAINT [FK_RestockLogs_Accounts_ActorAccountId]
                        FOREIGN KEY ([ActorAccountId]) REFERENCES [dbo].[Accounts]([Id])
                );

                CREATE INDEX [IX_RestockLogs_IngredientId_CreatedAt]
                    ON [dbo].[RestockLogs]([IngredientId], [CreatedAt]);
                CREATE INDEX [IX_RestockLogs_ActorAccountId]
                    ON [dbo].[RestockLogs]([ActorAccountId]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[RestockLogs]') IS NOT NULL
                DROP TABLE [dbo].[RestockLogs];
            IF OBJECT_ID(N'[dbo].[DishIngredients]') IS NOT NULL
                DROP TABLE [dbo].[DishIngredients];
            IF OBJECT_ID(N'[dbo].[Ingredients]') IS NOT NULL
                DROP TABLE [dbo].[Ingredients];
            """);
    }
}
