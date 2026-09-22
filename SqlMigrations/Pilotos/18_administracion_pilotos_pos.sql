/* MANUAL: habilita la interfaz de administracion de pilotos en POS.
   Seleccionar la base POS exacta. No modifica APK66, usuarios ni rutas.
   Vista previa por defecto. Para aplicar: completar base y @Aplicar=1. */
DECLARE @BaseEsperada sysname=NULL;
DECLARE @Aplicar bit=0;

IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,
           N'VISTA PREVIA: crea auditoria de vinculos y centros. No crea permisos ni cambia roles.' AS Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar la base POS destino exacta.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoCentro',N'U') IS NULL
   OR OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVehiculoHistorial',N'U') IS NULL
    THROW 51000,'Ejecutar primero las instalaciones 10 y 14.',1;
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

    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT N'Administracion de pilotos instalada. Acceso disponible para usuarios POS autenticados.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
