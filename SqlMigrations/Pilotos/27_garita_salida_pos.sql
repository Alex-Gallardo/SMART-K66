/* Ejecutar manualmente en POS. Vista previa por defecto.
   1. Ejecutar SELECT DB_NAME() AS BaseActual en la conexión POS.
   2. Copiar el nombre EXACTO a @BaseEsperada y cambiar @Aplicar a 1.
   3. Ejecutar el archivo completo. Crea roles GARITA/GARITA_ADMIN, sin asignar usuarios. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: roles, permisos, centros y eventos de garita en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL OR OBJECT_ID(N'dbo.Rol',N'U') IS NULL OR OBJECT_ID(N'dbo.Rol_Permiso',N'U') IS NULL OR OBJECT_ID(N'dbo.Permiso',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NULL
    THROW 51000,'Falta la estructura POS requerida.',1;
IF OBJECT_ID(N'dbo.GaritaCentro',N'U') IS NOT NULL OR OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NOT NULL
    THROW 51000,'Las tablas de garita ya existen. Revisar su definición; no sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.GaritaCentro
    (
        Usuario_Id bigint NOT NULL,
        Centro_Dist nvarchar(15) NOT NULL,
        CONSTRAINT PK_GaritaCentro PRIMARY KEY(Usuario_Id,Centro_Dist),
        CONSTRAINT FK_GaritaCentro_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
        CONSTRAINT CK_GaritaCentro_NoVacio CHECK(LEN(LTRIM(RTRIM(Centro_Dist)))>0)
    );
    CREATE TABLE dbo.GaritaRutaEvento
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_GaritaRutaEvento PRIMARY KEY,
        CatalogoRutas nvarchar(128) NOT NULL,
        Ruta_Id nvarchar(15) NOT NULL,
        Tipo nchar(1) NOT NULL CONSTRAINT CK_GaritaRutaEvento_Tipo CHECK(Tipo IN(N'S',N'D',N'R')),
        Solicitud uniqueidentifier NOT NULL CONSTRAINT UQ_GaritaRutaEvento_Solicitud UNIQUE,
        Usuario_Id bigint NOT NULL,
        Actor nvarchar(50) NOT NULL,
        Centro_Dist nvarchar(15) NOT NULL,
        Piloto nvarchar(90) NOT NULL,
        Placa nvarchar(15) NOT NULL,
        Documentos int NOT NULL,
        Bultos decimal(18,3) NOT NULL,
        Version nvarchar(44) NOT NULL,
        Precinto nvarchar(40) NULL,
        Observacion nvarchar(250) NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_GaritaRutaEvento_FechaUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_GaritaRutaEvento_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id)
    );
    CREATE UNIQUE INDEX UX_GaritaRutaEvento_Salida ON dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id) WHERE Tipo=N'S';
    CREATE INDEX IX_GaritaRutaEvento_Ruta ON dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id,Id DESC);
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Garita.Rutas.Ver')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo) VALUES(N'Garita.Rutas.Ver',N'Consultar rutas del centro autorizado en garita',N'Pilotos');
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Garita.Rutas.ConfirmarSalida')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo) VALUES(N'Garita.Rutas.ConfirmarSalida',N'Confirmar salida o reportar diferencia en garita',N'Pilotos');
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WHERE Nombre=N'Garita.Administrar')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo) VALUES(N'Garita.Administrar',N'Administrar centros e incidencias de garita',N'Pilotos');
    IF (SELECT COUNT(*) FROM dbo.Rol WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'GARITA')>1 OR
       (SELECT COUNT(*) FROM dbo.Rol WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'GARITA_ADMIN')>1
        THROW 51000,'Hay roles de garita duplicados; resolver antes de continuar.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol WHERE Nombre=N'GARITA') INSERT dbo.Rol(Nombre) VALUES(N'GARITA');
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol WHERE Nombre=N'GARITA_ADMIN') INSERT dbo.Rol(Nombre) VALUES(N'GARITA_ADMIN');
    DECLARE @RolGarita int=(SELECT Rol_Id FROM dbo.Rol WHERE Nombre=N'GARITA');
    DECLARE @RolAdmin int=(SELECT Rol_Id FROM dbo.Rol WHERE Nombre=N'GARITA_ADMIN');
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WHERE Rol_Id=@RolGarita AND Permiso_Id=N'Garita.Rutas.Ver')
        INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolGarita,N'Garita.Rutas.Ver');
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WHERE Rol_Id=@RolGarita AND Permiso_Id=N'Garita.Rutas.ConfirmarSalida')
        INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolGarita,N'Garita.Rutas.ConfirmarSalida');
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WHERE Rol_Id=@RolAdmin AND Permiso_Id=N'Garita.Administrar')
        INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolAdmin,N'Garita.Administrar');
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'INSTALADO' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
