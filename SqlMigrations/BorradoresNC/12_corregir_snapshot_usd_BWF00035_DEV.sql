/*
  Corrección puntual de BWF-00035 en POS-SmartK66_DEV, base utilizada por
  BorradoresNC en la aplicación publicada. Ejecutar el archivo completo.
  ESTADO: APLICADO el 04/11/2026; fila y respaldo verificados. No volver a
  ejecutar. El control del respaldo también impide una repetición.

  La fotografía del 28/10/2026 a las 12:08:31 guardó PAGADO = 119520.970
  en moneda local contra TOTAL_FACT = 20249.250 USD. SAP OITR/ITR1 para la
  factura FAES 2000777 (DocEntry 22031) muestra:
    ReconNum 31209, creado 30/09: 119520.97 local / 15678.13 USD.
    ReconNum 33665, creado 03/11:  13174.26 local /  1728.13 USD.
  Ambos estaban vigentes al consultar (Canceled = N). La segunda aplicación
  ocurrió después de crear el borrador. La primera iguala exactamente el
  PAGADO guardado y permite recuperar el valor histórico: 15678.130 USD.

  @Aplicar = 0 solo valida y muestra la propuesta. Cambiar a 1 únicamente
  después de revisar la simulación. La aplicación respalda la fila original
  y actualiza PAGADO en una transacción. El respaldo impide repetirla.
*/
USE [POS-SmartK66_DEV];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;

DECLARE @Aplicar bit = 0; -- 0 = solo lectura; 1 = respaldo y corrección

BEGIN TRY
    IF DB_NAME() <> N'POS-SmartK66_DEV'
        THROW 58200, 'Base incorrecta: se requiere POS-SmartK66_DEV.', 1;
    IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
       OR OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NULL
        THROW 58201, 'Faltan tablas de BorradoresNC.', 1;
    IF OBJECT_ID(N'dbo.BORR_NC_DET_RESPALDO_PAGADO_BWF00035_20261104') IS NOT NULL
        THROW 58202, 'Ya existe el respaldo: no volver a ejecutar la corrección.', 1;

    BEGIN TRANSACTION;
    DECLARE @Bloqueo int;
    EXEC @Bloqueo = sys.sp_getapplock
        @Resource = N'BorradorNc.CorregirPagadoBWF00035',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction',
        @LockTimeout = 5000;
    IF @Bloqueo < 0 THROW 58203, 'No se obtuvo el bloqueo de corrección.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.BORR_NC_DET D WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.BORR_NC_ENC E WITH (UPDLOCK, HOLDLOCK)
          ON E.ID_EMPRESA = D.ID_EMPRESA AND E.ID_BORRADOR = D.ID_BORRADOR
        WHERE D.ROWID = 207
          AND D.ID_EMPRESA = N'FAES' AND D.ID_BORRADOR = N'BWF-00035'
          AND D.DOCUMENTO = N'2000777' AND D.MONEDA = N'USD'
          AND D.TOTAL_FACT = 20249.250 AND D.PAGADO = 119520.970
          AND E.REGISTRO = '2026-10-28T12:08:31'
          AND E.ID_CLIENTE = N'CE0011' AND E.ESTADO = 'PENDIENTE'
    ) THROW 58204, 'La fila cambió o dejó de estar pendiente; no se corrigió.', 1;

    SELECT CASE WHEN @Aplicar = 1 THEN N'APLICAR' ELSE N'SIMULACION' END AS MODO,
           D.ROWID, D.ID_EMPRESA, D.ID_BORRADOR, D.DOCUMENTO,
           D.PAGADO AS PAGADO_ANTERIOR,
           CAST(15678.130 AS decimal(20, 3)) AS PAGADO_PROPUESTO,
           D.TOTAL_FACT,
           D.TOTAL_FACT - CAST(15678.130 AS decimal(20, 3)) AS SALDO_USD
    FROM dbo.BORR_NC_DET D
    WHERE D.ROWID = 207;

    IF @Aplicar = 1
    BEGIN
        SELECT D.*, E.REGISTRO AS REGISTRO_BORRADOR,
               E.ESTADO AS ESTADO_BORRADOR, E.ID_CLIENTE,
               CAST(15678.130 AS decimal(20, 3)) AS PAGADO_DESTINO,
               SYSDATETIME() AS FECHA_RESPALDO,
               SUSER_SNAME() AS USUARIO_EJECUCION
        INTO dbo.BORR_NC_DET_RESPALDO_PAGADO_BWF00035_20261104
        FROM dbo.BORR_NC_DET D
        JOIN dbo.BORR_NC_ENC E
          ON E.ID_EMPRESA = D.ID_EMPRESA AND E.ID_BORRADOR = D.ID_BORRADOR
        WHERE D.ROWID = 207;
        IF @@ROWCOUNT <> 1 THROW 58205, 'El respaldo no contiene una fila.', 1;

        UPDATE dbo.BORR_NC_DET
           SET PAGADO = 15678.130
         WHERE ROWID = 207 AND ID_EMPRESA = N'FAES'
           AND ID_BORRADOR = N'BWF-00035' AND PAGADO = 119520.970;
        IF @@ROWCOUNT <> 1 THROW 58206, 'No se actualizó exactamente una fila.', 1;

        COMMIT TRANSACTION;
    END
    ELSE
        ROLLBACK TRANSACTION;

    SELECT CASE WHEN @Aplicar = 1 THEN N'APLICADO' ELSE N'SIMULADO_SIN_CAMBIOS' END
           AS RESULTADO;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
