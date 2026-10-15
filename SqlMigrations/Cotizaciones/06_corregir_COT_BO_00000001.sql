/* Corrección puntual del documento del PDF. Primero ejecutar con @Aplicar = 0.
   Requiere validar la salida; después cambiar a 1 y ejecutar de nuevo.
   No modifica otras cotizaciones ni recalcula IVA. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Aplicar bit = 0;
DECLARE @NuevoTotal decimal(20,2);

IF DB_NAME() <> N'POS-SmartK66_DEV'
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
    IF NOT EXISTS (
        SELECT 1 FROM dbo.COT_ENC
        WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001'
          AND ((SUBTOTAL = 1888.71 AND IMPUESTO_TOTAL = 226.65 AND TOTAL = 2115.36)
            OR (SUBTOTAL = 1889.28 AND IMPUESTO_TOTAL = 0 AND TOTAL = 1889.28)))
        THROW 55015, 'Los importes de cabecera difieren del PDF o del resultado esperado.', 1;

    SELECT @NuevoTotal = ROUND(CANTIDAD * ROUND(PRECIO_UNITARIO, 2), 2)
    FROM dbo.COT_DET
    WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001';
    IF @NuevoTotal <> 1889.28
        THROW 55016, 'El cálculo no coincide con 144 x 13.12 = 1889.28.', 1;

    UPDATE dbo.COT_DET
       SET PRECIO_UNITARIO = ROUND(PRECIO_UNITARIO, 2),
           IMPORTE_BRUTO = @NuevoTotal,
           DESCUENTO_MONTO = 0,
           SUBTOTAL = @NuevoTotal,
           IMPUESTO_MONTO = 0,
           TOTAL = @NuevoTotal
     WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001';
    UPDATE dbo.COT_ENC
       SET IMPORTE_BRUTO = @NuevoTotal,
           DESCUENTO_TOTAL = 0,
           SUBTOTAL = @NuevoTotal,
           IMPUESTO_TOTAL = 0,
           TOTAL = @NuevoTotal
     WHERE ID_EMPRESA = N'BOLIK' AND ID_COTIZACION = N'COT-BO-00000001';

    SELECT e.ID_EMPRESA, e.ID_COTIZACION, e.IMPORTE_BRUTO, e.DESCUENTO_TOTAL,
           e.SUBTOTAL, e.IMPUESTO_TOTAL, e.TOTAL,
           d.CANTIDAD, d.PRECIO_UNITARIO, d.IMPUESTO_MONTO, d.TOTAL AS TOTAL_LINEA
    FROM dbo.COT_ENC AS e
    JOIN dbo.COT_DET AS d
      ON d.ID_EMPRESA = e.ID_EMPRESA AND d.ID_COTIZACION = e.ID_COTIZACION
    WHERE e.ID_EMPRESA = N'BOLIK' AND e.ID_COTIZACION = N'COT-BO-00000001';

    IF @Aplicar = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'APLICADO: corrección confirmada.';
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
