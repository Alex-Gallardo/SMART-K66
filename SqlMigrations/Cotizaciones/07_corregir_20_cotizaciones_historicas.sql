/*
  Reinterpreta como precio FINAL el precio unitario que se imprimia a dos
  decimales en 20 cotizaciones historicas. NO consulta ni modifica SAP HANA.

  ATENCION: el codigo anterior habia dividido el precio bruto SAP entre
  (1 + IVA/100) antes de guardar estas cotizaciones. Por tanto, esta operacion
  REDUCE los importes de documentos existentes; no es solo un cambio visual.
  Se requiere aprobacion comercial/creditos para reemplazar esos importes.

  1. Ejecutar con @Aplicar = 0. Revisar TODOS los resultados y compartirlos.
  2. Solo tras aprobar cada nuevo total, cambiar @Aplicar a 1 y escribir
     @Confirmacion = N'ACEPTO_REEMPLAZAR_20_TOTALES'.
  3. Volver a ejecutar. La transaccion respalda cabeceras y detalles completos
     en dbo.COT_RESPALDO_20261015_ENC / dbo.COT_RESPALDO_20261015_DET.

  El documento BOLIK / COT-BO-00000001 NO esta en este lote (script 06).
  Cualquier diferencia en los calculos anteriores cancela el lote completo.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Aplicar bit = 0;
DECLARE @Confirmacion nvarchar(50) = N'';

IF DB_NAME() <> N'POS-SmartK66_DEV'
    THROW 55020, 'Base no esperada. Verifique la conexion antes de continuar.', 1;
IF OBJECT_ID(N'dbo.COT_ENC', N'U') IS NULL OR OBJECT_ID(N'dbo.COT_DET', N'U') IS NULL
    THROW 55021, 'Faltan las tablas de cotizaciones.', 1;

DECLARE @Objetivos table
(
    ID_EMPRESA nvarchar(15) NOT NULL,
    ID_COTIZACION nvarchar(30) NOT NULL,
    PRIMARY KEY (ID_EMPRESA, ID_COTIZACION)
);
INSERT INTO @Objetivos (ID_EMPRESA, ID_COTIZACION) VALUES
    (N'GRACO', N'COT-GR-00000022'), (N'GRACO', N'COT-GR-00000023'),
    (N'GRACO', N'COT-GR-00000021'), (N'GRACO', N'COT-GR-00000020'),
    (N'GRACO', N'COT-GR-00000016'), (N'GRACO', N'COT-GR-00000017'),
    (N'GRACO', N'COT-GR-00000011'), (N'GRACO', N'COT-GR-00000012'),
    (N'GRACO', N'COT-GR-00000013'), (N'GRACO', N'COT-GR-00000014'),
    (N'GRACO', N'COT-GR-00000009'), (N'GRACO', N'COT-GR-00000010'),
    (N'GRACO', N'COT-GR-00000006'), (N'GRACO', N'COT-GR-00000007'),
    (N'GRACO', N'COT-GR-00000008'), (N'GRACO', N'COT-GR-00000005'),
    (N'GRACO', N'COT-GR-00000002'), (N'GRACO', N'COT-GR-00000003'),
    (N'FAES',  N'COT-FA-00000002'), (N'FAES',  N'COT-FA-00000001');

IF (SELECT COUNT(*) FROM @Objetivos) <> 20
    THROW 55022, 'La lista objetivo no contiene exactamente 20 documentos.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    -- El bloqueo evita que cambien los documentos entre la vista previa y
    -- la eventual escritura de esta misma ejecucion.
    SELECT e.ID_EMPRESA, e.ID_COTIZACION
    FROM dbo.COT_ENC AS e WITH (UPDLOCK, HOLDLOCK)
    JOIN @Objetivos AS o
      ON o.ID_EMPRESA = e.ID_EMPRESA AND o.ID_COTIZACION = e.ID_COTIZACION;

    SELECT d.ROWID, d.ID_EMPRESA, d.ID_COTIZACION, d.LINEA, d.ITEM_CODE,
           d.CANTIDAD, d.PRECIO_UNITARIO AS PRECIO_ANTERIOR,
           p.PRECIO_VISIBLE,
           CAST(ROUND(d.PRECIO_UNITARIO *
                (1 + d.IMPUESTO_PORCENTAJE / 100), 2) AS decimal(20,2))
                AS PRECIO_BRUTO_SAP_ESTIMADO,
           d.DESCUENTO_PORCENTAJE, d.IMPUESTO_PORCENTAJE,
           d.IMPORTE_BRUTO AS BRUTO_ANTERIOR,
           d.DESCUENTO_MONTO AS DESCUENTO_ANTERIOR,
           d.SUBTOTAL AS SUBTOTAL_ANTERIOR,
           d.IMPUESTO_MONTO AS IMPUESTO_ANTERIOR,
           d.TOTAL AS TOTAL_ANTERIOR,
           b.NUEVO_BRUTO, x.NUEVO_DESCUENTO,
           CAST(b.NUEVO_BRUTO - x.NUEVO_DESCUENTO AS decimal(20,2))
                AS NUEVO_TOTAL,
           CASE WHEN d.IMPORTE_BRUTO = ROUND(d.CANTIDAD * d.PRECIO_UNITARIO, 2)
                 AND d.DESCUENTO_MONTO = ROUND(d.IMPORTE_BRUTO * d.DESCUENTO_PORCENTAJE / 100, 2)
                 AND d.SUBTOTAL = d.IMPORTE_BRUTO - d.DESCUENTO_MONTO
                 AND d.IMPUESTO_MONTO = ROUND(d.SUBTOTAL * d.IMPUESTO_PORCENTAJE / 100, 2)
                 AND d.TOTAL = d.SUBTOTAL + d.IMPUESTO_MONTO
                THEN 0 ELSE 1 END AS ERROR_CALCULO_ANTERIOR
    INTO #Propuesta
    FROM dbo.COT_DET AS d WITH (UPDLOCK, HOLDLOCK)
    JOIN @Objetivos AS o
      ON o.ID_EMPRESA = d.ID_EMPRESA AND o.ID_COTIZACION = d.ID_COTIZACION
    CROSS APPLY
        (SELECT CAST(ROUND(d.PRECIO_UNITARIO, 2) AS decimal(20,2)) AS PRECIO_VISIBLE) AS p
    CROSS APPLY
        (SELECT CAST(ROUND(d.CANTIDAD * p.PRECIO_VISIBLE, 2) AS decimal(20,2)) AS NUEVO_BRUTO) AS b
    CROSS APPLY
        (SELECT CAST(ROUND(b.NUEVO_BRUTO * d.DESCUENTO_PORCENTAJE / 100, 2)
                     AS decimal(20,2)) AS NUEVO_DESCUENTO) AS x;

    SELECT o.ID_EMPRESA, o.ID_COTIZACION, e.FECHA, e.ESTADO AS ESTADO_DOCUMENTO,
           e.IMPORTE_BRUTO AS BRUTO_ANTERIOR,
           e.DESCUENTO_TOTAL AS DESCUENTO_ANTERIOR,
           e.SUBTOTAL AS SUBTOTAL_ANTERIOR,
           e.IMPUESTO_TOTAL AS IMPUESTO_ANTERIOR,
           e.TOTAL AS TOTAL_ANTERIOR,
           l.LINEAS, l.BRUTO_LINEAS, l.DESCUENTO_LINEAS,
           l.SUBTOTAL_LINEAS, l.IMPUESTO_LINEAS, l.TOTAL_LINEAS,
           l.NUEVO_BRUTO, l.NUEVO_DESCUENTO, l.NUEVO_TOTAL,
           l.ERRORES_LINEA, l.PRECIOS_CERO,
           CASE WHEN e.ID_COTIZACION IS NULL THEN N'FALTA_DOCUMENTO'
                WHEN e.ESTADO <> 'VIGENTE' THEN N'REVISAR_ESTADO'
                WHEN e.IMPUESTO_TOTAL <= 0 THEN N'NO_ES_LEGADO_CON_IVA'
                WHEN l.LINEAS IS NULL OR l.LINEAS = 0 THEN N'SIN_DETALLE'
                WHEN l.PRECIOS_CERO <> 0 THEN N'REVISAR_PRECIO_CERO'
                WHEN l.ERRORES_LINEA <> 0 THEN N'REVISAR_CALCULO_LINEA'
                WHEN e.IMPORTE_BRUTO <> l.BRUTO_LINEAS
                  OR e.DESCUENTO_TOTAL <> l.DESCUENTO_LINEAS
                  OR e.SUBTOTAL <> l.SUBTOTAL_LINEAS
                  OR e.IMPUESTO_TOTAL <> l.IMPUESTO_LINEAS
                  OR e.TOTAL <> l.TOTAL_LINEAS THEN N'REVISAR_CABECERA'
                ELSE N'LISTO' END AS VALIDACION
    INTO #Resumen
    FROM @Objetivos AS o
    LEFT JOIN dbo.COT_ENC AS e
      ON e.ID_EMPRESA = o.ID_EMPRESA AND e.ID_COTIZACION = o.ID_COTIZACION
    LEFT JOIN
    (
        SELECT ID_EMPRESA, ID_COTIZACION, COUNT(*) AS LINEAS,
               SUM(BRUTO_ANTERIOR) AS BRUTO_LINEAS,
               SUM(DESCUENTO_ANTERIOR) AS DESCUENTO_LINEAS,
               SUM(SUBTOTAL_ANTERIOR) AS SUBTOTAL_LINEAS,
               SUM(IMPUESTO_ANTERIOR) AS IMPUESTO_LINEAS,
               SUM(TOTAL_ANTERIOR) AS TOTAL_LINEAS,
               SUM(NUEVO_BRUTO) AS NUEVO_BRUTO,
               SUM(NUEVO_DESCUENTO) AS NUEVO_DESCUENTO,
               SUM(NUEVO_TOTAL) AS NUEVO_TOTAL,
               SUM(ERROR_CALCULO_ANTERIOR) AS ERRORES_LINEA,
               SUM(CASE WHEN PRECIO_VISIBLE <= 0 THEN 1 ELSE 0 END) AS PRECIOS_CERO
        FROM #Propuesta
        GROUP BY ID_EMPRESA, ID_COTIZACION
    ) AS l ON l.ID_EMPRESA = o.ID_EMPRESA AND l.ID_COTIZACION = o.ID_COTIZACION;

    SELECT DB_NAME() AS BASE_ACTUAL, ID_EMPRESA, ID_COTIZACION, FECHA,
           ESTADO_DOCUMENTO, LINEAS, TOTAL_ANTERIOR, NUEVO_TOTAL,
           NUEVO_TOTAL - TOTAL_ANTERIOR AS DIFERENCIA,
           IMPUESTO_ANTERIOR, NUEVO_DESCUENTO, VALIDACION
    FROM #Resumen
    ORDER BY ID_EMPRESA, ID_COTIZACION;

    SELECT ID_EMPRESA, ID_COTIZACION, LINEA, ITEM_CODE, CANTIDAD,
           PRECIO_ANTERIOR, PRECIO_VISIBLE, PRECIO_BRUTO_SAP_ESTIMADO,
           DESCUENTO_PORCENTAJE, IMPUESTO_PORCENTAJE,
           TOTAL_ANTERIOR, NUEVO_TOTAL,
           NUEVO_TOTAL - TOTAL_ANTERIOR AS DIFERENCIA,
           ERROR_CALCULO_ANTERIOR
    FROM #Propuesta
    ORDER BY ID_EMPRESA, ID_COTIZACION, LINEA;

    SELECT VALIDACION, COUNT(*) AS DOCUMENTOS
    FROM #Resumen GROUP BY VALIDACION ORDER BY VALIDACION;

    IF EXISTS (SELECT 1 FROM #Resumen WHERE VALIDACION <> N'LISTO')
        THROW 55023, 'Hay documentos no elegibles. No se modifico ninguno.', 1;
    IF (SELECT COUNT(*) FROM #Resumen) <> 20
        THROW 55024, 'El resumen no contiene exactamente 20 documentos.', 1;
    IF @Aplicar = 1 AND @Confirmacion <> N'ACEPTO_REEMPLAZAR_20_TOTALES'
        THROW 55025, 'Falta confirmacion expresa del cambio de importes.', 1;

    IF @Aplicar = 1
    BEGIN
        IF OBJECT_ID(N'dbo.COT_RESPALDO_20261015_ENC', N'U') IS NOT NULL
           OR OBJECT_ID(N'dbo.COT_RESPALDO_20261015_DET', N'U') IS NOT NULL
            THROW 55026, 'Ya existe un respaldo con este nombre. No se sobrescribe.', 1;

        SELECT e.* INTO dbo.COT_RESPALDO_20261015_ENC
        FROM dbo.COT_ENC AS e JOIN @Objetivos AS o
          ON o.ID_EMPRESA = e.ID_EMPRESA AND o.ID_COTIZACION = e.ID_COTIZACION;
        IF @@ROWCOUNT <> 20
            THROW 55027, 'El respaldo de cabeceras no contiene 20 filas.', 1;

        SELECT d.* INTO dbo.COT_RESPALDO_20261015_DET
        FROM dbo.COT_DET AS d JOIN @Objetivos AS o
          ON o.ID_EMPRESA = d.ID_EMPRESA AND o.ID_COTIZACION = d.ID_COTIZACION;
        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM #Propuesta)
            THROW 55028, 'El respaldo de detalles esta incompleto.', 1;
    END;

    UPDATE d SET d.PRECIO_UNITARIO = p.PRECIO_VISIBLE,
                 d.IMPORTE_BRUTO = p.NUEVO_BRUTO,
                 d.DESCUENTO_MONTO = p.NUEVO_DESCUENTO,
                 d.SUBTOTAL = p.NUEVO_TOTAL,
                 d.IMPUESTO_MONTO = 0,
                 d.TOTAL = p.NUEVO_TOTAL
    FROM dbo.COT_DET AS d JOIN #Propuesta AS p ON p.ROWID = d.ROWID;
    IF @@ROWCOUNT <> (SELECT COUNT(*) FROM #Propuesta)
        THROW 55029, 'No se actualizaron todas las lineas previstas.', 1;

    UPDATE e SET e.IMPORTE_BRUTO = r.NUEVO_BRUTO,
                 e.DESCUENTO_TOTAL = r.NUEVO_DESCUENTO,
                 e.SUBTOTAL = r.NUEVO_TOTAL,
                 e.IMPUESTO_TOTAL = 0,
                 e.TOTAL = r.NUEVO_TOTAL
    FROM dbo.COT_ENC AS e JOIN #Resumen AS r
      ON r.ID_EMPRESA = e.ID_EMPRESA AND r.ID_COTIZACION = e.ID_COTIZACION;
    IF @@ROWCOUNT <> 20
        THROW 55030, 'No se actualizaron las 20 cabeceras previstas.', 1;

    IF EXISTS
    (
        SELECT 1 FROM #Resumen AS r
        JOIN dbo.COT_ENC AS e
          ON e.ID_EMPRESA = r.ID_EMPRESA AND e.ID_COTIZACION = r.ID_COTIZACION
        WHERE e.TOTAL <> r.NUEVO_TOTAL OR e.IMPUESTO_TOTAL <> 0
    ) OR EXISTS
    (
        SELECT 1 FROM #Propuesta AS p
        JOIN dbo.COT_DET AS d ON d.ROWID = p.ROWID
        WHERE d.TOTAL <> p.NUEVO_TOTAL OR d.IMPUESTO_MONTO <> 0
           OR d.PRECIO_UNITARIO <> p.PRECIO_VISIBLE
    )
        THROW 55031, 'La verificacion posterior fallo; se revertira todo.', 1;

    IF @Aplicar = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'APLICADO: 20 documentos corregidos y respaldados.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'SIMULACION: no se guardaron cambios ni respaldos.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
