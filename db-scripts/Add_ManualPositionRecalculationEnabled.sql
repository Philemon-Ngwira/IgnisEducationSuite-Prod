-- Adds the per-school flag that gates the manual "Recalculate positions" controls on
-- Report Card Management. A SuperAdmin turns it on per school from the tenant console.
--
-- Null/false (the default) keeps a school on its existing automatic behaviour and hides the
-- buttons; true shows the recalculation controls to that school's admins.
--
-- Idempotent: safe to run more than once. Run against the IgnisEducationSuite database.
-- EF maps the School entity to dbo.Schools by convention; if your database's table is named
-- dbo.School instead, change the table name below accordingly.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Schools')
      AND name = N'ManualPositionRecalculationEnabled'
)
BEGIN
    ALTER TABLE dbo.Schools
        ADD ManualPositionRecalculationEnabled BIT NULL;
END
GO
