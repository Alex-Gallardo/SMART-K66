/* Ejecutar manualmente en POS, después de revisar el catálogo. Vista previa por defecto.
   Declarar el nombre POS exacto en @BaseEsperada y cambiar @Aplicar a 1.
   No crea usuarios POS ni modifica datos de APK66. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: crear PilotoEmpresa, PilotoConductorRuta e historial en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL
    OR OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NULL
    THROW 51000,'Primero instalar los vínculos de pilotos en POS.',1;
IF OBJECT_ID(N'dbo.PilotoEmpresa',N'U') IS NOT NULL OR OBJECT_ID(N'dbo.PilotoConductorRuta',N'U') IS NOT NULL
    OR OBJECT_ID(N'dbo.PilotoConductorHistorial',N'U') IS NOT NULL
    THROW 51000,'La migración 30 ya está presente o incompleta. Revisar definiciones; no sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.PilotoEmpresa
    (
        Usuario_Id bigint NOT NULL CONSTRAINT PK_PilotoEmpresa PRIMARY KEY,
        Nit nvarchar(50) NOT NULL,
        Nombre nvarchar(120) NOT NULL,
        Activo bit NOT NULL,
        ModificadoUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoEmpresa_Modificado DEFAULT SYSUTCDATETIME(),
        ModificadoPor nvarchar(128) NOT NULL,
        CONSTRAINT FK_PilotoEmpresa_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
        CONSTRAINT UQ_PilotoEmpresa_Nit UNIQUE(Nit),
        CONSTRAINT CK_PilotoEmpresa_Nombre CHECK(LEN(LTRIM(RTRIM(Nombre)))>0)
    );
    CREATE TABLE dbo.PilotoConductorRuta
    (
        CatalogoRutas nvarchar(128) NOT NULL,
        Ruta_Id nvarchar(15) NOT NULL,
        Usuario_Id bigint NOT NULL,
        Placa nvarchar(15) NOT NULL,
        Piloto nvarchar(90) NOT NULL,
        Nombre nvarchar(120) NOT NULL,
        Documento nvarchar(30) NOT NULL,
        Version uniqueidentifier NOT NULL CONSTRAINT DF_PilotoConductorRuta_Version DEFAULT NEWID(),
        ModificadoUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoConductorRuta_Modificado DEFAULT SYSUTCDATETIME(),
        ModificadoPor nvarchar(128) NOT NULL,
        CONSTRAINT PK_PilotoConductorRuta PRIMARY KEY(CatalogoRutas,Ruta_Id),
        CONSTRAINT FK_PilotoConductorRuta_Empresa FOREIGN KEY(Usuario_Id) REFERENCES dbo.PilotoEmpresa(Usuario_Id),
        CONSTRAINT CK_PilotoConductorRuta_Nombre CHECK(LEN(LTRIM(RTRIM(Nombre)))>=5),
        CONSTRAINT CK_PilotoConductorRuta_Documento CHECK(LEN(LTRIM(RTRIM(Documento)))>=4)
    );
    CREATE TABLE dbo.PilotoConductorHistorial
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PilotoConductorHistorial PRIMARY KEY,
        CatalogoRutas nvarchar(128) NOT NULL,
        Ruta_Id nvarchar(15) NOT NULL,
        Usuario_Id bigint NOT NULL,
        Placa nvarchar(15) NOT NULL,
        NombreAnterior nvarchar(120) NULL,
        DocumentoAnterior nvarchar(30) NULL,
        NombreNuevo nvarchar(120) NULL,
        DocumentoNuevo nvarchar(30) NULL,
        Actor nvarchar(128) NOT NULL,
        Motivo nvarchar(250) NOT NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoConductorHistorial_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PilotoConductorHistorial_Empresa FOREIGN KEY(Usuario_Id) REFERENCES dbo.PilotoEmpresa(Usuario_Id)
    );
    CREATE INDEX IX_PilotoConductorHistorial_Ruta ON dbo.PilotoConductorHistorial(CatalogoRutas,Ruta_Id,Id DESC);
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'INSTALADO' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
