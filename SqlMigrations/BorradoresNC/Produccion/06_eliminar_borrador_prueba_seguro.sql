/* =============================================================================
   BORRADORES NC — ELIMINAR UN BORRADOR DE PRUEBA DE FORMA CONTROLADA
   Destino exclusivo: POS-SmartK66_DEV (PRODUCCION)

   Elimina exactamente un borrador identificado por EMPRESA + ID_BORRADOR.
   BORR_NC_DET y BORR_NC_ADJUNTO se eliminan mediante sus FK ON DELETE CASCADE.
   El correlativo de BORR_NC_SERIES NO se modifica ni se reutiliza.

   Uso
   ---
   1. Complete @Empresa y @IdBorrador.
   2. Ejecute primero con @ConfirmarEliminacion = 0 para revisar el objetivo.
   3. Si la salida es correcta, cambie el valor a 1 y ejecute TODO el archivo.
   ============================================================================= */

USE [POS-SmartK66_DEV];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

DECLARE @Empresa nvarchar(15) = N'GRACO';       -- BOLIK, FAES o GRACO
DECLARE @IdBorrador nvarchar(20) = N'BWG-00000'; -- Reemplazar por el ID exacto
DECLARE @EstadoEsperado varchar(20) = 'PENDIENTE'; -- Ajustar si la prueba fue resuelta
DECLARE @ConfirmarEliminacion bit = 0;            -- Cambiar a 1 para eliminar

IF DB_NAME() <> N'POS-SmartK66_DEV'
BEGIN
    THROW 51600,
          'SEGURIDAD: este script solo esta autorizado para POS-SmartK66_DEV.',
          1;
END;

SET @Empresa = UPPER(LTRIM(RTRIM(@Empresa)));
SET @IdBorrador = UPPER(LTRIM(RTRIM(@IdBorrador)));

IF NULLIF(@Empresa, N'') IS NULL OR NULLIF(@IdBorrador, N'') IS NULL
BEGIN
    THROW 51601, 'Debe indicar empresa e ID de borrador.', 1;
END;

IF @Empresa NOT IN (N'BOLIK', N'FAES', N'GRACO')
BEGIN
    THROW 51602, 'La empresa indicada no es valida.', 1;
END;

IF @EstadoEsperado NOT IN ('PENDIENTE', 'AUTORIZADO', 'RECHAZADO', 'ANULADO')
BEGIN
    THROW 51608, 'El estado esperado no es valido.', 1;
END;

IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NULL
BEGIN
    THROW 51603, 'Falta una tabla requerida de Borradores NC.', 1;
END;

/* Vista previa del único objetivo y sus relaciones. */
SELECT
    N'06A_OBJETIVO' AS SECCION,
    E.ID_EMPRESA,
    E.ID_BORRADOR,
    E.FECHA,
    E.ID_CLIENTE,
    E.NOMBRE,
    E.AGENTE,
    E.MONEDA,
    E.TOTAL,
    E.ESTADO,
    E.ID_USR,
    E.REGISTRO,
    (SELECT COUNT_BIG(*)
     FROM dbo.BORR_NC_DET D
     WHERE D.ID_EMPRESA = E.ID_EMPRESA
       AND D.ID_BORRADOR = E.ID_BORRADOR) AS DETALLES,
    (SELECT COUNT_BIG(*)
     FROM dbo.BORR_NC_ADJUNTO A
     WHERE A.ID_EMPRESA = E.ID_EMPRESA
       AND A.ID_BORRADOR = E.ID_BORRADOR) AS ADJUNTOS
FROM dbo.BORR_NC_ENC E
WHERE E.ID_EMPRESA = @Empresa
  AND E.ID_BORRADOR = @IdBorrador;

IF (SELECT COUNT(*)
    FROM dbo.BORR_NC_ENC
    WHERE ID_EMPRESA = @Empresa
      AND ID_BORRADOR = @IdBorrador
      AND ESTADO = @EstadoEsperado) <> 1
BEGIN
    THROW 51604,
          'No existe exactamente un borrador con la empresa, ID y estado esperados. No se elimino nada.',
          1;
END;

IF @ConfirmarEliminacion <> 1
BEGIN
    PRINT N'MODO VISTA PREVIA: no se elimino ningun registro. Revise 06A_OBJETIVO; luego cambie @ConfirmarEliminacion a 1.';''
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Bloquea y vuelve a validar el objetivo dentro de la transacción. */
    IF (SELECT COUNT(*)
        FROM dbo.BORR_NC_ENC WITH (UPDLOCK, HOLDLOCK)
        WHERE ID_EMPRESA = @Empresa
          AND ID_BORRADOR = @IdBorrador
          AND ESTADO = @EstadoEsperado) <> 1
    BEGIN
        THROW 51605,
              'El borrador cambio o dejo de existir antes de eliminarlo.',
              1;
    END;

    DECLARE @DetallesAntes bigint =
    (
        SELECT COUNT_BIG(*)
        FROM dbo.BORR_NC_DET
        WHERE ID_EMPRESA = @Empresa
          AND ID_BORRADOR = @IdBorrador
    );

    DECLARE @AdjuntosAntes bigint =
    (
        SELECT COUNT_BIG(*)
        FROM dbo.BORR_NC_ADJUNTO
        WHERE ID_EMPRESA = @Empresa
          AND ID_BORRADOR = @IdBorrador
    );

    /* Las relaciones ON DELETE CASCADE eliminan detalles y adjuntos. */
    DELETE FROM dbo.BORR_NC_ENC
    WHERE ID_EMPRESA = @Empresa
      AND ID_BORRADOR = @IdBorrador;

    IF @@ROWCOUNT <> 1
        THROW 51606, 'No se elimino exactamente un encabezado.', 1;

    IF EXISTS
    (
        SELECT 1 FROM dbo.BORR_NC_ENC
        WHERE ID_EMPRESA = @Empresa AND ID_BORRADOR = @IdBorrador
    )
    OR EXISTS
    (
        SELECT 1 FROM dbo.BORR_NC_DET
        WHERE ID_EMPRESA = @Empresa AND ID_BORRADOR = @IdBorrador
    )
    OR EXISTS
    (
        SELECT 1 FROM dbo.BORR_NC_ADJUNTO
        WHERE ID_EMPRESA = @Empresa AND ID_BORRADOR = @IdBorrador
    )
    BEGIN
        THROW 51607,
              'La eliminación no retiró completamente el borrador y sus relaciones.',
              1;
    END;

    COMMIT TRANSACTION;

    SELECT
        N'06B_RESULTADO' AS SECCION,
        @Empresa AS ID_EMPRESA,
        @IdBorrador AS ID_BORRADOR,
        @DetallesAntes AS DETALLES_ELIMINADOS,
        @AdjuntosAntes AS ADJUNTOS_ELIMINADOS,
        N'ELIMINADO' AS RESULTADO,
        N'El correlativo de BORR_NC_SERIES no fue modificado.' AS OBSERVACION;

    PRINT N'OK: borrador de prueba eliminado de POS-SmartK66_DEV.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
