/* POS: crea SOLO el permiso de administracion. No crea roles ni asignaciones.
   Revisar vista previa; asignar luego desde el panel de permisos existente. */
DECLARE @BaseEsperada sysname=N'POS-SmartK66_DEV';
DECLARE @Aplicar bit=0;
IF DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar la base POS declarada antes de ejecutar.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,N'Pilotos.Administrar' AS Permiso,
        N'VISTA PREVIA: crear solo el permiso si falta; sin roles ni asignaciones.' AS Accion;
    RETURN;
END;
IF OBJECT_ID(N'dbo.Permiso',N'U') IS NULL
    THROW 51000,'Falta la tabla Permiso de POS.',1;
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 3000;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'Pilotos.Administrar')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo)
        VALUES(N'Pilotos.Administrar',N'Administrar vinculos, centros y vehiculos de pilotos',N'Pilotos');
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT Nombre,Descripcion,Modulo FROM dbo.Permiso WHERE Nombre=N'Pilotos.Administrar';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
