/* INSTALACIÓN MANUAL POS: borradores por cliente para rutas de piloto.
   Seleccionar POS-SmartK66_DEV en SSMS. Vista previa por defecto.
   No modifica APK66 ni APP_TEST. Ejecutar en ventana nueva sin transacciones. */
DECLARE @BaseEsperada sysname=N'POS-SmartK66_DEV';
DECLARE @Aplicar bit=0;

SELECT DB_NAME() AS BaseSeleccionada,
       OBJECT_ID(N'dbo.PilotoBorradorCliente',N'U') AS TablaBorradores,
       @Aplicar AS Aplicar;
IF @Aplicar=0 RETURN;
IF DB_NAME()<>@BaseEsperada THROW 51000,'Seleccionar POS-SmartK66_DEV antes de instalar borradores.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL
    THROW 51000,'Falta la instalación POS de usuarios o pilotos.',1;
IF OBJECT_ID(N'dbo.PilotoBorradorCliente',N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Usuario_Id') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Ruta_Id') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Primer_RowId') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Version') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'MasivoActivo') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Resultados') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Anteriores') IS NULL OR
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'FechaUtc') IS NULL
        THROW 51000,'La tabla de borradores existente no coincide con el contrato. Revisar manualmente.',1;
    SELECT N'YA INSTALADO: sin cambios.' AS Resultado;
    RETURN;
END;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.PilotoBorradorCliente (
        Usuario_Id bigint NOT NULL,
        Ruta_Id nvarchar(15) NOT NULL,
        Primer_RowId int NOT NULL,
        Version varchar(44) NOT NULL,
        MasivoActivo bit NOT NULL,
        Resultados nvarchar(max) NOT NULL,
        Anteriores nvarchar(max) NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoBorradorCliente_Fecha DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_PilotoBorradorCliente PRIMARY KEY(Usuario_Id,Ruta_Id,Primer_RowId),
        CONSTRAINT FK_PilotoBorradorCliente_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
        CONSTRAINT CK_PilotoBorradorCliente_Id CHECK(Primer_RowId>0),
        CONSTRAINT CK_PilotoBorradorCliente_Xml CHECK(
            TRY_CONVERT(xml,Resultados) IS NOT NULL AND
            (Anteriores IS NULL OR TRY_CONVERT(xml,Anteriores) IS NOT NULL) AND
            DATALENGTH(Resultados)<=250000 AND (Anteriores IS NULL OR DATALENGTH(Anteriores)<=250000)),
        CONSTRAINT CK_PilotoBorradorCliente_Masivo CHECK(
            (MasivoActivo=0 AND Anteriores IS NULL) OR (MasivoActivo=1 AND Anteriores IS NOT NULL))
    );
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT DB_NAME() AS BaseSeleccionada,N'INSTALADO: borradores de cliente en POS.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
