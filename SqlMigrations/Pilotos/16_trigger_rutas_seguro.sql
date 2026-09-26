/* MANUAL: reemplazar el trigger heredado por una version set-based.
   Probar primero en una copia aislada y revisar con Distribucion.
   No completar valores ni aplicar directamente en produccion. */
DECLARE @BaseEsperada sysname=NULL;
DECLARE @HuellaOriginalEsperada varchar(64)=NULL;
DECLARE @Aplicar bit=0;
DECLARE @HuellaActual varchar(64);

IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Declarar y seleccionar el catalogo de rutas destino exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;

SELECT @HuellaActual=CONVERT(varchar(64),HASHBYTES('SHA2_256',m.definition),2)
FROM sys.triggers t
JOIN sys.sql_modules m ON m.object_id=t.object_id
WHERE t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS')
  AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS';

IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,@HuellaActual AS HuellaActual,
           @HuellaOriginalEsperada AS HuellaEsperada,
           CASE WHEN @HuellaOriginalEsperada IS NULL THEN N'COMPLETAR HUELLA Y REVISAR'
                WHEN @HuellaActual=@HuellaOriginalEsperada THEN N'LISTO PARA APLICAR'
                ELSE N'NO APLICAR: DEFINICION DIFERENTE' END AS Resultado,
           N'Reemplazar el trigger por una version set-based limitada a inserted.' AS Accion;
    RETURN;
END;

IF @HuellaOriginalEsperada IS NULL OR LEN(@HuellaOriginalEsperada)<>64
    THROW 51000,'Completar la huella SHA-256 revisada del trigger original.',1;
IF @HuellaActual IS NULL OR @HuellaActual<>@HuellaOriginalEsperada
    THROW 51000,'La definicion del trigger no coincide. No se sobrescribio.',1;
IF EXISTS
(
    SELECT 1 FROM sys.triggers t
    WHERE t.parent_id IN(OBJECT_ID(N'dbo.RT_RUTAS'),OBJECT_ID(N'dbo.RT_RUTAS_DET'))
      AND t.is_disabled=0
      AND NOT(t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS') AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS')
)
    THROW 51000,'Hay otro trigger habilitado en las tablas de rutas.',1;

SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.PortalPilotoTriggerRespaldo',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PortalPilotoTriggerRespaldo
        (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PortalPilotoTriggerRespaldo PRIMARY KEY,
            TriggerNombre sysname NOT NULL,
            Tabla sysname NOT NULL,
            Huella varchar(64) NOT NULL,
            Definicion nvarchar(max) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PortalPilotoTriggerRespaldo_Fecha DEFAULT(SYSUTCDATETIME()),
            Actor sysname NOT NULL CONSTRAINT DF_PortalPilotoTriggerRespaldo_Actor DEFAULT(ORIGINAL_LOGIN())
        );
        CREATE UNIQUE INDEX UX_PortalPilotoTriggerRespaldo_Huella
            ON dbo.PortalPilotoTriggerRespaldo(TriggerNombre,Huella);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.PortalPilotoTriggerRespaldo WITH(UPDLOCK,HOLDLOCK)
        WHERE TriggerNombre=N'INSERT_MA_CUST_ORDER_LISTAS' AND Huella=@HuellaOriginalEsperada
    )
    BEGIN
        INSERT dbo.PortalPilotoTriggerRespaldo(TriggerNombre,Tabla,Huella,Definicion)
        SELECT t.name,o.name,@HuellaOriginalEsperada,m.definition
        FROM sys.triggers t
        JOIN sys.objects o ON o.object_id=t.parent_id
        JOIN sys.sql_modules m ON m.object_id=t.object_id
        WHERE t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS') AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS';
        IF @@ROWCOUNT<>1 THROW 51000,'No se respaldo exactamente una definicion.',1;
    END;

    EXEC sys.sp_executesql N'
CREATE OR ALTER TRIGGER dbo.INSERT_MA_CUST_ORDER_LISTAS
ON dbo.RT_RUTAS
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT detalle.ROWID AS DetalleRowId,afectada.ID_RUTA,
           fuente.ID_EMPRESA,fuente.TIPO_DOC,fuente.ID_DOCUMENTO,fuente.ID_FACTURA
    INTO #AsignacionesPortalPiloto
    FROM inserted afectada
    JOIN dbo.RT_RUTAS_DET detalle ON detalle.ID_RUTA=afectada.ID_RUTA
    JOIN dbo.RT_DOC_VARIOS_ENC fuente
      ON fuente.ID_EMPRESA=detalle.ID_EMPRESA
     AND fuente.TIPO_DOC=detalle.TIPO
     AND fuente.ID_DOCUMENTO=detalle.ID_DOCUMENTO
    WHERE afectada.STATUS<>N''X''
      AND detalle.TIPO IN(N''CAMBIO'',N''ENVIO'')
      AND detalle.F_DOCTO>=CONVERT(date,N''20250701'',112)
      AND fuente.ID_FACTURA IS NOT NULL
      AND fuente.RUTA_ASIGNADA IS NULL AND fuente.STATUS=N''A'';

    IF EXISTS
    (
        SELECT 1 FROM #AsignacionesPortalPiloto
        GROUP BY DetalleRowId HAVING COUNT(*)>1
    )
        THROW 51000,''El documento CAMBIO/ENVIO tiene mas de una factura fuente.'',1;

    IF EXISTS
    (
        SELECT 1 FROM #AsignacionesPortalPiloto
        GROUP BY ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA
        HAVING MIN(ID_RUTA)<>MAX(ID_RUTA)
    )
        THROW 51000,''El documento fuente coincide con mas de una ruta modificada.'',1;

    ;WITH Asignaciones AS
    (
        SELECT DISTINCT ID_RUTA,ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA
        FROM #AsignacionesPortalPiloto
    )
    UPDATE fuente
    SET fuente.RUTA_ASIGNADA=asignacion.ID_RUTA,fuente.STATUS=N''R''
    FROM dbo.RT_DOC_VARIOS_ENC fuente
    JOIN Asignaciones asignacion
      ON asignacion.ID_EMPRESA=fuente.ID_EMPRESA
     AND asignacion.TIPO_DOC=fuente.TIPO_DOC
     AND asignacion.ID_DOCUMENTO=fuente.ID_DOCUMENTO
     AND asignacion.ID_FACTURA=fuente.ID_FACTURA
    WHERE fuente.RUTA_ASIGNADA IS NULL AND fuente.STATUS=N''A'';
END;';

    IF EXISTS
    (
        SELECT 1 FROM sys.triggers
        WHERE parent_id=OBJECT_ID(N'dbo.RT_RUTAS')
          AND name=N'INSERT_MA_CUST_ORDER_LISTAS' AND is_disabled=1
    )
        THROW 51000,'El trigger corregido quedo deshabilitado.',1;

    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;

    SELECT N'TRIGGER CORREGIDO' AS Resultado,t.name AS TriggerNombre,t.is_disabled AS Deshabilitado,
           CONVERT(varchar(64),HASHBYTES('SHA2_256',m.definition),2) AS NuevaHuella,LEN(m.definition) AS Caracteres
    FROM sys.triggers t JOIN sys.sql_modules m ON m.object_id=t.object_id
    WHERE t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS') AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
