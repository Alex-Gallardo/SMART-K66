/* =============================================================================
   BORRADORES NC — DASHBOARD DE CREDITOS Y BITACORA (DESARROLLO)
   Destino autorizado: POS-SmartK66_DEV

   Crea, de forma idempotente:
   - dbo.BORR_NC_BITACORA (eventos append-only de la aplicación).
   - Permiso Control.BorradorNC.Dashboard.
   - Menú BorradorNc/DashboardBNC.

   No crea roles ni asigna permisos. Esa asignación se realiza manualmente.
   Ejecute el archivo completo en una ventana nueva de SSMS.
   ============================================================================= */
USE [POS-SmartK66_DEV];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

IF DB_NAME() <> N'POS-SmartK66_DEV'
    THROW 56100, 'SEGURIDAD: script autorizado únicamente para POS-SmartK66_DEV.', 1;

IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Permiso', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Menu', N'U') IS NULL
    THROW 56101, 'Faltan objetos base requeridos por el dashboard.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Control.BorradorNC.VerTodos')
    THROW 56108, 'Falta el permiso base Control.BorradorNC.VerTodos. Ejecute primero 02_configurar_permisos_menu_roles.sql.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE Controller=N'BorradorNc' AND Action=N'Index')
    THROW 56109, 'Falta la entrada base BorradorNc/Index. Ejecute primero 02_configurar_permisos_menu_roles.sql.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
         @Resource=N'BorradorNc.Dashboard.Bitacora.DEV', @LockMode=N'Exclusive',
         @LockOwner=N'Transaction', @LockTimeout=5000;
    IF @LockResult < 0 THROW 56102, 'No fue posible obtener el bloqueo de migración.', 1;

    IF OBJECT_ID(N'dbo.BORR_NC_BITACORA', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BORR_NC_BITACORA
        (
            EVENTO_ID       bigint IDENTITY(1,1) NOT NULL,
            ID_EMPRESA      nvarchar(15) NOT NULL,
            ID_BORRADOR     nvarchar(20) NOT NULL,
            EVENTO          varchar(30) NOT NULL,
            ESTADO_ANTERIOR varchar(20) NULL,
            ESTADO_NUEVO    varchar(20) NULL,
            USUARIO         nvarchar(50) NOT NULL,
            IP              nvarchar(45) NULL,
            DETALLE         nvarchar(1000) NULL,
            REGISTRO        datetime2(3) NOT NULL
                CONSTRAINT DF_BNB_REGISTRO DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_BORR_NC_BITACORA PRIMARY KEY CLUSTERED (EVENTO_ID),
            CONSTRAINT CK_BNB_EVENTO CHECK
                (EVENTO IN ('CREADO','AUTORIZADO','RECHAZADO','ANULADO',
                            'IMPRESO','IMPRESO_LOTE','EXPORTADO'))
        );

        CREATE INDEX IX_BORR_NC_BITACORA_BORRADOR_FECHA
            ON dbo.BORR_NC_BITACORA (ID_EMPRESA, ID_BORRADOR, REGISTRO DESC);
        CREATE INDEX IX_BORR_NC_BITACORA_EVENTO_FECHA
            ON dbo.BORR_NC_BITACORA (EVENTO, REGISTRO DESC);
    END;

    IF COL_LENGTH(N'dbo.BORR_NC_BITACORA', N'EVENTO_ID') IS NULL
       OR COL_LENGTH(N'dbo.BORR_NC_BITACORA', N'ID_EMPRESA') IS NULL
       OR COL_LENGTH(N'dbo.BORR_NC_BITACORA', N'ID_BORRADOR') IS NULL
       OR COL_LENGTH(N'dbo.BORR_NC_BITACORA', N'EVENTO') IS NULL
       OR COL_LENGTH(N'dbo.BORR_NC_BITACORA', N'USUARIO') IS NULL
       OR COL_LENGTH(N'dbo.BORR_NC_BITACORA', N'REGISTRO') IS NULL
        THROW 56103, 'BORR_NC_BITACORA existe con una estructura incompatible.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA')
                     AND name=N'IX_BORR_NC_BITACORA_BORRADOR_FECHA')
        CREATE INDEX IX_BORR_NC_BITACORA_BORRADOR_FECHA
            ON dbo.BORR_NC_BITACORA (ID_EMPRESA, ID_BORRADOR, REGISTRO DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA')
                     AND name=N'IX_BORR_NC_BITACORA_EVENTO_FECHA')
        CREATE INDEX IX_BORR_NC_BITACORA_EVENTO_FECHA
            ON dbo.BORR_NC_BITACORA (EVENTO, REGISTRO DESC);

    IF EXISTS (SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Control.BorradorNC.Dashboard'
               AND (Descripcion<>N'Consultar el dashboard de borradores de nota de crédito'
                    OR Modulo<>N'Borradores NC'))
        THROW 56104, 'El permiso Dashboard existe con datos incompatibles.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Permiso WITH (UPDLOCK,HOLDLOCK)
                   WHERE Nombre=N'Control.BorradorNC.Dashboard')
        INSERT dbo.Permiso (Nombre,Descripcion,Modulo)
        VALUES (N'Control.BorradorNC.Dashboard',
                N'Consultar el dashboard de borradores de nota de crédito',
                N'Borradores NC');

    DECLARE @MenuPadre int;
    SELECT @MenuPadre=MIN(Menu_Padre_Id)
    FROM dbo.Menu WHERE Controller=N'BorradorNc' AND Action=N'Index';

    IF EXISTS (SELECT 1 FROM dbo.Menu WHERE Controller=N'BorradorNc'
               AND Action=N'DashboardBNC'
               AND (ISNULL(Menu_Padre_Id,-1)<>@MenuPadre
                    OR ISNULL(PermisoId,N'')<>N'Control.BorradorNC.Dashboard'
                    OR ISNULL(IsActive,0)<>1))
        THROW 56107, 'La entrada DashboardBNC existente es incompatible.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE Controller=N'BorradorNc' AND Action=N'DashboardBNC')
    BEGIN
        DECLARE @MenuId int;
        DECLARE @Orden int;
        SELECT @MenuId=ISNULL(MAX(Menu_Id),0)+1 FROM dbo.Menu WITH (TABLOCKX,HOLDLOCK);
        SELECT @Orden=ISNULL(MAX(Orden),0)+1 FROM dbo.Menu WITH (HOLDLOCK) WHERE Menu_Padre_Id=@MenuPadre;
        INSERT dbo.Menu
            (Menu_Id,Menu_Padre_Id,Nombre,Titulo,Action,Controller,Orden,IconName,IsActive,PermisoId)
        VALUES
            (@MenuId,@MenuPadre,N'DashboardBNC',N'Dashboard Borradores NC',
             N'DashboardBNC',N'BorradorNc',@Orden,N'clip-stats',1,
             N'Control.BorradorNC.Dashboard');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT N'06A_ENTORNO_DEV' SECCION,
       CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) SERVIDOR,
       DB_NAME() BASE_ACTUAL, SYSDATETIME() FECHA_EJECUCION;

