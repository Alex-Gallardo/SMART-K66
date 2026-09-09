/* =============================================================================
   BORRADORES NC — HABILITAR CONCEPTO DESCUENTO AUTORIZADO
   Destino autorizado: POS-SmartK66

   Alcance
   -------
   - Actualiza CK_BND_CONCEPTO para admitir DESCUENTO AUTORIZADO.
   - No modifica ni elimina datos existentes.
   - Es transaccional y puede ejecutarse nuevamente de forma segura.

   Ejecute el archivo COMPLETO en una ventana nueva de SSMS.
   ============================================================================= */
USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

EXEC sys.sp_set_session_context
     @key=N'BorradorNcDescuentoAutorizadoValidado', @value=0;
GO

IF DB_NAME() <> N'POS-SmartK66'
    THROW 57000, 'SEGURIDAD: script autorizado únicamente para POS-SmartK66.', 1;

IF CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) <> N'READ_WRITE'
    THROW 57001, 'La base autorizada no está disponible para escritura.', 1;

IF OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NULL
    THROW 57002, 'No existe dbo.BORR_NC_DET.', 1;

IF COL_LENGTH(N'dbo.BORR_NC_DET', N'CONCEPTO') IS NULL
    THROW 57003, 'No existe dbo.BORR_NC_DET.CONCEPTO.', 1;

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id=OBJECT_ID(N'dbo.BORR_NC_DET')
      AND name=N'CONCEPTO'
      AND (TYPE_NAME(system_type_id)<>N'nvarchar'
           OR (max_length<>-1 AND max_length<40))
)
    THROW 57003, 'dbo.BORR_NC_DET.CONCEPTO no es nvarchar(20) o compatible.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.BORR_NC_DET
    WHERE CONCEPTO NOT IN
        (N'DEVOLUCION', N'DESCUENTO', N'DESCUENTO AUTORIZADO', N'OTROS')
)
    THROW 57004, 'Existen conceptos fuera del catálogo autorizado; no se modificó la restricción.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult=sys.sp_getapplock
         @Resource=N'BorradorNc.Concepto.DescuentoAutorizado',
         @LockMode=N'Exclusive',
         @LockOwner=N'Transaction',
         @LockTimeout=5000;

    IF @LockResult < 0
        THROW 57005, 'No fue posible obtener el bloqueo de migración.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.objects
        WHERE name=N'CK_BND_CONCEPTO'
          AND NOT (parent_object_id=OBJECT_ID(N'dbo.BORR_NC_DET') AND type=N'C')
    )
        THROW 57006, 'CK_BND_CONCEPTO existe asociado a otro objeto o con otro tipo.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id=OBJECT_ID(N'dbo.BORR_NC_DET')
          AND name=N'CK_BND_CONCEPTO'
    )
        ALTER TABLE dbo.BORR_NC_DET DROP CONSTRAINT CK_BND_CONCEPTO;

    ALTER TABLE dbo.BORR_NC_DET WITH CHECK
        ADD CONSTRAINT CK_BND_CONCEPTO
        CHECK (CONCEPTO IN
            (N'DEVOLUCION', N'DESCUENTO', N'DESCUENTO AUTORIZADO', N'OTROS'));

    ALTER TABLE dbo.BORR_NC_DET CHECK CONSTRAINT CK_BND_CONCEPTO;

    EXEC sys.sp_set_session_context
         @key=N'BorradorNcDescuentoAutorizadoValidado', @value=1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

IF TRY_CONVERT(int, SESSION_CONTEXT(N'BorradorNcDescuentoAutorizadoValidado')) <> 1
    THROW 57007, 'La migración no terminó correctamente; no se emitirán resultados de éxito.', 1;

SELECT
    N'07A_ENTORNO' AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME() AS BASE_ACTUAL,
    SYSDATETIME() AS FECHA_EJECUCION;

SELECT
    N'07B_RESTRICCION' AS SECCION,
    C.name AS RESTRICCION,
    C.is_disabled AS DESHABILITADA,
    C.is_not_trusted AS NO_CONFIABLE,
    C.definition AS DEFINICION
FROM sys.check_constraints C
WHERE C.parent_object_id=OBJECT_ID(N'dbo.BORR_NC_DET')
  AND C.name=N'CK_BND_CONCEPTO';

SELECT
    N'07C_RESUMEN' AS SECCION,
    N'CK_BND_CONCEPTO habilitada y confiable' AS VALIDACION,
    CONVERT(bigint, 1) AS ESPERADO,
    CONVERT(bigint, COUNT_BIG(*)) AS REAL,
    CASE WHEN COUNT_BIG(*)=1 THEN N'OK' ELSE N'REVISAR' END AS RESULTADO
FROM sys.check_constraints C
WHERE C.parent_object_id=OBJECT_ID(N'dbo.BORR_NC_DET')
  AND C.name=N'CK_BND_CONCEPTO'
  AND C.is_disabled=0
  AND C.is_not_trusted=0
  AND C.definition LIKE N'%DESCUENTO AUTORIZADO%';
GO
