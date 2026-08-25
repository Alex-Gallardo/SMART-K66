/* =============================================================================
   BORRADORES NC — PERMISOS, MENU Y ROLES
   Destino autorizado: POS-SmartK66 (PRUEBAS)

   Alcance autorizado
   ------------------
   - Registra los cinco permisos usados por BorradorNcController.
   - Registra dos entradas de menu bajo el mismo padre de ReciboCaja.
   - Asigna los cinco permisos a Administrador y AdministradorK66.
   - Asigna Ver y Guardar a VendedorK66 y Telemarketing.
   - No asigna permisos a DespachosK66 ni PreciosK66.

   Garantias
   ---------
   - No elimina ni actualiza filas existentes.
   - Inserta exclusivamente filas faltantes y valida las ya existentes.
   - Calcula Menu_Id y Orden bajo bloqueo exclusivo para evitar colisiones.
   - Usa una transaccion y revierte todo ante cualquier error.
   - Es reejecutable; se detiene ante configuraciones incompatibles.
   - No cambia BorradorNC.OmitirPermisos: el bypass sigue activo durante pruebas.

   Ejecucion
   ---------
   1. Abra una ventana NUEVA en SSMS y conectese al servidor de pruebas.
   2. Ejecute el archivo COMPLETO, sin seleccionar bloques parciales.
   3. Envie las salidas 02A a 02E y la pestana Messages.
   ============================================================================= */

USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

EXEC sys.sp_set_session_context
     @key = N'BorradorNcSeguridadValidada',
     @value = 0;
GO

IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    THROW 52000,
          'SEGURIDAD: esta migracion solo esta autorizada para POS-SmartK66.',
          1;
END;

IF CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) <> N'READ_WRITE'
BEGIN
    THROW 52001, 'La base autorizada no esta disponible para escritura.', 1;
END;

