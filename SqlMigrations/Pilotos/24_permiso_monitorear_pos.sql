/* POS: registra el permiso de consulta global. No crea roles ni asigna permisos a usuarios. */
SET NOCOUNT ON;
DECLARE @BaseEsperada sysname=N'POS-SmartK66_DEV', @Aplicar bit=0;
IF DB_NAME()<>@BaseEsperada THROW 51000,'Seleccionar la base POS exacta.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
SELECT DB_NAME() BaseSeleccionada,N'Pilotos.Monitorear' Permiso,@Aplicar Aplicar,
    CASE WHEN EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Pilotos.Monitorear') THEN N'EXISTE' ELSE N'POR CREAR' END Estado;
IF @Aplicar=0 RETURN;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 3000;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'Pilotos.Monitorear')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo)
        VALUES(N'Pilotos.Monitorear',N'Consultar globalmente rutas, resultados, fotos y actividad de pilotos',N'Pilotos');
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT Nombre,Descripcion,Modulo FROM dbo.Permiso WHERE Nombre=N'Pilotos.Monitorear';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
