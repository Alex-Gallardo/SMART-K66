/* Corrección puntual de BOLIK / COT-BO-00000001.
   1. Confirmar que @BaseEsperada es la base de la aplicación en este entorno.
   2. Ejecutar con @Aplicar = 0 y revisar que el nuevo total es 1889.28.
   3. Para guardar: @Aplicar = 1 y escribir la frase de @Confirmacion.
   No modifica otras cotizaciones ni consulta SAP HANA. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @BaseEsperada sysname = N'POS-SmartK66_DEV';
DECLARE @Aplicar bit = 0;
DECLARE @Confirmacion nvarchar(50) = N'';
DECLARE @NuevoTotal decimal(20,2);

IF DB_NAME() <> @BaseEsperada
    THROW 55010, 'Base no esperada. Revise el nombre de la base antes de ejecutar.', 1;
IF OBJECT_ID(N'dbo.COT_ENC', N'U') IS NULL OR OBJECT_ID(N'dbo.COT_DET', N'U') IS NULL
    THROW 55011, 'Faltan tablas de cotizaciones.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    SELECT e.ID_EMPRESA, e.ID_COTIZACION, e.SUBTOTAL, e.IMPUESTO_TOTAL, e.TOTAL
    FROM dbo.COT_ENC AS e WITH (UPDLOCK, HOLDLOCK)
    WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001';

    IF (SELECT COUNT(*) FROM dbo.COT_ENC
        WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001') <> 1
        THROW 55012, 'No existe exactamente una cabecera objetivo.', 1;
    IF (SELECT COUNT(*) FROM dbo.COT_DET
        WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001') <> 1
        THROW 55013, 'El detalle no coincide con el PDF de una línea; no aplicar.', 1;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.COT_DET WITH (UPDLOCK, HOLDLOCK)
        WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001'
          AND ITEM_CODE = N'PT00362' AND CANTIDAD = 144
          AND ROUND(PRECIO_UNITARIO, 2) = 13.12
          AND DESCUENTO_PORCENTAJE = 0)
        THROW 55014, 'Cantidad, precio visible o descuento difieren del PDF; no aplicar.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.COT_ENC AS e JOIN dbo.COT_DET AS d
          ON d.ID_EMPRESA = e.ID_EMPRESA AND d.ID_COTIZACION = e.ID_COTIZACION
        WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001'
          AND e.IMPORTE_BRUTO = 1889.28 AND e.DESCUENTO_TOTAL = 0
          AND e.SUBTOTAL = 1889.28 AND e.IMPUESTO_TOTAL = 0 AND e.TOTAL = 1889.28
          AND d.PRECIO_UNITARIO = 13.12 AND d.IMPORTE_BRUTO = 1889.28
          AND d.DESCUENTO_MONTO = 0 AND d.SUBTOTAL = 1889.28
          AND d.IMPUESTO_MONTO = 0 AND d.TOTAL = 1889.28)
    BEGIN
        SELECT DB_NAME() AS BASE_ACTUAL, N'YA_CORREGIDO' AS ESTADO,
               e.ID_EMPRESA, e.ID_COTIZACION, e.TOTAL, d.PRECIO_UNITARIO,
               d.CANTIDAD, d.TOTAL AS TOTAL_LINEA
        FROM dbo.COT_ENC AS e JOIN dbo.COT_DET AS d
          ON d.ID_EMPRESA = e.ID_EMPRESA AND d.ID_COTIZACION = e.ID_COTIZACION
        WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001';
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.COT_ENC AS e JOIN dbo.COT_DET AS d
          ON d.ID_EMPRESA = e.ID_EMPRESA AND d.ID_COTIZACION = e.ID_COTIZACION
        WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001'
          AND e.IMPORTE_BRUTO = 1888.71 AND e.DESCUENTO_TOTAL = 0
          AND e.SUBTOTAL = 1888.71 AND e.IMPUESTO_TOTAL = 226.65 AND e.TOTAL = 2115.36
          AND d.IMPORTE_BRUTO = 1888.71 AND d.DESCUENTO_MONTO = 0
          AND d.SUBTOTAL = 1888.71 AND d.IMPUESTO_MONTO = 226.65 AND d.TOTAL = 2115.36)
        THROW 55015, 'Los importes de cabecera difieren del PDF o del resultado esperado.', 1;

    SELECT @NuevoTotal = ROUND(CANTIDAD * ROUND(PRECIO_UNITARIO, 2), 2)
    FROM dbo.COT_DET
    WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001';
    IF @NuevoTotal <> 1889.28
        THROW 55016, 'El cálculo no coincide con 144 x 13.12 = 1889.28.', 1;

    IF @Aplicar = 1
    BEGIN
        IF @Confirmacion <> N'ACEPTO_TOTAL_1889_28'
            THROW 55017, 'Falta la confirmacion para reemplazar el total historico.', 1;
        IF OBJECT_ID(N'dbo.COT_RESPALDO_BO_00000001_ENC', N'U') IS NOT NULL
           OR OBJECT_ID(N'dbo.COT_RESPALDO_BO_00000001_DET', N'U') IS NOT NULL
            THROW 55018, 'Ya existe un respaldo de esta cotizacion. No se sobrescribe.', 1;

        SELECT e.* INTO dbo.COT_RESPALDO_BO_00000001_ENC
        FROM dbo.COT_ENC AS e
        WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001';
        IF @@ROWCOUNT <> 1 THROW 55019, 'No se respaldo la cabecera.', 1;

        SELECT d.* INTO dbo.COT_RESPALDO_BO_00000001_DET
        FROM dbo.COT_DET AS d
        WHERE d.ID_EMPRESA = N'BOLIK' AND d.ID_COTIZACION = N'COT-BO-00000001';
        IF @@ROWCOUNT <> 1 THROW 55020, 'No se respaldo el detalle.', 1;
    END;

    UPDATE dbo.COT_DET
       SET PRECIO_UNITARIO = ROUND(PRECIO_UNITARIO, 2),
           IMPORTE_BRUTO = @NuevoTotal,
           DESCUENTO_MONTO = 0,
           SUBTOTAL = @NuevoTotal,
           IMPUESTO_MONTO = 0,
           TOTAL = @NuevoTotal
     WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001';
    IF @@ROWCOUNT <> 1 THROW 55021, 'No se actualizo exactamente una linea.', 1;
    UPDATE dbo.COT_ENC
       SET IMPORTE_BRUTO = @NuevoTotal,
           DESCUENTO_TOTAL = 0,
           SUBTOTAL = @NuevoTotal,
           IMPUESTO_TOTAL = 0,
           TOTAL = @NuevoTotal
     WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001';
    IF @@ROWCOUNT <> 1 THROW 55022, 'No se actualizo exactamente una cabecera.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.COT_ENC AS e JOIN dbo.COT_DET AS d
          ON d.ID_EMPRESA = e.ID_EMPRESA AND d.ID_COTIZACION = e.ID_COTIZACION
        WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001'
          AND e.IMPORTE_BRUTO = 1889.28 AND e.DESCUENTO_TOTAL = 0
          AND e.SUBTOTAL = 1889.28 AND e.IMPUESTO_TOTAL = 0 AND e.TOTAL = 1889.28
          AND d.PRECIO_UNITARIO = 13.12 AND d.IMPORTE_BRUTO = 1889.28
          AND d.DESCUENTO_MONTO = 0 AND d.SUBTOTAL = 1889.28
          AND d.IMPUESTO_MONTO = 0 AND d.TOTAL = 1889.28)
        THROW 55023, 'La verificacion posterior no coincide; se revertira todo.', 1;

    SELECT DB_NAME() AS BASE_ACTUAL, N'PROPUESTA_1889_28' AS ESTADO,
           e.ID_EMPRESA, e.ID_COTIZACION, e.IMPORTE_BRUTO, e.DESCUENTO_TOTAL,
           e.SUBTOTAL, e.IMPUESTO_TOTAL, e.TOTAL,
           d.CANTIDAD, d.PRECIO_UNITARIO, d.IMPUESTO_MONTO, d.TOTAL AS TOTAL_LINEA
    FROM dbo.COT_ENC AS e
    JOIN dbo.COT_DET AS d
      ON d.ID_EMPRESA = e.ID_EMPRESA AND d.ID_COTIZACION = e.ID_COTIZACION
    WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001';

    IF @Aplicar = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'APLICADO: total 1889.28 confirmado. Respaldo creado.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'SIMULACION: cambios revertidos. Revise los valores antes de poner @Aplicar = 1.';
    END
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