IF OBJECT_ID(N'dbo.Permiso', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Menu', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rol', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Rol_Permiso', N'U') IS NULL
BEGIN
    THROW 52002, 'Faltan tablas requeridas para configurar seguridad y menu.', 1;
END;
GO

/* 02A — Identidad inequívoca del entorno ---------------------------------- */
SELECT
    N'02A_ENTORNO' AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME() AS BASE_ACTUAL,
    CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL,
    CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) AS ACTUALIZABLE,
    SYSDATETIME() AS FECHA_EJECUCION;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ResultadoBloqueo int;
    EXEC @ResultadoBloqueo = sys.sp_getapplock
         @Resource = N'BorradorNc.ConfiguracionSeguridad',
         @LockMode = N'Exclusive',
         @LockOwner = N'Transaction',
         @LockTimeout = 5000;

    IF @ResultadoBloqueo < 0
        THROW 52003, 'No fue posible obtener el bloqueo de configuracion.', 1;

    DECLARE @PermisosEsperados TABLE
    (
        NOMBRE      nvarchar(100) NOT NULL PRIMARY KEY,
        DESCRIPCION nvarchar(500) NOT NULL,
        MODULO      nvarchar(200) NOT NULL
    );

    INSERT INTO @PermisosEsperados (NOMBRE, DESCRIPCION, MODULO)
    VALUES
        (N'Control.BorradorNC.Ver',
         N'Ver y capturar borradores de nota de crédito', N'Borradores NC'),
        (N'Control.BorradorNC.Guardar',
         N'Guardar borradores de nota de crédito', N'Borradores NC'),
        (N'Control.BorradorNC.Autorizar',
         N'Autorizar o rechazar borradores', N'Borradores NC'),
        (N'Control.BorradorNC.Anular',
         N'Anular un borrador ya autorizado', N'Borradores NC'),
        (N'Control.BorradorNC.VerTodos',
         N'Ver borradores de todos los usuarios', N'Borradores NC');

    IF EXISTS
    (
        SELECT 1
        FROM @PermisosEsperados E
        JOIN dbo.Permiso P
          ON P.Nombre = E.NOMBRE
        WHERE P.Descripcion <> E.DESCRIPCION
           OR P.Modulo <> E.MODULO
    )
    BEGIN
        THROW 52004,
              'Existe un permiso BorradorNC con descripcion o modulo incompatible.',
              1;
    END;

    INSERT INTO dbo.Permiso (Nombre, Descripcion, Modulo)
    SELECT E.NOMBRE, E.DESCRIPCION, E.MODULO
    FROM @PermisosEsperados E
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Permiso P WITH (UPDLOCK, HOLDLOCK)
        WHERE P.Nombre = E.NOMBRE
    );

    DECLARE @RolesEsperados TABLE
    (
        NOMBRE nvarchar(150) NOT NULL PRIMARY KEY
    );

    INSERT INTO @RolesEsperados (NOMBRE)
    VALUES
        (N'Administrador'),
        (N'AdministradorK66'),
        (N'Telemarketing'),
        (N'VendedorK66');

    IF EXISTS
    (
        SELECT E.NOMBRE
        FROM @RolesEsperados E
        LEFT JOIN dbo.Rol R
          ON R.Nombre = E.NOMBRE
        GROUP BY E.NOMBRE
        HAVING COUNT(R.Rol_Id) <> 1
    )
    BEGIN
        THROW 52005, 'Falta un rol objetivo o su nombre esta duplicado.', 1;
    END;

    DECLARE @AsignacionesEsperadas TABLE
    (
        ROL_NOMBRE nvarchar(150) NOT NULL,
        PERMISO    nvarchar(100) NOT NULL,
        PRIMARY KEY (ROL_NOMBRE, PERMISO)
    );

    INSERT INTO @AsignacionesEsperadas (ROL_NOMBRE, PERMISO)
    VALUES
        (N'Administrador',    N'Control.BorradorNC.Ver'),
        (N'Administrador',    N'Control.BorradorNC.Guardar'),
        (N'Administrador',    N'Control.BorradorNC.Autorizar'),
        (N'Administrador',    N'Control.BorradorNC.Anular'),
        (N'Administrador',    N'Control.BorradorNC.VerTodos'),
        (N'AdministradorK66', N'Control.BorradorNC.Ver'),
        (N'AdministradorK66', N'Control.BorradorNC.Guardar'),
        (N'AdministradorK66', N'Control.BorradorNC.Autorizar'),
        (N'AdministradorK66', N'Control.BorradorNC.Anular'),
        (N'AdministradorK66', N'Control.BorradorNC.VerTodos'),
        (N'Telemarketing',    N'Control.BorradorNC.Ver'),
        (N'Telemarketing',    N'Control.BorradorNC.Guardar'),
        (N'VendedorK66',      N'Control.BorradorNC.Ver'),
        (N'VendedorK66',      N'Control.BorradorNC.Guardar');

    INSERT INTO dbo.Rol_Permiso (Rol_Id, Permiso_Id)
    SELECT R.Rol_Id, A.PERMISO
    FROM @AsignacionesEsperadas A
    JOIN dbo.Rol R
      ON R.Nombre = A.ROL_NOMBRE
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Rol_Permiso RP WITH (UPDLOCK, HOLDLOCK)
        WHERE RP.Rol_Id = R.Rol_Id
          AND RP.Permiso_Id = A.PERMISO
    );

    DECLARE @MenuPadreId int;
    DECLARE @CantidadPadres int;

    SELECT
        @MenuPadreId = MIN(Menu_Padre_Id),
        @CantidadPadres = COUNT(DISTINCT Menu_Padre_Id)
    FROM dbo.Menu
    WHERE Controller = N'ReciboCaja';

    IF @CantidadPadres <> 1 OR @MenuPadreId IS NULL OR @MenuPadreId <> 543
    BEGIN
        THROW 52006,
              'El menu padre de ReciboCaja no coincide con el diagnostico autorizado.',
              1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Menu
        WHERE Controller = N'BorradorNc'
          AND (Action IS NULL OR Action NOT IN (N'Index', N'Autorizaciones'))
    )
    OR (SELECT COUNT(*) FROM dbo.Menu
        WHERE Controller = N'BorradorNc' AND Action = N'Index') > 1
    OR (SELECT COUNT(*) FROM dbo.Menu
        WHERE Controller = N'BorradorNc' AND Action = N'Autorizaciones') > 1
    BEGIN
        THROW 52007, 'Existe una configuracion de menu BorradorNc incompatible.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Menu
        WHERE Controller = N'BorradorNc'
          AND Action = N'Index'
          AND
          (
              ISNULL(Menu_Padre_Id, -1) <> @MenuPadreId
              OR Nombre <> N'BorradoresNC'
              OR Titulo <> N'Borradores NC'
              OR ISNULL(IconName, N'') <> N'clip-file-plus'
              OR IsActive <> 1
              OR PermisoId <> N'Control.BorradorNC.Ver'
          )
    )
    OR EXISTS
    (
        SELECT 1
        FROM dbo.Menu
        WHERE Controller = N'BorradorNc'
          AND Action = N'Autorizaciones'
          AND
          (
              ISNULL(Menu_Padre_Id, -1) <> @MenuPadreId
              OR Nombre <> N'AutorizacionBorradoresNC'
              OR Titulo <> N'Autorización NC'
              OR ISNULL(IconName, N'') <> N'clip-file-check'
              OR IsActive <> 1
              OR PermisoId <> N'Control.BorradorNC.Autorizar'
          )
    )
    BEGIN
        THROW 52008, 'Una entrada de menu BorradorNc no coincide con el contrato.', 1;
    END;

    DECLARE @SiguienteMenuId int;
    DECLARE @SiguienteOrden int;

    SELECT @SiguienteMenuId = ISNULL(MAX(Menu_Id), 0) + 1
    FROM dbo.Menu WITH (TABLOCKX, HOLDLOCK);

    SELECT @SiguienteOrden = ISNULL(MAX(Orden), 0) + 1
    FROM dbo.Menu WITH (HOLDLOCK)
    WHERE Menu_Padre_Id = @MenuPadreId;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Menu
        WHERE Controller = N'BorradorNc' AND Action = N'Index'
    )
    BEGIN
        INSERT INTO dbo.Menu
            (Menu_Id, Menu_Padre_Id, Nombre, Titulo, Action, Controller,
             Orden, IconName, IsActive, PermisoId)
        VALUES
            (@SiguienteMenuId, @MenuPadreId, N'BorradoresNC', N'Borradores NC',
             N'Index', N'BorradorNc', @SiguienteOrden, N'clip-file-plus', 1,
             N'Control.BorradorNC.Ver');

        SET @SiguienteMenuId += 1;
        SET @SiguienteOrden += 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Menu
        WHERE Controller = N'BorradorNc' AND Action = N'Autorizaciones'
    )
    BEGIN
        INSERT INTO dbo.Menu
            (Menu_Id, Menu_Padre_Id, Nombre, Titulo, Action, Controller,
             Orden, IconName, IsActive, PermisoId)
        VALUES
            (@SiguienteMenuId, @MenuPadreId,
             N'AutorizacionBorradoresNC', N'Autorización NC',
             N'Autorizaciones', N'BorradorNc', @SiguienteOrden,
             N'clip-file-check', 1, N'Control.BorradorNC.Autorizar');
    END;

    IF
    (
        SELECT COUNT(*)
        FROM @PermisosEsperados E
        JOIN dbo.Permiso P
          ON P.Nombre = E.NOMBRE
         AND P.Descripcion = E.DESCRIPCION
         AND P.Modulo = E.MODULO
    ) <> 5
    BEGIN
        THROW 52009, 'No quedaron configurados los cinco permisos BorradorNC.', 1;
    END;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.Menu
        WHERE Controller = N'BorradorNc'
          AND Action IN (N'Index', N'Autorizaciones')
    ) <> 2
    BEGIN
        THROW 52010, 'No quedaron configuradas las dos entradas de menu.', 1;
    END;

    IF
    (
        SELECT COUNT(*)
        FROM @AsignacionesEsperadas A
        JOIN dbo.Rol R
          ON R.Nombre = A.ROL_NOMBRE
        JOIN dbo.Rol_Permiso RP
          ON RP.Rol_Id = R.Rol_Id
         AND RP.Permiso_Id = A.PERMISO
    ) <> 14
    BEGIN
        THROW 52011, 'No quedaron configuradas las catorce asignaciones objetivo.', 1;
    END;

    COMMIT TRANSACTION;

    EXEC sys.sp_set_session_context
         @key = N'BorradorNcSeguridadValidada',
         @value = 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

