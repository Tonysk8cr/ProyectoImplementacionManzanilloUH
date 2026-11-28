-- Compatibilidad de citas/recordatorios sobre el esquema existente de SQL Server/Somee.
-- No crea datos de prueba ni modifica citas. Ejecutar por el administrador de la base.
-- Para bases administradas con EF Migrations, utilizar las migraciones en su lugar.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
    THROW 50001, 'Falta la tabla Appointments. Inicialice primero el esquema base.', 1;
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
    THROW 50002, 'Falta la tabla Notifications. Inicialice primero el esquema base.', 1;

IF COL_LENGTH(N'dbo.Appointments', N'Reason') IS NULL
    ALTER TABLE dbo.Appointments ADD Reason nvarchar(500) NOT NULL
        CONSTRAINT DF_Appointments_Reason DEFAULT N'Consulta veterinaria';

IF COL_LENGTH(N'dbo.Notifications', N'EventAt') IS NULL
    ALTER TABLE dbo.Notifications ADD EventAt datetimeoffset NULL;
IF COL_LENGTH(N'dbo.Notifications', N'NotBefore') IS NULL
    ALTER TABLE dbo.Notifications ADD NotBefore datetimeoffset NULL;

COMMIT TRANSACTION;
