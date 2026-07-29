SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Today date = CONVERT(date, GETDATE());

BEGIN TRANSACTION;

;WITH OpenQuantities AS
(
    SELECT
        items.MenuItemId,
        SUM(items.Quantity) AS ReservedQuantity
    FROM dbo.Orders AS orders
    INNER JOIN dbo.OrderItems AS items
        ON items.OrderId = orders.Id
    WHERE CONVERT(date, orders.CreatedAt) = @Today
      AND orders.Status NOT IN (5, 6, 8)
    GROUP BY items.MenuItemId
)
INSERT INTO dbo.DailyQuotas
(
    MenuItemId,
    DailyLimit,
    ReservedQuantity,
    QuotaDate,
    UpdatedAt
)
SELECT
    quantities.MenuItemId,
    30,
    quantities.ReservedQuantity,
    @Today,
    GETDATE()
FROM OpenQuantities AS quantities
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.DailyQuotas AS quota
    WHERE quota.MenuItemId = quantities.MenuItemId
);

;WITH OpenQuantities AS
(
    SELECT
        items.MenuItemId,
        SUM(items.Quantity) AS ReservedQuantity
    FROM dbo.Orders AS orders
    INNER JOIN dbo.OrderItems AS items
        ON items.OrderId = orders.Id
    WHERE CONVERT(date, orders.CreatedAt) = @Today
      AND orders.Status NOT IN (5, 6, 8)
    GROUP BY items.MenuItemId
)
UPDATE quota
SET
    quota.QuotaDate = @Today,
    quota.ReservedQuantity = COALESCE(quantities.ReservedQuantity, 0),
    quota.UpdatedAt = GETDATE()
FROM dbo.DailyQuotas AS quota
LEFT JOIN OpenQuantities AS quantities
    ON quantities.MenuItemId = quota.MenuItemId;

COMMIT TRANSACTION;
