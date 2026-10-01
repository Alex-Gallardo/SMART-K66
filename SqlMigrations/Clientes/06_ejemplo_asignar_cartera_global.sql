/* Plantilla OPCIONAL: editar @RolNombre antes de ejecutar.
   Puede repetirse con otros roles. No es necesaria para instalar 05. */
USE [POS-SmartK66_DEV];
GO
SET XACT_ABORT ON;
DECLARE @RolNombre nvarchar(150) = N'ESCRIBIR_NOMBRE_EXACTO_DEL_ROL';
IF @RolNombre=N'ESCRIBIR_NOMBRE_EXACTO_DEL_ROL'
    THROW 55300, 'Edite @RolNombre antes de ejecutar.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Nombre=@RolNombre)
    THROW 55301, 'El rol indicado no existe.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.Permiso
               WHERE Nombre=N'Control.Clientes.CarteraGlobal')
    THROW 55302, 'Ejecute primero 05_cartera_por_usuario.sql.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT dbo.Rol_Permiso (Rol_Id,Permiso_Id)
    SELECT R.Rol_Id,P.Nombre FROM dbo.Rol R
    CROSS JOIN (VALUES (N'Control.Clientes.Modulo'),
                       (N'Control.Clientes.CarteraGlobal')) P(Nombre)
    WHERE R.Nombre=@RolNombre
      AND NOT EXISTS (SELECT 1 FROM dbo.Rol_Permiso RP WITH (UPDLOCK,HOLDLOCK)
                      WHERE RP.Rol_Id=R.Rol_Id AND RP.Permiso_Id=P.Nombre);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
SELECT R.Nombre AS Rol,RP.Permiso_Id AS Permiso
FROM dbo.Rol R JOIN dbo.Rol_Permiso RP ON RP.Rol_Id=R.Rol_Id
WHERE R.Nombre=@RolNombre AND RP.Permiso_Id LIKE N'Control.Clientes.%'
ORDER BY RP.Permiso_Id;
GO
