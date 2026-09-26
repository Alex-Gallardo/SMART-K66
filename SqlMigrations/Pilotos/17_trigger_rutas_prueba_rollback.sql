/* QA: comprueba dos rutas, fuentes pendientes/protegidas y rutas ajenas; revierte todo.
   Ejecutar exclusivamente en una copia aislada, sin usuarios ni integraciones concurrentes. */
SET NOCOUNT ON;
DECLARE @BaseEsperada sysname=N'APP_TEST';
DECLARE @HuellaEsperada varchar(64)=NULL;
DECLARE @ConfirmarCopiaAislada nvarchar(20)=NULL; -- Escribir: COPIA AISLADA
DECLARE @HuellaActual varchar(64);

IF @BaseEsperada<>N'APP_TEST' OR DB_NAME()<>N'APP_TEST' OR ISNULL(@ConfirmarCopiaAislada,N'')<>N'COPIA AISLADA'
    THROW 51000,'Esta prueba requiere APP_TEST aislada y su confirmacion explicita. No ejecutar en APK66.',1;
IF @HuellaEsperada IS NULL OR LEN(@HuellaEsperada)<>64
    THROW 51000,'Completar la huella del trigger corregido.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;

SELECT @HuellaActual=CONVERT(varchar(64),HASHBYTES('SHA2_256',m.definition),2)
FROM sys.triggers t JOIN sys.sql_modules m ON m.object_id=t.object_id
WHERE t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS') AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS' AND t.is_disabled=0;
IF @HuellaActual<>@HuellaEsperada OR @HuellaActual IS NULL
    THROW 51000,'El trigger corregido no coincide o esta deshabilitado.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE parent_id IN(OBJECT_ID(N'dbo.RT_RUTAS'),OBJECT_ID(N'dbo.RT_RUTAS_DET'))
          AND is_disabled=0 AND name<>N'INSERT_MA_CUST_ORDER_LISTAS')
    THROW 51000,'Hay otros triggers de rutas habilitados. Revisar antes de probar.',1;

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
SELECT TOP (2) ID_RUTA INTO #RutasPrueba FROM RutasCandidatas ORDER BY FECHA_RUTA DESC,ID_RUTA DESC;
IF (SELECT COUNT(*) FROM #RutasPrueba)<>2
    THROW 51000,'Se necesitan dos rutas CAMBIO/ENVIO sin ambiguedades en la copia aislada.',1;

SELECT MIN(d.ROWID) AS DetalleRowId,d.ID_RUTA,
       f.ID_EMPRESA,f.TIPO_DOC,f.ID_DOCUMENTO,f.ID_FACTURA,
       f.STATUS AS EstadoOriginal,f.RUTA_ASIGNADA AS RutaOriginal
INTO #FuentesEsperadas
FROM dbo.RT_RUTAS_DET d JOIN dbo.RT_DOC_VARIOS_ENC f
  ON f.ID_EMPRESA=d.ID_EMPRESA AND f.TIPO_DOC=d.TIPO AND f.ID_DOCUMENTO=d.ID_DOCUMENTO
WHERE d.ID_RUTA IN(SELECT ID_RUTA FROM #RutasPrueba) AND d.TIPO IN(N'CAMBIO',N'ENVIO')
  AND d.F_DOCTO>=CONVERT(date,N'20250701',112) AND f.ID_FACTURA IS NOT NULL
GROUP BY d.ID_RUTA,f.ID_EMPRESA,f.TIPO_DOC,f.ID_DOCUMENTO,f.ID_FACTURA,f.STATUS,f.RUTA_ASIGNADA;
IF EXISTS(SELECT 1 FROM #FuentesEsperadas GROUP BY ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA HAVING COUNT(*)>1)
    THROW 51000,'Las dos rutas comparten una fuente. Preparar rutas independientes antes de probar.',1;

SELECT N'CANDIDATO' AS Seccion,ID_RUTA,COUNT(*) AS DocumentosFuente,@HuellaActual AS HuellaTrigger
FROM #FuentesEsperadas GROUP BY ID_RUTA;
SELECT ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA,STATUS,RUTA_ASIGNADA
INTO #FuentesAntes FROM dbo.RT_DOC_VARIOS_ENC;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    SELECT f.ID_EMPRESA FROM dbo.RT_DOC_VARIOS_ENC f WITH(UPDLOCK,HOLDLOCK)
    JOIN #FuentesEsperadas e ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
    -- Un estado distinto de A, una asignacion existente o ambos deben conservarse.
    DECLARE @Escenario tinyint=1;
    WHILE @Escenario<=3
    BEGIN
        UPDATE f SET f.STATUS=CASE WHEN @Escenario=2 THEN N'A' ELSE N'R' END,
                     f.RUTA_ASIGNADA=CASE WHEN @Escenario=1 THEN NULL ELSE
                         (SELECT MIN(p.ID_RUTA) FROM #RutasPrueba p WHERE p.ID_RUTA<>e.ID_RUTA) END
        FROM dbo.RT_DOC_VARIOS_ENC f JOIN #FuentesEsperadas e
          ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
         AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
        UPDATE r SET OBSR_GENERAL=OBSR_GENERAL FROM dbo.RT_RUTAS r
        JOIN #RutasPrueba p ON p.ID_RUTA=r.ID_RUTA;
        IF @@ROWCOUNT<>2 THROW 51000,'No se actualizaron exactamente dos rutas.',1;
        IF EXISTS(SELECT 1 FROM #FuentesEsperadas e LEFT JOIN dbo.RT_DOC_VARIOS_ENC f
          ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
         AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA
          WHERE f.ID_DOCUMENTO IS NULL
             OR ISNULL(f.STATUS,N'')<>CASE WHEN @Escenario=2 THEN N'A' ELSE N'R' END
             OR ISNULL(f.RUTA_ASIGNADA,N'')<>CASE WHEN @Escenario=1 THEN N'' ELSE
                 (SELECT MIN(p.ID_RUTA) FROM #RutasPrueba p WHERE p.ID_RUTA<>e.ID_RUTA) END)
            THROW 51000,'El trigger altero una fuente protegida.',1;
        SELECT N'FUENTES_PROTEGIDAS' AS Seccion,@Escenario AS Escenario,N'SIN CAMBIOS POR EL TRIGGER' AS Resultado;
        SET @Escenario+=1;
    END;
    UPDATE f SET f.STATUS=N'A',f.RUTA_ASIGNADA=NULL
    FROM dbo.RT_DOC_VARIOS_ENC f JOIN #FuentesEsperadas e
      ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
    IF @@ROWCOUNT<>(SELECT COUNT(*) FROM #FuentesEsperadas)
        THROW 51000,'No se preparo exactamente el conjunto esperado.',1;
    UPDATE r SET OBSR_GENERAL=OBSR_GENERAL FROM dbo.RT_RUTAS r
    JOIN #RutasPrueba p ON p.ID_RUTA=r.ID_RUTA;
    IF @@ROWCOUNT<>2 THROW 51000,'No se actualizaron exactamente dos rutas.',1;
    IF EXISTS
    (
        SELECT 1 FROM #FuentesEsperadas e LEFT JOIN dbo.RT_DOC_VARIOS_ENC f
          ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
         AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA
        WHERE f.ID_DOCUMENTO IS NULL OR ISNULL(f.STATUS,N'')<>N'R' OR ISNULL(f.RUTA_ASIGNADA,N'')<>e.ID_RUTA
    ) THROW 51000,'El trigger no asigno todas las fuentes esperadas.',1;
    SELECT N'DENTRO_TRANSACCION' AS Seccion,e.ID_RUTA,f.ID_EMPRESA,f.TIPO_DOC,
           f.ID_DOCUMENTO,f.ID_FACTURA,f.STATUS,f.RUTA_ASIGNADA,N'TRIGGER APLICO CORRECTAMENTE' AS Resultado
    FROM dbo.RT_DOC_VARIOS_ENC f JOIN #FuentesEsperadas e
      ON e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC
     AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA;
    IF EXISTS(
        SELECT ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA,STATUS,RUTA_ASIGNADA
        FROM dbo.RT_DOC_VARIOS_ENC f WHERE NOT EXISTS(SELECT 1 FROM #FuentesEsperadas e
          WHERE e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA)
        EXCEPT
        SELECT ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA,STATUS,RUTA_ASIGNADA
        FROM #FuentesAntes f WHERE NOT EXISTS(SELECT 1 FROM #FuentesEsperadas e
          WHERE e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA)
    ) OR EXISTS(
        SELECT ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA,STATUS,RUTA_ASIGNADA
        FROM #FuentesAntes f WHERE NOT EXISTS(SELECT 1 FROM #FuentesEsperadas e
          WHERE e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA)
        EXCEPT
        SELECT ID_EMPRESA,TIPO_DOC,ID_DOCUMENTO,ID_FACTURA,STATUS,RUTA_ASIGNADA
        FROM dbo.RT_DOC_VARIOS_ENC f WHERE NOT EXISTS(SELECT 1 FROM #FuentesEsperadas e
          WHERE e.ID_EMPRESA=f.ID_EMPRESA AND e.TIPO_DOC=f.TIPO_DOC AND e.ID_DOCUMENTO=f.ID_DOCUMENTO AND e.ID_FACTURA=f.ID_FACTURA)
    ) THROW 51000,'El trigger altero fuentes ajenas a las rutas probadas.',1;
    SELECT N'FUENTES_AJENAS' AS Seccion,N'SIN CAMBIOS' AS Resultado;
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
    SELECT N'DESPUES_ROLLBACK' AS Seccion,e.ID_RUTA,f.ID_EMPRESA,f.TIPO_DOC,
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
