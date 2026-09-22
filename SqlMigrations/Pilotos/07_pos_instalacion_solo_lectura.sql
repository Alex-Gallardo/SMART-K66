/* PILOTOS - ESTADO DE INSTALACION POS. SOLO LECTURA.
   Usar una ventana nueva de SSMS y seleccionar el catalogo POS exacto.
   No crea objetos, usuarios, roles, permisos ni vinculos.
   En dbo.Permiso la clave primaria real es Nombre; Permiso_Id pertenece a Rol_Permiso. */
DECLARE @BaseEsperada sysname=NULL; -- Completar localmente.
DECLARE @LoginPiloto nvarchar(50)=NULL; -- Opcional. No escribir ni compartir contrasenas.

IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar la base POS destino exacta.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;

SET NOCOUNT ON;
SET LOCK_TIMEOUT 3000;
BEGIN TRY
    SELECT N'BASE' AS Seccion,DB_NAME() AS BaseSeleccionada;

    SELECT N'OBJETOS_POS' AS Seccion,v.Nombre,
        CASE WHEN OBJECT_ID(N'dbo.'+v.Nombre,N'U') IS NULL THEN N'NO EXISTE' ELSE N'EXISTE' END AS Estado
    FROM (VALUES(N'PilotoVinculo'),(N'PilotoCentro'),(N'PilotoCierre'),
                (N'PilotoVehiculo'),(N'PilotoVehiculoHistorial'),
                (N'PilotoVinculoHistorial'),(N'PilotoCentroHistorial')) v(Nombre)
    ORDER BY v.Nombre;

    SELECT N'ROL_Y_PERMISOS' AS Seccion,r.Rol_Id,r.Nombre AS Rol,
        rp.Permiso_Id,p.Descripcion,p.Modulo
    FROM dbo.Rol r
    LEFT JOIN dbo.Rol_Permiso rp ON rp.Rol_Id=r.Rol_Id
        AND rp.Permiso_Id IN(N'Pilotos.Rutas.Ver',N'Pilotos.Rutas.Confirmar',N'Pilotos.Configurar')
    LEFT JOIN dbo.Permiso p ON p.Nombre=rp.Permiso_Id
    WHERE r.Nombre=N'PILOTO'
    ORDER BY rp.Permiso_Id;

    SELECT N'ADMINISTRADORES_PILOTO' AS Seccion,r.Rol_Id,r.Nombre AS Rol,
        rp.Permiso_Id,p.Descripcion,p.Modulo
    FROM dbo.Rol_Permiso rp
    JOIN dbo.Rol r ON r.Rol_Id=rp.Rol_Id
    JOIN dbo.Permiso p ON p.Nombre=rp.Permiso_Id
    WHERE rp.Permiso_Id=N'Pilotos.Configurar'
    ORDER BY r.Nombre;

    SELECT N'PERMISOS_SIN_ROL' AS Seccion,p.Nombre,p.Descripcion,p.Modulo
    FROM dbo.Permiso p
    WHERE p.Nombre IN(N'Pilotos.Rutas.Ver',N'Pilotos.Rutas.Confirmar',N'Pilotos.Configurar')
      AND NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso rp WHERE rp.Permiso_Id=p.Nombre);

    SELECT N'INDICES_POS' AS Seccion,OBJECT_NAME(i.object_id) AS Tabla,i.name AS Indice,
        i.is_unique AS EsUnico,i.has_filter AS TieneFiltro,i.filter_definition AS Filtro
    FROM sys.indexes i
    WHERE OBJECT_NAME(i.object_id) IN(N'PilotoVinculo',N'PilotoCentro',N'PilotoCierre',N'PilotoVehiculo',N'PilotoVehiculoHistorial',N'PilotoVinculoHistorial',N'PilotoCentroHistorial')
      AND i.name IS NOT NULL
    ORDER BY Tabla,Indice;

    SELECT N'USUARIO_CANDIDATO' AS Seccion,u.Usuario_Id,u.Login,u.Nombre,u.Activo,u.Autenticar_Site,
        r.Rol_Id,r.Nombre AS Rol
    FROM dbo.Usuario u
    LEFT JOIN dbo.Usuario_Rol ur ON ur.Usuario_Id=u.Usuario_Id
    LEFT JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id
    WHERE @LoginPiloto IS NOT NULL AND u.Login=@LoginPiloto
    ORDER BY u.Usuario_Id,r.Nombre;

    SET LOCK_TIMEOUT -1;
END TRY
BEGIN CATCH
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
