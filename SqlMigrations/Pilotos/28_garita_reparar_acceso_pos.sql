/* Reparación puntual de acceso a garita después de migración 27.
   Ejecutar en POS (la base configurada como GiveContext), no en APK66.
   Vista previa por defecto. Antes de aplicar, declarar el catálogo POS exacto.
   La reparación solamente agrega los vínculos faltantes; no quita accesos. */
DECLARE @BaseEsperada sysname=NULL,@Login nvarchar(50)=N'mlopez',@Aplicar bit=0;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
BEGIN
    SELECT DB_NAME() BaseSeleccionada,@Login LoginSolicitado,
           N'VISTA PREVIA: declarar @BaseEsperada y ejecutar de nuevo; sin cambios.' Accion;
    IF @Aplicar=1 THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
    RETURN;
END;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL OR OBJECT_ID(N'dbo.Usuario_Rol',N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rol',N'U') IS NULL OR OBJECT_ID(N'dbo.Rol_Permiso',N'U') IS NULL
   OR OBJECT_ID(N'dbo.Permiso',N'U') IS NULL OR OBJECT_ID(N'dbo.GaritaCentro',N'U') IS NULL
    THROW 51000,'Falta la migración 27 o la estructura de seguridad POS.',1;
IF NULLIF(LTRIM(RTRIM(@Login)),N'') IS NULL
    THROW 51000,'Indicar un login POS.',1;
IF (SELECT COUNT(*) FROM dbo.Usuario WHERE Login=@Login)<>1
    THROW 51000,'El login POS debe existir una sola vez.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE Login=@Login AND Activo=1 AND Autenticar_Site=1)
    THROW 51000,'El usuario POS debe estar activo y habilitado para el sitio.',1;
IF (SELECT COUNT(*) FROM dbo.Rol WHERE Nombre=N'GARITA')<>1 OR
   (SELECT COUNT(*) FROM dbo.Rol WHERE Nombre=N'GARITA_ADMIN')<>1 OR
   NOT EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Garita.Rutas.Ver') OR
   NOT EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Garita.Rutas.ConfirmarSalida') OR
   NOT EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Garita.Administrar')
    THROW 51000,'Faltan roles o permisos de migración 27. Revisar la instalación.',1;

IF @Aplicar=1
BEGIN
    IF @@TRANCOUNT<>0 THROW 51000,'Ejecutar en una ventana sin transacción abierta.',1;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @UsuarioId bigint=(SELECT Usuario_Id FROM dbo.Usuario WITH(UPDLOCK,HOLDLOCK) WHERE Login=@Login);
        DECLARE @RolGarita int=(SELECT Rol_Id FROM dbo.Rol WHERE Nombre=N'GARITA');
        DECLARE @RolAdmin int=(SELECT Rol_Id FROM dbo.Rol WHERE Nombre=N'GARITA_ADMIN');
        IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Rol_Id=@RolGarita AND Permiso_Id=N'Garita.Rutas.Ver')
            INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolGarita,N'Garita.Rutas.Ver');
        IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Rol_Id=@RolGarita AND Permiso_Id=N'Garita.Rutas.ConfirmarSalida')
            INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolGarita,N'Garita.Rutas.ConfirmarSalida');
        IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Rol_Id=@RolAdmin AND Permiso_Id=N'Garita.Administrar')
            INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolAdmin,N'Garita.Administrar');
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuario_Rol WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@UsuarioId AND Rol_Id=@RolGarita)
            INSERT dbo.Usuario_Rol(Usuario_Id,Rol_Id) VALUES(@UsuarioId,@RolGarita);
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuario_Rol WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@UsuarioId AND Rol_Id=@RolAdmin)
            INSERT dbo.Usuario_Rol(Usuario_Id,Rol_Id) VALUES(@UsuarioId,@RolAdmin);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK;
        THROW;
    END CATCH;
END;

SELECT DB_NAME() Catalogo,u.Login,esperado.Rol,esperado.Permiso,
       CAST(CASE WHEN ur.Usuario_Id IS NULL THEN 0 ELSE 1 END AS bit) RolAsignado,
       CAST(CASE WHEN rp.Permiso_Id IS NULL THEN 0 ELSE 1 END AS bit) PermisoEnRol,
       CAST(CASE WHEN ur.Usuario_Id IS NOT NULL AND rp.Permiso_Id IS NOT NULL THEN 1 ELSE 0 END AS bit) AccesoEfectivo,
       CASE WHEN @Aplicar=1 THEN N'APLICADO' ELSE N'VISTA PREVIA' END Estado
FROM dbo.Usuario u
CROSS JOIN (VALUES (N'GARITA',N'Garita.Rutas.Ver'),
                   (N'GARITA',N'Garita.Rutas.ConfirmarSalida'),
                   (N'GARITA_ADMIN',N'Garita.Administrar')) esperado(Rol,Permiso)
JOIN dbo.Rol r ON r.Nombre=esperado.Rol
LEFT JOIN dbo.Usuario_Rol ur ON ur.Usuario_Id=u.Usuario_Id AND ur.Rol_Id=r.Rol_Id
LEFT JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=r.Rol_Id AND rp.Permiso_Id=esperado.Permiso
WHERE u.Login=@Login
ORDER BY esperado.Rol,esperado.Permiso;
