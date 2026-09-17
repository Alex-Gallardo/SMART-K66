/* MANUAL. Crear en el catalogo de rutas, despues de revisar/probar el contrato.
   Solo cambia visita, resultado y motivo. Conserva horas y observaciones.
   El caller debe autorizar al piloto y usar una transaccion con el cierre.
   No modifica procedimientos existentes, encabezados ni documentos financieros. */
DECLARE @BaseEsperada sysname=NULL, @Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada, N'VISTA PREVIA: crear portal_piloto_guardar_resultado; sin cambios.' AS Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000, 'Declarar y seleccionar el catalogo de rutas destino exacto.', 1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000, 'Usar una ventana nueva sin transacciones ni opciones de sesion modificadas.', 1;
IF OBJECT_ID(N'dbo.portal_piloto_guardar_resultado') IS NOT NULL
    THROW 51000, 'El procedimiento ya existe. Comparar la definicion; no sobrescribir.', 1;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    EXEC sys.sp_executesql N'CREATE PROCEDURE dbo.portal_piloto_guardar_resultado
        @rowid int, @idruta nvarchar(15), @visito bit, @entrega nvarchar(50), @motivo nvarchar(150)
    AS
    BEGIN
        SET NOCOUNT ON;
        IF @@TRANCOUNT=0 THROW 51000, ''Requiere una transaccion de cierre.'', 1;
        IF @visito IS NULL OR @entrega IS NULL OR @entrega NOT IN(N''ENTREGADO'',N''NO ENTREGADO'',N''INCIDENCIA'')
            THROW 51000, ''Resultado no valido.'', 1;
        IF @entrega=N''ENTREGADO'' AND @visito<>1 THROW 51000, ''Entrega requiere visita.'', 1;
        IF @entrega<>N''ENTREGADO'' AND NULLIF(LTRIM(RTRIM(@motivo)),N'''') IS NULL
            THROW 51000, ''El resultado requiere motivo.'', 1;
        UPDATE d SET MO_VISITO=@visito,MO_ENTREGA=@entrega,MO_MOTIVO=@motivo
        FROM dbo.RT_RUTAS_DET d JOIN dbo.RT_RUTAS r ON r.ID_RUTA=d.ID_RUTA
        WHERE d.ROWID=@rowid AND d.ID_RUTA=@idruta AND r.STATUS=N''E'' AND ISNULL(r.LIQUIDADO,0)=0;
        IF @@ROWCOUNT<>1 THROW 51000, ''Documento no disponible para completar.'', 1;
    END;';
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
