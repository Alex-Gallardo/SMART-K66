/* MANUAL: ampliar la vinculacion POS sin duplicar dbo.Usuario.
   Seleccionar la base POS correcta y revisar antes de @Aplicar=1.
   No cambia APK66, cuentas, roles, rutas ni placas. No se ejecuta desde la web. */
DECLARE @BaseEsperada sysname=NULL, @Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,N'VISTA PREVIA: crear PilotoVehiculo y su historial, sin asignaciones.' AS Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar la base POS destino exacta.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL
    THROW 51000,'Revisar primero la instalacion del vinculo usuario-piloto (script 10).',1;
IF OBJECT_ID(N'dbo.PilotoVehiculo') IS NOT NULL OR OBJECT_ID(N'dbo.PilotoVehiculoHistorial') IS NOT NULL
    THROW 51000,'La instalacion ya existe o esta incompleta. Revisar; no sobrescribir.',1;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.PilotoVehiculo (
        Usuario_Id bigint NOT NULL,
        Placa nvarchar(15) NOT NULL,
        Activo bit NOT NULL CONSTRAINT DF_PilotoVehiculo_Activo DEFAULT(0),
        ModificadoUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoVehiculo_Fecha DEFAULT(SYSUTCDATETIME()),
        ModificadoPor sysname NOT NULL CONSTRAINT DF_PilotoVehiculo_Actor DEFAULT(ORIGINAL_LOGIN()),
        CONSTRAINT PK_PilotoVehiculo PRIMARY KEY(Usuario_Id,Placa),
        CONSTRAINT FK_PilotoVehiculo_Vinculo FOREIGN KEY(Usuario_Id) REFERENCES dbo.PilotoVinculo(Usuario_Id),
        CONSTRAINT CK_PilotoVehiculo_Placa CHECK(LEN(LTRIM(RTRIM(Placa)))>0)
    );
    CREATE TABLE dbo.PilotoVehiculoHistorial (
        Id bigint IDENTITY NOT NULL CONSTRAINT PK_PilotoVehiculoHistorial PRIMARY KEY,
        Usuario_Id bigint NOT NULL,
        Placa nvarchar(15) NOT NULL,
        ActivoAnterior bit NULL,
        ActivoNuevo bit NOT NULL,
        Motivo nvarchar(250) NOT NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoVehiculoHistorial_Fecha DEFAULT(SYSUTCDATETIME()),
        Actor sysname NOT NULL CONSTRAINT DF_PilotoVehiculoHistorial_Actor DEFAULT(ORIGINAL_LOGIN()),
        CONSTRAINT FK_PilotoVehiculoHistorial_Vehiculo FOREIGN KEY(Usuario_Id,Placa) REFERENCES dbo.PilotoVehiculo(Usuario_Id,Placa)
    );
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT N'Vinculos de vehiculo instalados; ninguna placa autorizada.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
