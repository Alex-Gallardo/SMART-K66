/* =============================================================================
   COTIZACIONES — HABILITAR PROSPECTOS SIN CÓDIGO SAP
   Destino autorizado: POS-SmartK66 (PRUEBAS)
   Reejecutable, sin DROP/DELETE y con rollback ante errores.

   Único cambio: COT_ENC.ID_CLIENTE pasa de NOT NULL a NULL. El nombre del
   cliente/prospecto continúa siendo obligatorio por la estructura existente.
   ============================================================================= */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'POS-SmartK66'
    THROW 53400, 'SEGURIDAD: este script solo se ejecuta en POS-SmartK66.', 1;

IF CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) <> N'READ_WRITE'
    THROW 53401, 'La base de pruebas no está disponible para escritura.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.COT_ENC', N'U') IS NULL
        THROW 53402, 'No existe dbo.COT_ENC. Ejecute primero 01_crear_estructura_cotizaciones.sql.', 1;

    IF COL_LENGTH(N'dbo.COT_ENC', N'ID_CLIENTE') IS NULL
        THROW 53403, 'No existe dbo.COT_ENC.ID_CLIENTE.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.columns C
        JOIN sys.types T ON T.user_type_id = C.user_type_id
        WHERE C.object_id = OBJECT_ID(N'dbo.COT_ENC')
          AND C.name = N'ID_CLIENTE'
          AND T.name = N'nvarchar'
          AND C.max_length = 40
    )
        THROW 53404, 'dbo.COT_ENC.ID_CLIENTE no tiene el tipo esperado nvarchar(20). No se aplicaron cambios.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.COT_ENC')
          AND name = N'ID_CLIENTE'
          AND is_nullable = 0
    )
        ALTER TABLE dbo.COT_ENC
            ALTER COLUMN ID_CLIENTE nvarchar(20) NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.COT_ENC')
          AND name = N'ID_CLIENTE'
          AND is_nullable = 1
    )
        THROW 53405, 'No fue posible habilitar valores NULL en dbo.COT_ENC.ID_CLIENTE.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT N'PROSPECTOS' AS SECCION,
       S.name AS ESQUEMA,
       O.name AS TABLA,
       C.name AS COLUMNA,
       T.name AS TIPO,
       C.max_length / 2 AS LONGITUD,
       C.is_nullable,
       CASE WHEN C.is_nullable = 1 THEN N'LISTO' ELSE N'REVISAR' END AS ESTADO
FROM sys.objects O
JOIN sys.schemas S ON S.schema_id = O.schema_id
JOIN sys.columns C ON C.object_id = O.object_id
JOIN sys.types T ON T.user_type_id = C.user_type_id
WHERE O.object_id = OBJECT_ID(N'dbo.COT_ENC')
  AND C.name = N'ID_CLIENTE';
GO
