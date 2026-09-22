/* MANUAL: habilita la interfaz de administracion de pilotos en POS.
   Seleccionar la base POS exacta. No modifica APK66, usuarios ni rutas.
   Vista previa por defecto. Para aplicar: completar base, rol y @Aplicar=1. */
DECLARE @BaseEsperada sysname=NULL;
DECLARE @RolAdministrador nvarchar(100)=NULL; -- Rol existente de Distribucion/administracion.
DECLARE @Aplicar bit=0;

IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,
           N'VISTA PREVIA: crea auditoria de vinculos/centros y permiso Pilotos.Configurar; lo asigna al rol indicado.' AS Accion;
    SELECT Rol_Id,Nombre FROM dbo.Rol ORDER BY Nombre;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar la base POS destino exacta.',1;
IF NULLIF(LTRIM(RTRIM(@RolAdministrador)),N'') IS NULL
    THROW 51000,'Indicar el rol existente que administrara los vinculos.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NULL
   OR OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVehiculoHistorial',N'U') IS NULL
    THROW 51000,'Ejecutar primero las instalaciones 10 y 14.',1;
IF (SELECT COUNT(*) FROM dbo.Rol WHERE Nombre=@RolAdministrador)<>1
    THROW 51000,'El rol administrador no existe o esta duplicado.',1;
IF OBJECT_ID(N'dbo.PilotoVinculoHistorial',N'U') IS NOT NULL AND
   (COL_LENGTH(N'dbo.PilotoVinculoHistorial',N'EmpleadoAnterior') IS NULL OR COL_LENGTH(N'dbo.PilotoVinculoHistorial',N'ActivoNuevo') IS NULL)
    THROW 51000,'PilotoVinculoHistorial existe con una estructura diferente; revisar manualmente.',1;
IF OBJECT_ID(N'dbo.PilotoCentroHistorial',N'U') IS NOT NULL AND
   (COL_LENGTH(N'dbo.PilotoCentroHistorial',N'Centro_Dist') IS NULL OR COL_LENGTH(N'dbo.PilotoCentroHistorial',N'ActivoNuevo') IS NULL)
    THROW 51000,'PilotoCentroHistorial existe con una estructura diferente; revisar manualmente.',1;

SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.PilotoVinculoHistorial',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PilotoVinculoHistorial (
            Id bigint IDENTITY NOT NULL CONSTRAINT PK_PilotoVinculoHistorial PRIMARY KEY,
            Usuario_Id bigint NOT NULL,
            EmpleadoAnterior int NULL,
            EmpleadoNuevo int NOT NULL,
            OperadorAnterior nvarchar(15) NULL,
            OperadorNuevo nvarchar(15) NOT NULL,
            ActivoAnterior bit NULL,
            ActivoNuevo bit NOT NULL,
            Motivo nvarchar(250) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoVinculoHistorial_Fecha DEFAULT(SYSUTCDATETIME()),
            Actor sysname NOT NULL,
            CONSTRAINT FK_PilotoVinculoHistorial_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id)
        );
        CREATE INDEX IX_PilotoVinculoHistorial_UsuarioFecha ON dbo.PilotoVinculoHistorial(Usuario_Id,FechaUtc DESC);
    END;

    IF OBJECT_ID(N'dbo.PilotoCentroHistorial',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PilotoCentroHistorial (
            Id bigint IDENTITY NOT NULL CONSTRAINT PK_PilotoCentroHistorial PRIMARY KEY,
            Usuario_Id bigint NOT NULL,
            Centro_Dist nvarchar(15) NOT NULL,
            ActivoNuevo bit NOT NULL,
            Motivo nvarchar(250) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoCentroHistorial_Fecha DEFAULT(SYSUTCDATETIME()),
            Actor sysname NOT NULL,
            CONSTRAINT FK_PilotoCentroHistorial_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id)
        );
        CREATE INDEX IX_PilotoCentroHistorial_UsuarioFecha ON dbo.PilotoCentroHistorial(Usuario_Id,FechaUtc DESC);
    END;

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PilotoVinculoHistorial') AND name=N'IX_PilotoVinculoHistorial_UsuarioFecha')
        CREATE INDEX IX_PilotoVinculoHistorial_UsuarioFecha ON dbo.PilotoVinculoHistorial(Usuario_Id,FechaUtc DESC);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PilotoCentroHistorial') AND name=N'IX_PilotoCentroHistorial_UsuarioFecha')
        CREATE INDEX IX_PilotoCentroHistorial_UsuarioFecha ON dbo.PilotoCentroHistorial(Usuario_Id,FechaUtc DESC);

    IF NOT EXISTS(SELECT 1 FROM dbo.Permiso WITH(UPDLOCK,HOLDLOCK) WHERE Nombre=N'Pilotos.Configurar')
        INSERT dbo.Permiso(Nombre,Descripcion,Modulo)
        VALUES(N'Pilotos.Configurar',N'Administrar vinculos, centros y vehiculos de pilotos',N'Pilotos');

    DECLARE @RolId int=(SELECT Rol_Id FROM dbo.Rol WHERE Nombre=@RolAdministrador);
    IF NOT EXISTS(SELECT 1 FROM dbo.Rol_Permiso WITH(UPDLOCK,HOLDLOCK)
                  WHERE Rol_Id=@RolId AND Permiso_Id=N'Pilotos.Configurar')
        INSERT dbo.Rol_Permiso(Rol_Id,Permiso_Id) VALUES(@RolId,N'Pilotos.Configurar');

    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT N'Administracion de pilotos instalada.' AS Resultado,@RolAdministrador AS RolAutorizado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
