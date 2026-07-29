SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE dbo.Accounts
SET Role = 0
WHERE Username = N'manager'
  AND Role = 4;

SELECT Id, Username, Role, IsActive
FROM dbo.Accounts
WHERE Username = N'manager';

COMMIT TRANSACTION;
