/*
  Corrección puntual de seis borradores USD pendientes creados el 28/10/2026.
  La vista HANA ya fue corregida por la migración 10. Esta operación corrige
  únicamente la fotografía PAGADO guardada antes de esa migración.

  Evidencia del 04/11/2026: en los seis casos, BORR_NC_DET.PAGADO coincide
  exactamente con OINV.PaidToDate (moneda local), mientras TOTAL_FACT coincide
  con OINV.DocTotalFC. Los nuevos valores son OINV.PaidFC (USD). Antes de
  aplicar, volver a verificar en SAP que PaidToDate y PaidFC no cambiaron.

  BWF-00035 / ROWID 207 queda EXCLUIDO: su PAGADO guardado (119520.970)
  difiere del PaidToDate actual en SAP (132695.230). Requiere reconstruir el
  pagado en USD a la fecha de creación; no usar PaidFC actual como histórico.

  @Aplicar = 0 solo valida y muestra los seis cambios propuestos. Después de
  revisarlos, cambiar a 1 para aplicar. La aplicación crea un respaldo con
  todos los campos originales antes de actualizar. Reejecutar después de
  aplicar produce un error de seguridad; no repite los cambios.
*/
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;

DECLARE @Aplicar bit = 0; -- 0 = solo lectura; 1 = respaldo y corrección
DECLARE @Correcciones TABLE
(
    ROWID bigint NOT NULL PRIMARY KEY,
    ID_EMPRESA nvarchar(15) NOT NULL,
    ID_BORRADOR nvarchar(20) NOT NULL,
    REGISTRO datetime2(0) NOT NULL,
    ID_CLIENTE nvarchar(20) NOT NULL,
    DOCUMENTO nvarchar(50) NOT NULL,
    TOTAL_FACT decimal(20, 3) NOT NULL,
    PAGADO_ANTERIOR decimal(20, 3) NOT NULL,
    PAGADO_USD decimal(20, 3) NOT NULL
);

INSERT INTO @Correcciones
    (ROWID, ID_EMPRESA, ID_BORRADOR, REGISTRO, ID_CLIENTE, DOCUMENTO,
     TOTAL_FACT, PAGADO_ANTERIOR, PAGADO_USD)
VALUES
    (227, N'FAES',  N'BWF-00037', '2026-10-28T16:49:30', N'CE0004', N'2000607', 21690.000, 165844.780, 21629.520),
    (220, N'GRACO', N'BWG-00017', '2026-10-28T16:31:14', N'CE0016', N'2001463',  3084.000,  17148.780,  2250.960),
    (218, N'GRACO', N'BWG-00016', '2026-10-28T16:25:56', N'CE0016', N'2001424', 12524.000,  89971.230, 11803.530),
    (216, N'GRACO', N'BWG-00015', '2026-10-28T16:19:27', N'CE0016', N'2001384', 26240.250, 194340.200, 25489.880),
    (214, N'GRACO', N'BWG-00014', '2026-10-28T16:14:33', N'CE0016', N'2001368',  3275.000,  23202.970,  3040.540),
    (213, N'GRACO', N'BWG-00013', '2026-10-28T16:10:49', N'CE0016', N'2001324', 18236.000, 134237.060, 17571.170);

BEGIN TRY
    IF DB_NAME() <> N'POS-SmartK66'
        THROW 58100, 'Base incorrecta: se requiere POS-SmartK66.', 1;
    IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
       OR OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NULL
        THROW 58101, 'Faltan tablas de BorradoresNC.', 1;
    IF OBJECT_ID(N'dbo.BORR_NC_DET_RESPALDO_PAGADO_20261104') IS NOT NULL
        THROW 58102, 'Ya existe el respaldo: no volver a ejecutar la corrección.', 1;

    BEGIN TRANSACTION;
    DECLARE @Bloqueo int;
    EXEC @Bloqueo = sys.sp_getapplock
        @Resource = N'BorradorNc.CorregirPagadoUsd20261104',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction',
        @LockTimeout = 5000;
    IF @Bloqueo < 0 THROW 58103, 'No se obtuvo el bloqueo de corrección.', 1;

    -- Revalidar el estado y cada importe mientras las filas están bloqueadas.
    IF EXISTS
    (
        SELECT 1
        FROM @Correcciones C
        LEFT JOIN dbo.BORR_NC_DET D WITH (UPDLOCK, HOLDLOCK)
          ON D.ROWID = C.ROWID
        LEFT JOIN dbo.BORR_NC_ENC E WITH (UPDLOCK, HOLDLOCK)
          ON E.ID_EMPRESA = D.ID_EMPRESA AND E.ID_BORRADOR = D.ID_BORRADOR
        WHERE D.ROWID IS NULL OR E.ID_BORRADOR IS NULL
           OR D.ID_EMPRESA <> C.ID_EMPRESA
           OR D.ID_BORRADOR <> C.ID_BORRADOR
           OR D.DOCUMENTO <> C.DOCUMENTO
           OR D.MONEDA <> N'USD'
           OR D.TOTAL_FACT <> C.TOTAL_FACT
           OR D.PAGADO <> C.PAGADO_ANTERIOR
           OR E.REGISTRO <> C.REGISTRO
           OR E.ID_CLIENTE <> C.ID_CLIENTE
           OR E.ESTADO <> 'PENDIENTE'
    ) THROW 58104, 'Una fila cambió o dejó de estar pendiente; no se corrigió ninguna.', 1;

    SELECT CASE WHEN @Aplicar = 1 THEN N'APLICAR' ELSE N'SIMULACION' END AS MODO,
           C.ID_EMPRESA, C.ID_BORRADOR, C.DOCUMENTO,
           D.PAGADO AS PAGADO_ANTERIOR, C.PAGADO_USD AS PAGADO_PROPUESTO,
           C.TOTAL_FACT, C.TOTAL_FACT - C.PAGADO_USD AS SALDO_USD
    FROM @Correcciones C
    JOIN dbo.BORR_NC_DET D ON D.ROWID = C.ROWID
    ORDER BY C.ID_EMPRESA, C.ID_BORRADOR;

    IF @Aplicar = 1
    BEGIN
        -- El respaldo permanente conserva el registro completo y el valor destino.
        SELECT D.*, E.REGISTRO AS REGISTRO_BORRADOR,
               E.ESTADO AS ESTADO_BORRADOR, E.ID_CLIENTE,
               C.PAGADO_USD AS PAGADO_DESTINO,
               SYSDATETIME() AS FECHA_RESPALDO,
               SUSER_SNAME() AS USUARIO_EJECUCION
        INTO dbo.BORR_NC_DET_RESPALDO_PAGADO_20261104
        FROM dbo.BORR_NC_DET D
        JOIN @Correcciones C ON C.ROWID = D.ROWID
        JOIN dbo.BORR_NC_ENC E
          ON E.ID_EMPRESA = D.ID_EMPRESA AND E.ID_BORRADOR = D.ID_BORRADOR;
        IF @@ROWCOUNT <> 6 THROW 58105, 'El respaldo no contiene seis filas.', 1;

        UPDATE D
           SET PAGADO = C.PAGADO_USD
        FROM dbo.BORR_NC_DET D
        JOIN @Correcciones C ON C.ROWID = D.ROWID
        WHERE D.PAGADO = C.PAGADO_ANTERIOR;
        IF @@ROWCOUNT <> 6 THROW 58106, 'No se actualizaron exactamente seis filas.', 1;

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
