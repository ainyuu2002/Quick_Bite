SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE ledger
SET Note = N'Đơn ' + orders.OrderCode + N' hoàn tất'
FROM dbo.PointLedgers AS ledger
INNER JOIN dbo.Orders AS orders
    ON orders.Id = ledger.OrderId
WHERE ledger.Type = 0
  AND ledger.OrderId IS NOT NULL
  AND orders.OrderCode IS NOT NULL
  AND ledger.Note <> N'Đơn ' + orders.OrderCode + N' hoàn tất';

COMMIT TRANSACTION;
