/* Amplía la descripción del permiso existente de anulación.
   Destino: POS-SmartK66_DEV. Ejecute el archivo completo en SSMS.
   No crea permisos ni modifica asignaciones de roles. Es reejecutable. */
USE [POS-SmartK66_DEV];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

IF DB_NAME() <> N'POS-SmartK66_DEV'
    THROW 58100, 'SEGURIDAD: script autorizado únicamente para POS-SmartK66_DEV.', 1;
IF OBJECT_ID(N'dbo.Permiso', N'U') IS NULL
    THROW 58101, 'Falta dbo.Permiso. Ejecute primero la configuración base.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF (SELECT COUNT_BIG(*) FROM dbo.Permiso
        WHERE Nombre = N'Control.BorradorNC.Anular') <> 1
        THROW 58102, 'Se esperaba exactamente un permiso Control.BorradorNC.Anular.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.Permiso
        WHERE Nombre = N'Control.BorradorNC.Anular'
          AND (Modulo <> N'Borradores NC' OR Descripcion NOT IN (
              N'Anular un borrador ya autorizado',
              N'Anular borradores pendientes, autorizados o rechazados'))
    )
        THROW 58103, 'El permiso Anular tiene una definición inesperada.', 1;

    UPDATE dbo.Permiso
       SET Descripcion = N'Anular borradores pendientes, autorizados o rechazados'
     WHERE Nombre = N'Control.BorradorNC.Anular'
       AND Descripcion = N'Anular un borrador ya autorizado';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT Nombre, Descripcion, Modulo
FROM dbo.Permiso
WHERE Nombre = N'Control.BorradorNC.Anular';
GO
