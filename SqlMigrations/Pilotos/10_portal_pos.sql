/* Instalacion MANUAL. Revisar y probar en una copia aislada antes de desplegar.
   Seleccionar explicitamente el catalogo POS. No se ejecuta desde el appweb.
   La salida inicial es de solo lectura. Para aplicar: base exacta + @Aplicar=1.
   Usar una ventana nueva de SSMS y cerrarla si se cancela el lote. */
DECLARE @BaseEsperada sysname = NULL; -- Completar localmente; no versionar nombres de entorno.
DECLARE @Aplicar bit = 0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada, N'VISTA PREVIA: crea vinculos, centros, auditoria y permisos; no crea usuarios ni asigna roles.' AS Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000, 'Seleccionar y declarar la base POS destino exacta.', 1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000, 'Usar una ventana nueva sin transacciones ni opciones de sesion modificadas.', 1;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL OR OBJECT_ID(N'dbo.Rol',N'U') IS NULL
    THROW 51000, 'Falta el esquema de seguridad esperado.', 1;
IF OBJECT_ID(N'dbo.PilotoVinculo') IS NOT NULL OR OBJECT_ID(N'dbo.PilotoCentro') IS NOT NULL OR OBJECT_ID(N'dbo.PilotoCierre') IS NOT NULL
    THROW 51000, 'Ya existen objetos del portal. Revisar la instalacion; no sobrescribir.', 1;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.PilotoVinculo (
        Usuario_Id bigint NOT NULL CONSTRAINT PK_PilotoVinculo PRIMARY KEY,
        Empleado_RowId int NOT NULL,
        Codigo_Operador nvarchar(15) NOT NULL,
        Activo bit NOT NULL CONSTRAINT DF_PilotoVinculo_Activo DEFAULT(0),
        ModificadoUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoVinculo_Fecha DEFAULT(SYSUTCDATETIME()),
        ModificadoPor sysname NOT NULL CONSTRAINT DF_PilotoVinculo_Actor DEFAULT(ORIGINAL_LOGIN()),
        CONSTRAINT FK_PilotoVinculo_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
        CONSTRAINT CK_PilotoVinculo_Operador CHECK(LEN(LTRIM(RTRIM(Codigo_Operador)))>0)
    );
    CREATE UNIQUE INDEX UX_PilotoVinculo_EmpleadoActivo ON dbo.PilotoVinculo(Empleado_RowId) WHERE Activo=1;
    CREATE UNIQUE INDEX UX_PilotoVinculo_OperadorActivo ON dbo.PilotoVinculo(Codigo_Operador) WHERE Activo=1;
    CREATE TABLE dbo.PilotoCentro (
        Usuario_Id bigint NOT NULL,
        Centro_Dist nvarchar(15) NOT NULL,
        CONSTRAINT PK_PilotoCentro PRIMARY KEY(Usuario_Id,Centro_Dist),
        CONSTRAINT FK_PilotoCentro_Vinculo FOREIGN KEY(Usuario_Id) REFERENCES dbo.PilotoVinculo(Usuario_Id),
        CONSTRAINT CK_PilotoCentro_NoVacio CHECK(LEN(LTRIM(RTRIM(Centro_Dist)))>0)
    );
    CREATE TABLE dbo.PilotoCierre (
        Usuario_Id bigint NOT NULL,
        Solicitud uniqueidentifier NOT NULL,
        Ruta_Id nvarchar(15) NOT NULL,
        Login nvarchar(50) NOT NULL,
        Operador nvarchar(15) NOT NULL,
        Huella varchar(44) NOT NULL,
        Antes xml NOT NULL,
        Despues xml NOT NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoCierre_Fecha DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_PilotoCierre PRIMARY KEY(Usuario_Id,Solicitud),
        CONSTRAINT FK_PilotoCierre_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id)
    );
    CREATE INDEX IX_PilotoCierre_Ruta ON dbo.PilotoCierre(Ruta_Id,FechaUtc);
    IF (SELECT COUNT(*) FROM dbo.Rol WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'PILOTO')>1
        THROW 51000, 'Hay roles PILOTO duplicados; resolver antes de continuar.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol WHERE Nombre=N'PILOTO')
        INSERT dbo.Rol(Nombre) VALUES(N'PILOTO');
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'Pilotos.Rutas.Ver')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo) VALUES(N'Pilotos.Rutas.Ver',N'Consultar las rutas propias',N'Pilotos');
    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'Pilotos.Rutas.Confirmar')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo) VALUES(N'Pilotos.Rutas.Confirmar',N'Completar rutas propias y registrar resultados',N'Pilotos');
    DECLARE @Rol int=(SELECT Rol_Id FROM dbo.Rol WHERE Nombre=N'PILOTO');
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Rol_Id=@Rol AND Permiso_Id=N'Pilotos.Rutas.Ver')
        INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@Rol,N'Pilotos.Rutas.Ver');
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Rol_Id=@Rol AND Permiso_Id=N'Pilotos.Rutas.Confirmar')
        INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@Rol,N'Pilotos.Rutas.Confirmar');
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT N'Instalacion completada. Sin usuarios vinculados; modulo desactivado por configuracion.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