SELECT N'06B_OBJETO_DEV' SECCION, s.name ESQUEMA, o.name OBJETO, o.type_desc TIPO
FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE o.object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA');

SELECT N'06C_PERMISO_DEV' SECCION, Nombre, Descripcion, Modulo
FROM dbo.Permiso
WHERE Nombre=N'Control.BorradorNC.Dashboard';

SELECT N'06D_MENU_DEV' SECCION,Menu_Id,Menu_Padre_Id,Titulo,Action,Controller,Orden,PermisoId
FROM dbo.Menu WHERE Controller=N'BorradorNc' AND Action=N'DashboardBNC';

SELECT N'06E_RESUMEN_DEV' SECCION, VALIDACION, ESPERADO, REAL,
       CASE WHEN ESPERADO=REAL THEN N'OK' ELSE N'REVISAR' END RESULTADO
FROM (VALUES
    (N'Tabla de bitácora',CONVERT(bigint,1),CONVERT(bigint,CASE WHEN OBJECT_ID(N'dbo.BORR_NC_BITACORA',N'U') IS NULL THEN 0 ELSE 1 END)),
    (N'Índices de bitácora',CONVERT(bigint,2),(SELECT COUNT_BIG(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA') AND name IN (N'IX_BORR_NC_BITACORA_BORRADOR_FECHA',N'IX_BORR_NC_BITACORA_EVENTO_FECHA'))),
    (N'Permiso Dashboard',CONVERT(bigint,1),(SELECT COUNT_BIG(*) FROM dbo.Permiso WHERE Nombre=N'Control.BorradorNC.Dashboard')),
    (N'Entrada de menú',CONVERT(bigint,1),(SELECT COUNT_BIG(*) FROM dbo.Menu WHERE Controller=N'BorradorNc' AND Action=N'DashboardBNC'))
) V(VALIDACION,ESPERADO,REAL);
GO