IF ISNULL(TRY_CONVERT(int,
       SESSION_CONTEXT(N'BorradorNcSeguridadValidada')), 0) <> 1
BEGIN
    THROW 52012,
          'La configuracion BorradorNC no finalizo; no se reportara exito.',
          1;
END;

/* 02B — Permisos ----------------------------------------------------------- */
SELECT
    N'02B_PERMISOS' AS SECCION,
    Nombre,
    Descripcion,
    Modulo
FROM dbo.Permiso
WHERE Nombre IN
      (N'Control.BorradorNC.Ver',
       N'Control.BorradorNC.Guardar',
       N'Control.BorradorNC.Autorizar',
       N'Control.BorradorNC.Anular',
       N'Control.BorradorNC.VerTodos')
ORDER BY Nombre;

/* 02C — Menu --------------------------------------------------------------- */
SELECT
    N'02C_MENU' AS SECCION,
    Menu_Id,
    Menu_Padre_Id,
    Nombre,
    Titulo,
    Action,
    Controller,
    Orden,
    IconName,
    IsActive,
    PermisoId
FROM dbo.Menu
WHERE Controller = N'BorradorNc'
ORDER BY Orden, Menu_Id;

/* 02D — Asignaciones por rol ---------------------------------------------- */
SELECT
    N'02D_ROL_PERMISO' AS SECCION,
    R.Rol_Id,
    R.Nombre AS ROL,
    RP.Permiso_Id
