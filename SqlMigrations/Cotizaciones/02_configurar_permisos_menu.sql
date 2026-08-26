/* COTIZACIONES — PERMISOS, ROLES Y MENÚ. Destino: POS-SmartK66 pruebas. */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'POS-SmartK66'
    THROW 53100, 'SEGURIDAD: este script solo se ejecuta en POS-SmartK66.', 1;

IF OBJECT_ID(N'dbo.Permiso', N'U') IS NULL OR OBJECT_ID(N'dbo.Menu', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rol', N'U') IS NULL OR OBJECT_ID(N'dbo.Rol_Permiso', N'U') IS NULL
    THROW 53101, 'Faltan tablas de seguridad requeridas.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Permisos TABLE
    (
        Nombre nvarchar(100) PRIMARY KEY,
        Descripcion nvarchar(500),
        Modulo nvarchar(200)
    );
    INSERT INTO @Permisos VALUES
      (N'Control.Cotizaciones.Ver', N'Acceder al módulo y ver cotizaciones propias', N'Cotizaciones'),
      (N'Control.Cotizaciones.Crear', N'Crear cotizaciones', N'Cotizaciones'),
      (N'Control.Cotizaciones.Anular', N'Anular cotizaciones con trazabilidad', N'Cotizaciones'),
      (N'Control.Cotizaciones.VerTodos', N'Ver cotizaciones de todos los usuarios en empresas asignadas', N'Cotizaciones');

    IF EXISTS
    (
        SELECT 1 FROM @Permisos E JOIN dbo.Permiso P ON P.Nombre = E.Nombre
        WHERE P.Descripcion <> E.Descripcion OR P.Modulo <> E.Modulo
    )
        THROW 53102, 'Existe un permiso Cotizaciones con contrato incompatible.', 1;

    INSERT INTO dbo.Permiso (Nombre, Descripcion, Modulo)
    SELECT E.Nombre, E.Descripcion, E.Modulo
    FROM @Permisos E
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Permiso P WITH (UPDLOCK, HOLDLOCK) WHERE P.Nombre = E.Nombre);

    DECLARE @Asignaciones TABLE (Rol nvarchar(150), Permiso nvarchar(100), PRIMARY KEY (Rol, Permiso));
    INSERT INTO @Asignaciones VALUES
      (N'Administrador', N'Control.Cotizaciones.Ver'),
      (N'Administrador', N'Control.Cotizaciones.Crear'),
      (N'Administrador', N'Control.Cotizaciones.Anular'),
      (N'Administrador', N'Control.Cotizaciones.VerTodos'),
      (N'AdministradorK66', N'Control.Cotizaciones.Ver'),
      (N'AdministradorK66', N'Control.Cotizaciones.Crear'),
      (N'AdministradorK66', N'Control.Cotizaciones.Anular'),
      (N'AdministradorK66', N'Control.Cotizaciones.VerTodos'),
      (N'Telemarketing', N'Control.Cotizaciones.Ver'),
      (N'Telemarketing', N'Control.Cotizaciones.Crear'),
      (N'VendedorK66', N'Control.Cotizaciones.Ver'),
      (N'VendedorK66', N'Control.Cotizaciones.Crear');

    IF EXISTS
    (
        SELECT A.Rol FROM @Asignaciones A
        LEFT JOIN dbo.Rol R ON R.Nombre = A.Rol
        GROUP BY A.Rol HAVING COUNT(DISTINCT R.Rol_Id) <> 1
    )
        THROW 53103, 'Falta uno de los roles objetivo o está duplicado.', 1;

    INSERT INTO dbo.Rol_Permiso (Rol_Id, Permiso_Id)
    SELECT R.Rol_Id, A.Permiso
    FROM @Asignaciones A JOIN dbo.Rol R ON R.Nombre = A.Rol
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Rol_Permiso RP WITH (UPDLOCK, HOLDLOCK)
        WHERE RP.Rol_Id = R.Rol_Id AND RP.Permiso_Id = A.Permiso
    );

    DECLARE @Padre int, @CantidadPadres int;
    SELECT @Padre = MIN(Menu_Padre_Id), @CantidadPadres = COUNT(DISTINCT Menu_Padre_Id)
    FROM dbo.Menu WHERE Controller = N'ReciboCaja';
    IF @CantidadPadres <> 1 OR @Padre IS NULL
        THROW 53104, 'No se pudo resolver inequívocamente el menú padre de ReciboCaja.', 1;

    IF (SELECT COUNT(*) FROM dbo.Menu WHERE Controller = N'Cotizacion' AND Action = N'Index') > 1
        THROW 53105, 'Hay más de una entrada de menú Cotizaciones.', 1;

    IF EXISTS
    (
        SELECT 1 FROM dbo.Menu
        WHERE Controller = N'Cotizacion' AND Action = N'Index'
          AND (ISNULL(Menu_Padre_Id,-1) <> @Padre OR Titulo <> N'Cotizaciones'
               OR IsActive <> 1 OR PermisoId <> N'Control.Cotizaciones.Ver')
    )
        THROW 53106, 'La entrada existente de Cotizaciones es incompatible.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE Controller = N'Cotizacion' AND Action = N'Index')
    BEGIN
        DECLARE @MenuId int, @Orden int;
        SELECT @MenuId = ISNULL(MAX(Menu_Id),0) + 1 FROM dbo.Menu WITH (TABLOCKX, HOLDLOCK);
        SELECT @Orden = ISNULL(MAX(Orden),0) + 1 FROM dbo.Menu WITH (HOLDLOCK) WHERE Menu_Padre_Id = @Padre;
        INSERT INTO dbo.Menu
            (Menu_Id, Menu_Padre_Id, Nombre, Titulo, Action, Controller,
             Orden, IconName, IsActive, PermisoId)
        VALUES
            (@MenuId, @Padre, N'Cotizaciones', N'Cotizaciones', N'Index',
             N'Cotizacion', @Orden, N'clip-file', 1, N'Control.Cotizaciones.Ver');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT N'PERMISOS' AS SECCION, Nombre, Descripcion, Modulo
FROM dbo.Permiso WHERE Nombre LIKE N'Control.Cotizaciones.%' ORDER BY Nombre;
SELECT N'MENU' AS SECCION, Menu_Id, Menu_Padre_Id, Titulo, Action, Controller,
       Orden, IconName, IsActive, PermisoId
FROM dbo.Menu WHERE Controller = N'Cotizacion';
SELECT N'ROLES' AS SECCION, R.Nombre AS ROL, RP.Permiso_Id
FROM dbo.Rol R JOIN dbo.Rol_Permiso RP ON RP.Rol_Id = R.Rol_Id
WHERE RP.Permiso_Id LIKE N'Control.Cotizaciones.%'
ORDER BY R.Nombre, RP.Permiso_Id;
GO
