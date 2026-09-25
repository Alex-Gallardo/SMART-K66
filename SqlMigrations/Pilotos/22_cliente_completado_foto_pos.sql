/* MANUAL POS-SmartK66_DEV: bloqueo definitivo del cliente y una foto final vigente.
   Vista previa por defecto. No modifica APK66 ni APP_TEST. Ejecutar en ventana nueva. */
DECLARE @BaseEsperada sysname=N'POS-SmartK66_DEV';
DECLARE @Aplicar bit=0;

SELECT DB_NAME() AS BaseSeleccionada,@Aplicar AS Aplicar,
       COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Completado') AS ColumnaCompletado,
       OBJECT_ID(N'dbo.PilotoClienteImagen',N'U') AS TablaFotoCliente,
       OBJECT_ID(N'dbo.PilotoClienteImagenEvento',N'U') AS TablaAuditoria;
IF @Aplicar=0 RETURN;
IF DB_NAME()<>@BaseEsperada THROW 51000,'Seleccionar POS-SmartK66_DEV antes de instalar.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.PilotoBorradorCliente',N'U') IS NULL OR OBJECT_ID(N'dbo.Usuario',N'U') IS NULL
    THROW 51000,'Falta instalar los borradores de clientes o usuarios POS.',1;
IF OBJECT_ID(N'dbo.PilotoClienteImagen',N'U') IS NOT NULL AND
    (COL_LENGTH(N'dbo.PilotoClienteImagen',N'Ruta_Id') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'Primer_RowId') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'Usuario_Id') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'Nombre') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'ContentType') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'Tamano') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'Contenido') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'HashSha256') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagen',N'FechaUtc') IS NULL)
    THROW 51000,'Tabla de foto final existente incompatible. Revisar manualmente.',1;
IF OBJECT_ID(N'dbo.PilotoClienteImagenEvento',N'U') IS NOT NULL AND
    (COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'Ruta_Id') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'Primer_RowId') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'Usuario_Id') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'Accion') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'HashAnterior') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'HashNueva') IS NULL OR
     COL_LENGTH(N'dbo.PilotoClienteImagenEvento',N'FechaUtc') IS NULL)
    THROW 51000,'Tabla de auditoría existente incompatible. Revisar manualmente.',1;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.PilotoBorradorCliente',N'Completado') IS NULL
        ALTER TABLE dbo.PilotoBorradorCliente ADD Completado bit NOT NULL
            CONSTRAINT DF_PilotoBorradorCliente_Completado DEFAULT(0) WITH VALUES;
    IF OBJECT_ID(N'dbo.PilotoClienteImagen',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PilotoClienteImagen (
            Ruta_Id nvarchar(15) NOT NULL,
            Primer_RowId int NOT NULL,
            Usuario_Id bigint NOT NULL,
            Nombre nvarchar(255) NOT NULL,
            ContentType nvarchar(50) NOT NULL,
            Tamano int NOT NULL,
            Contenido varbinary(max) NOT NULL,
            HashSha256 binary(32) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoClienteImagen_Fecha DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT PK_PilotoClienteImagen PRIMARY KEY(Ruta_Id,Primer_RowId),
            CONSTRAINT FK_PilotoClienteImagen_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
            CONSTRAINT CK_PilotoClienteImagen_RowId CHECK(Primer_RowId>0),
            CONSTRAINT CK_PilotoClienteImagen_Tamano CHECK(Tamano BETWEEN 1 AND 10485760 AND DATALENGTH(Contenido)=Tamano),
            CONSTRAINT CK_PilotoClienteImagen_Tipo CHECK(ContentType IN(N'image/jpeg',N'image/png',N'image/webp'))
        );
    END;
    IF OBJECT_ID(N'dbo.PilotoClienteImagenEvento',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PilotoClienteImagenEvento (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PilotoClienteImagenEvento PRIMARY KEY,
            Ruta_Id nvarchar(15) NOT NULL,
            Primer_RowId int NOT NULL,
            Usuario_Id bigint NOT NULL,
            Accion nvarchar(12) NOT NULL,
            HashAnterior binary(32) NULL,
            HashNueva binary(32) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoClienteImagenEvento_Fecha DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT FK_PilotoClienteImagenEvento_Imagen FOREIGN KEY(Ruta_Id,Primer_RowId)
                REFERENCES dbo.PilotoClienteImagen(Ruta_Id,Primer_RowId),
            CONSTRAINT FK_PilotoClienteImagenEvento_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
            CONSTRAINT CK_PilotoClienteImagenEvento_Accion CHECK(Accion IN(N'AGREGADA',N'REEMPLAZADA'))
        );
        CREATE INDEX IX_PilotoClienteImagenEvento_ClienteFecha
            ON dbo.PilotoClienteImagenEvento(Ruta_Id,Primer_RowId,FechaUtc DESC);
    END;
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT N'INSTALADO: bloqueo y foto final de cliente en POS.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