FROM dbo.Rol R
JOIN dbo.Rol_Permiso RP
  ON RP.Rol_Id = R.Rol_Id
WHERE RP.Permiso_Id IN
      (N'Control.BorradorNC.Ver',
       N'Control.BorradorNC.Guardar',
       N'Control.BorradorNC.Autorizar',
       N'Control.BorradorNC.Anular',
       N'Control.BorradorNC.VerTodos')
ORDER BY R.Nombre, RP.Permiso_Id;

/* 02E — Resumen ------------------------------------------------------------ */
SELECT
    N'02E_RESUMEN' AS SECCION,
    V.VALIDACION,
    V.ESPERADO,
    V.REAL,
    CASE WHEN V.ESPERADO = V.REAL THEN N'OK' ELSE N'REVISAR' END AS RESULTADO
FROM
(
    SELECT N'Permisos objetivo' AS VALIDACION,
           CONVERT(bigint, 5) AS ESPERADO,
           COUNT_BIG(*) AS REAL
    FROM dbo.Permiso
    WHERE Nombre IN
          (N'Control.BorradorNC.Ver',
           N'Control.BorradorNC.Guardar',
           N'Control.BorradorNC.Autorizar',
           N'Control.BorradorNC.Anular',
           N'Control.BorradorNC.VerTodos')

    UNION ALL

    SELECT N'Entradas de menu', 2, COUNT_BIG(*)
    FROM dbo.Menu
    WHERE Controller = N'BorradorNc'
      AND Action IN (N'Index', N'Autorizaciones')

    UNION ALL

    SELECT N'Asignaciones objetivo', 14, COUNT_BIG(*)
    FROM
    (
        SELECT N'Administrador' AS ROL, N'Control.BorradorNC.Ver' AS PERMISO
        UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.Guardar'
        UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.Autorizar'
        UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.Anular'
        UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.VerTodos'
        UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Ver'
        UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Guardar'
        UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Autorizar'
        UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Anular'
        UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.VerTodos'
        UNION ALL SELECT N'Telemarketing',    N'Control.BorradorNC.Ver'
        UNION ALL SELECT N'Telemarketing',    N'Control.BorradorNC.Guardar'
        UNION ALL SELECT N'VendedorK66',      N'Control.BorradorNC.Ver'
        UNION ALL SELECT N'VendedorK66',      N'Control.BorradorNC.Guardar'
    ) E
    JOIN dbo.Rol R
      ON R.Nombre = E.ROL
    JOIN dbo.Rol_Permiso RP
      ON RP.Rol_Id = R.Rol_Id
     AND RP.Permiso_Id = E.PERMISO
) V
ORDER BY V.VALIDACION;

PRINT 'OK: permisos, menu y roles de BorradorNC validados en POS-SmartK66.';
GO
