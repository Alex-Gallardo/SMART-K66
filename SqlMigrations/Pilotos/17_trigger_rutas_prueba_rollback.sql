/* QA: prueba la asignacion CAMBIO/ENVIO del trigger y revierte todo.
   Ejecutar exclusivamente en una copia aislada, sin usuarios concurrentes. */
SET NOCOUNT ON;
DECLARE @BaseEsperada sysname=NULL;
DECLARE @HuellaEsperada varchar(64)=NULL;
DECLARE @ConfirmarCopiaAislada nvarchar(20)=NULL; -- Escribir: COPIA AISLADA
DECLARE @HuellaActual varchar(64),@RutaId nvarchar(15);

IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada OR @ConfirmarCopiaAislada<>N'COPIA AISLADA'
    THROW 51000,'Declarar la copia aislada y confirmar su uso.',1;
IF @HuellaEsperada IS NULL OR LEN(@HuellaEsperada)<>64
    THROW 51000,'Completar la huella del trigger corregido.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;

SELECT @HuellaActual=CONVERT(varchar(64),HASHBYTES('SHA2_256',m.definition),2)
FROM sys.triggers t JOIN sys.sql_modules m ON m.object_id=t.object_id
WHERE t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS') AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS' AND t.is_disabled=0;
IF @HuellaActual<>@HuellaEsperada OR @HuellaActual IS NULL
    THROW 51000,'El trigger corregido no coincide o esta deshabilitado.',1;

;WITH RutasCandidatas AS
(
    SELECT r.ID_RUTA,r.FECHA_RUTA
    FROM dbo.RT_RUTAS r
    WHERE r.STATUS<>N'X'
      AND EXISTS
      (
          SELECT 1 FROM dbo.RT_RUTAS_DET d
          JOIN dbo.RT_DOC_VARIOS_ENC f
            ON f.ID_EMPRESA=d.ID_EMPRESA AND f.TIPO_DOC=d.TIPO AND f.ID_DOCUMENTO=d.ID_DOCUMENTO
          WHERE d.ID_RUTA=r.ID_RUTA AND d.TIPO IN(N'CAMBIO',N'ENVIO')
            AND d.F_DOCTO>=CONVERT(date,N'20250701',112) AND f.ID_FACTURA IS NOT NULL
      )
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.RT_RUTAS_DET d
          CROSS APPLY
          (
              SELECT COUNT(*) AS Coincidencias FROM dbo.RT_DOC_VARIOS_ENC f
              WHERE f.ID_EMPRESA=d.ID_EMPRESA AND f.TIPO_DOC=d.TIPO
                AND f.ID_DOCUMENTO=d.ID_DOCUMENTO AND f.ID_FACTURA IS NOT NULL
          ) c
          WHERE d.ID_RUTA=r.ID_RUTA AND d.TIPO IN(N'CAMBIO',N'ENVIO')
            AND d.F_DOCTO>=CONVERT(date,N'20250701',112) AND c.Coincidencias>1
      )
)
SELECT TOP (1) @RutaId=ID_RUTA FROM RutasCandidatas ORDER BY FECHA_RUTA DESC,ID_RUTA DESC;
IF @RutaId IS NULL THROW 51000,'No se encontro una ruta CAMBIO/ENVIO sin ambiguedades.',1;

SELECT MIN(d.ROWID) AS DetalleRowId,@RutaId AS ID_RUTA,
       f.ID_EMPRESA,f.TIPO_DOC,f.ID_DOCUMENTO,f.ID_FACTURA,
       f.STATUS AS EstadoOriginal,f.RUTA_ASIGNADA AS RutaOriginal
INTO #FuentesEsperadas
FROM dbo.RT_RUTAS_DET d JOIN dbo.RT_DOC_VARIOS_ENC f
  ON f.ID_EMPRESA=d.ID_EMPRESA AND f.TIPO_DOC=d.TIPO AND f.ID_DOCUMENTO=d.ID_DOCUMENTO
WHERE d.ID_RUTA=@RutaId AND d.TIPO IN(N'CAMBIO',N'ENVIO')
  AND d.F_DOCTO>=CONVERT(date,N'20250701',112) AND f.ID_FACTURA IS NOT NULL
GROUP BY f.ID_EMPRESA,f.TIPO_DOC,f.ID_DOCUMENTO,f.ID_FACTURA,f.STATUS,f.RUTA_ASIGNADA;

SELECT N'CANDIDATO' AS Seccion,@RutaId AS ID_RUTA,COUNT(*) AS DocumentosFuente,@HuellaActual AS HuellaTrigger
FROM #FuentesEsperadas;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    SELECT f.ID_EMPRESA FROM dbo.RT_DOC_VARIOS_ENC f WITH(UPDLOCK,HOLDLOCK)
    JOIN #FuentesEsperadas e ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
    UPDATE f SET f.STATUS=N'A',f.RUTA_ASIGNADA=NULL
    FROM dbo.RT_DOC_VARIOS_ENC f JOIN #FuentesEsperadas e
      ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
    IF @@ROWCOUNT<>(SELECT COUNT(*) FROM #FuentesEsperadas)
        THROW 51000,'No se preparo exactamente el conjunto esperado.',1;
    UPDATE dbo.RT_RUTAS SET OBSR_GENERAL=OBSR_GENERAL WHERE ID_RUTA=@RutaId;
    IF @@ROWCOUNT<>1 THROW 51000,'No se actualizo exactamente una ruta.',1;
    IF EXISTS
    (
        SELECT 1 FROM #FuentesEsperadas e LEFT JOIN dbo.RT_DOC_VARIOS_ENC f
          ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
         AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA
        WHERE f.ID_DOCUMENTO IS NULL OR f.STATUS<>N'R' OR f.RUTA_ASIGNADA<>@RutaId
    ) THROW 51000,'El trigger no asigno todas las fuentes esperadas.',1;
    SELECT N'DENTRO_TRANSACCION' AS Seccion,@RutaId AS ID_RUTA,f.ID_EMPRESA,f.TIPO_DOC,
           f.ID_DOCUMENTO,f.ID_FACTURA,f.STATUS,f.RUTA_ASIGNADA,N'TRIGGER APLICO CORRECTAMENTE' AS Resultado
    FROM dbo.RT_DOC_VARIOS_ENC f JOIN #FuentesEsperadas e
      ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
    ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    IF EXISTS
    (
        SELECT 1 FROM #FuentesEsperadas e LEFT JOIN dbo.RT_DOC_VARIOS_ENC f
          ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
         AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA
        WHERE f.ID_DOCUMENTO IS NULL
           OR ISNULL(f.STATUS,N'<NULL>')<>ISNULL(e.EstadoOriginal,N'<NULL>')
           OR ISNULL(f.RUTA_ASIGNADA,N'<NULL>')<>ISNULL(e.RutaOriginal,N'<NULL>')
    ) THROW 51000,'El rollback no restauro los documentos fuente.',1;
    SELECT N'DESPUES_ROLLBACK' AS Seccion,@RutaId AS ID_RUTA,f.ID_EMPRESA,f.TIPO_DOC,
           f.ID_DOCUMENTO,f.ID_FACTURA,f.STATUS,f.RUTA_ASIGNADA,N'SIN CAMBIOS PERSISTENTES' AS Resultado
    FROM dbo.RT_DOC_VARIOS_ENC f JOIN #FuentesEsperadas e
      ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
