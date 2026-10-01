/* Variante para POS-SmartK66 (PRUEBAS). No crea usuarios ni asigna permisos a roles. */
/* CLIENTES CRM: permisos y menú. Base validada con diagnóstico 00. */
USE [POS-SmartK66];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() <> N'POS-SmartK66'
    THROW 55100, 'Base incorrecta para permisos Clientes CRM.', 1;
IF OBJECT_ID(N'dbo.CRM_CLIENTE', N'U') IS NULL OR OBJECT_ID(N'dbo.CRM_SOLICITUD', N'U') IS NULL
    THROW 55101, 'Ejecute primero 01_estructura_clientes.sql.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @P TABLE (Nombre nvarchar(100) PRIMARY KEY, Descripcion nvarchar(500));
    INSERT @P VALUES
      (N'Control.Clientes.Modulo', N'Mostrar el módulo Clientes'),
      (N'Control.Clientes.Ver', N'Ver solicitudes propias y fichas vinculadas'),
      (N'Control.Clientes.Crear', N'Crear, editar y enviar solicitudes propias'),
      (N'Control.Clientes.Dashboard', N'Consultar y resolver solicitudes desde Créditos'),
      (N'Control.Clientes.Administrar', N'Administrar fichas SQL y registrar códigos SAP');

    IF EXISTS (SELECT 1 FROM @P E JOIN dbo.Permiso P ON P.Nombre=E.Nombre
               WHERE P.Descripcion<>E.Descripcion OR P.Modulo<>N'Clientes')
        THROW 55103, 'Un permiso Clientes existente no coincide con el contrato.', 1;
    INSERT dbo.Permiso (Nombre,Descripcion,Modulo)
    SELECT E.Nombre,E.Descripcion,N'Clientes' FROM @P E
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Permiso P WITH (UPDLOCK,HOLDLOCK) WHERE P.Nombre=E.Nombre);

    DECLARE @Raiz int;
    SELECT @Raiz=Menu_Id FROM dbo.Menu WITH (UPDLOCK,HOLDLOCK)
    WHERE Nombre=N'ClientesCrm' AND Menu_Padre_Id IS NULL;
    IF @Raiz IS NULL
    BEGIN
        SELECT @Raiz=ISNULL(MAX(Menu_Id),0)+1 FROM dbo.Menu WITH (TABLOCKX,HOLDLOCK);
        INSERT dbo.Menu (Menu_Id,Menu_Padre_Id,Nombre,Titulo,Action,Controller,
                          Orden,IconName,IsActive,PermisoId)
        VALUES (@Raiz,NULL,N'ClientesCrm',N'Clientes',NULL,NULL,
                @Raiz,N'clip-users',1,N'Control.Clientes.Modulo');
    END
    ELSE IF EXISTS (SELECT 1 FROM dbo.Menu WHERE Menu_Id=@Raiz AND
                   (Titulo<>N'Clientes' OR PermisoId<>N'Control.Clientes.Modulo' OR IsActive<>1))
        THROW 55104, 'El menú raíz Clientes existente es incompatible.', 1;

    DECLARE @M TABLE (Nombre nvarchar(60), Titulo nvarchar(80), Accion nvarchar(50),
                      Permiso nvarchar(100), Orden int, Icono nvarchar(50));
    INSERT @M VALUES
      (N'ClientesSolicitudes',N'Solicitudes de clientes',N'Index',N'Control.Clientes.Ver',1,N'clip-file-text'),
      (N'ClientesDashboard',N'Dashboard',N'Dashboard',N'Control.Clientes.Dashboard',2,N'clip-grid'),
      (N'ClientesCartera',N'Cartera CRM',N'Clientes',N'Control.Clientes.Dashboard',3,N'clip-users');
    IF EXISTS (SELECT 1 FROM @M E JOIN dbo.Menu M ON M.Nombre=E.Nombre
               WHERE M.Menu_Padre_Id<>@Raiz OR M.Titulo<>E.Titulo
                 OR M.Action<>E.Accion OR M.Controller<>N'ClientesCrm'
                 OR (M.PermisoId<>E.Permiso AND NOT
                     (E.Nombre=N'ClientesCartera' AND
                      M.PermisoId=N'Control.Clientes.Modulo')) OR M.IsActive<>1)
        THROW 55105, 'Una entrada de Clientes CRM existente es incompatible.', 1;
    DECLARE @Nombre nvarchar(60),@Titulo nvarchar(80),@Accion nvarchar(50),
            @Permiso nvarchar(100),@Orden int,@Icono nvarchar(50),@Id int;
    DECLARE menu_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Nombre,Titulo,Accion,Permiso,Orden,Icono FROM @M ORDER BY Orden;
    OPEN menu_cursor;
    FETCH NEXT FROM menu_cursor INTO @Nombre,@Titulo,@Accion,@Permiso,@Orden,@Icono;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE Nombre=@Nombre)
        BEGIN
            SELECT @Id=ISNULL(MAX(Menu_Id),0)+1 FROM dbo.Menu WITH (TABLOCKX,HOLDLOCK);
            INSERT dbo.Menu (Menu_Id,Menu_Padre_Id,Nombre,Titulo,Action,Controller,
                              Orden,IconName,IsActive,PermisoId)
            VALUES (@Id,@Raiz,@Nombre,@Titulo,@Accion,N'ClientesCrm',@Orden,@Icono,1,@Permiso);
        END;
        FETCH NEXT FROM menu_cursor INTO @Nombre,@Titulo,@Accion,@Permiso,@Orden,@Icono;
    END;
    CLOSE menu_cursor;
    DEALLOCATE menu_cursor;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
