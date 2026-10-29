/* Gerencia Comercial: registra permiso y menú sin asignarlo a roles. */
USE [POS-SmartK66_DEV];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'POS-SmartK66_DEV'
    THROW 55300, 'Base incorrecta para Gerencia Comercial.', 1;
IF OBJECT_ID(N'dbo.Permiso', N'U') IS NULL OR OBJECT_ID(N'dbo.Menu', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rol', N'U') IS NULL OR OBJECT_ID(N'dbo.Rol_Permiso', N'U') IS NULL
    THROW 55301, 'Faltan tablas de seguridad requeridas.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Permiso nvarchar(100) = N'Control.GerenciaComercial.Ver';
    IF EXISTS (SELECT 1 FROM dbo.Permiso WHERE Nombre = @Permiso
               AND (Descripcion <> N'Consultar el panel de Gerencia Comercial y sus datos'
                    OR Modulo <> N'Gerencia Comercial'))
        THROW 55302, 'El permiso existente de Gerencia Comercial es incompatible.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Permiso WITH (UPDLOCK, HOLDLOCK) WHERE Nombre = @Permiso)
        INSERT dbo.Permiso (Nombre, Descripcion, Modulo)
        VALUES (@Permiso, N'Consultar el panel de Gerencia Comercial y sus datos', N'Gerencia Comercial');

    IF (SELECT COUNT(*) FROM dbo.Menu WHERE Nombre = N'GerenciaComercial'
         OR (Controller = N'GerenciaComercial' AND Action = N'Index')) > 1
        THROW 55303, 'Hay más de una entrada de menú de Gerencia Comercial.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Menu WHERE Nombre = N'GerenciaComercial'
               OR (Controller = N'GerenciaComercial' AND Action = N'Index'))
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Menu
                   WHERE (Nombre = N'GerenciaComercial'
                          OR (Controller = N'GerenciaComercial' AND Action = N'Index'))
                     AND (Menu_Padre_Id IS NOT NULL OR Nombre <> N'GerenciaComercial'
                          OR Titulo <> N'Gerencia Comercial' OR ISNULL(Action,N'') <> N'Index'
                          OR ISNULL(Controller,N'') <> N'GerenciaComercial' OR IsActive <> 1
                          OR PermisoId <> @Permiso OR ISNULL(IconName,N'') <> N'clip-stats'))
            THROW 55304, 'La entrada existente de Gerencia Comercial es incompatible.', 1;
    END
    ELSE
    BEGIN
        DECLARE @MenuId int, @Orden int;
        SELECT @MenuId = ISNULL(MAX(Menu_Id), 0) + 1 FROM dbo.Menu WITH (TABLOCKX, HOLDLOCK);
        SELECT @Orden = ISNULL(MAX(Orden), 0) + 1 FROM dbo.Menu WITH (HOLDLOCK)
        WHERE Menu_Padre_Id IS NULL;
        INSERT dbo.Menu (Menu_Id, Menu_Padre_Id, Nombre, Titulo, Action, Controller,
                          Orden, IconName, IsActive, PermisoId)
        VALUES (@MenuId, NULL, N'GerenciaComercial', N'Gerencia Comercial', N'Index',
                N'GerenciaComercial', @Orden, N'clip-stats', 1, @Permiso);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT Nombre, Descripcion, Modulo FROM dbo.Permiso
WHERE Nombre = N'Control.GerenciaComercial.Ver';
SELECT Menu_Id, Menu_Padre_Id, Titulo, Action, Controller, IsActive, PermisoId
FROM dbo.Menu WHERE Nombre = N'GerenciaComercial';
SELECT R.Nombre AS Rol, RP.Permiso_Id
FROM dbo.Rol_Permiso RP JOIN dbo.Rol R ON R.Rol_Id = RP.Rol_Id
WHERE RP.Permiso_Id = N'Control.GerenciaComercial.Ver';
GO
