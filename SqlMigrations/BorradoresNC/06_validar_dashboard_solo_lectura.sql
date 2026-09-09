/* =============================================================================
   BORRADORES NC — VALIDACION DEL DASHBOARD (SOLO LECTURA)
   Bases autorizadas: POS-SmartK66 y POS-SmartK66_DEV

   No crea, modifica ni elimina objetos o datos.
   Ejecute el archivo completo en una ventana nueva de SSMS.
   ============================================================================= */
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
GO

IF DB_NAME() NOT IN (N'POS-SmartK66', N'POS-SmartK66_DEV')
    THROW 56200, 'SEGURIDAD: diagnóstico autorizado únicamente para POS-SmartK66 o POS-SmartK66_DEV.', 1;
GO

SELECT N'06V_ENTORNO' SECCION,
       CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) SERVIDOR,
       DB_NAME() BASE_ACTUAL,
       SYSDATETIME() FECHA_DIAGNOSTICO;

SELECT N'06V_PRERREQUISITOS' SECCION, V.OBJETO,
       CASE WHEN OBJECT_ID(V.OBJETO,N'U') IS NULL THEN N'FALTA' ELSE N'OK' END RESULTADO
FROM (VALUES
    (N'dbo.BORR_NC_ENC'), (N'dbo.BORR_NC_DET'), (N'dbo.BORR_NC_ADJUNTO'),
    (N'dbo.Permiso'), (N'dbo.Menu')
) V(OBJETO);

SELECT N'06V_BITACORA' SECCION, C.name COLUMNA, T.name TIPO,
       C.max_length LARGO_BYTES, C.is_nullable ES_NULLABLE
FROM sys.columns C
JOIN sys.types T ON T.user_type_id=C.user_type_id
WHERE C.object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA')
ORDER BY C.column_id;

SELECT N'06V_INDICES' SECCION, I.name INDICE, I.type_desc TIPO, I.is_unique ES_UNICO
FROM sys.indexes I
WHERE I.object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA')
ORDER BY I.index_id;

IF OBJECT_ID(N'dbo.Permiso',N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Menu',N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''06V_PERMISOS'' SECCION, Nombre, Descripcion, Modulo
        FROM dbo.Permiso
        WHERE Nombre IN (N''Control.BorradorNC.Dashboard'',N''Control.BorradorNC.VerTodos'')
        ORDER BY Nombre;

        SELECT N''06V_MENU'' SECCION, Menu_Id,Menu_Padre_Id,Titulo,Action,Controller,
               Orden,IsActive,PermisoId
        FROM dbo.Menu
        WHERE Controller=N''BorradorNc'' AND Action IN (N''Index'',N''DashboardBNC'')
        ORDER BY Action;';
END;

DECLARE @Tabla bit=CASE WHEN OBJECT_ID(N'dbo.BORR_NC_BITACORA',N'U') IS NULL THEN 0 ELSE 1 END;
DECLARE @Columnas int=(SELECT COUNT(*) FROM sys.columns
                       WHERE object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA')
                         AND name IN (N'EVENTO_ID',N'ID_EMPRESA',N'ID_BORRADOR',N'EVENTO',
                                      N'ESTADO_ANTERIOR',N'ESTADO_NUEVO',N'USUARIO',N'IP',
                                      N'DETALLE',N'REGISTRO'));
DECLARE @Indices int=(SELECT COUNT(*) FROM sys.indexes
                      WHERE object_id=OBJECT_ID(N'dbo.BORR_NC_BITACORA')
                        AND name IN (N'IX_BORR_NC_BITACORA_BORRADOR_FECHA',
                                     N'IX_BORR_NC_BITACORA_EVENTO_FECHA'));
DECLARE @PermisoDashboard int=0, @PermisoGlobal int=0, @MenuDashboard int=0;

IF OBJECT_ID(N'dbo.Permiso',N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql
        N'SELECT @Dashboard=COUNT(*) FROM dbo.Permiso WHERE Nombre=N''Control.BorradorNC.Dashboard'';
          SELECT @Global=COUNT(*) FROM dbo.Permiso WHERE Nombre=N''Control.BorradorNC.VerTodos'';',
        N'@Dashboard int OUTPUT,@Global int OUTPUT',
        @Dashboard=@PermisoDashboard OUTPUT,@Global=@PermisoGlobal OUTPUT;
END;

IF OBJECT_ID(N'dbo.Menu',N'U') IS NOT NULL
    EXEC sys.sp_executesql
        N'SELECT @Menu=COUNT(*) FROM dbo.Menu
          WHERE Controller=N''BorradorNc'' AND Action=N''DashboardBNC''
            AND IsActive=1 AND PermisoId=N''Control.BorradorNC.Dashboard'';',
        N'@Menu int OUTPUT', @Menu=@MenuDashboard OUTPUT;

SELECT N'06V_RESUMEN' SECCION, VALIDACION, ESPERADO, REAL,
       CASE WHEN ESPERADO=REAL THEN N'OK' ELSE N'REVISAR' END RESULTADO
FROM (VALUES
    (N'Tabla de bitácora',1,CONVERT(int,@Tabla)),
    (N'Columnas de bitácora',10,@Columnas),
    (N'Índices de consulta',2,@Indices),
    (N'Permiso Dashboard',1,@PermisoDashboard),
    (N'Permiso de alcance global',1,@PermisoGlobal),
    (N'Entrada de menú activa',1,@MenuDashboard)
) V(VALIDACION,ESPERADO,REAL);
GO
