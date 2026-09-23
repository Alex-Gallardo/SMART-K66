/* MANUAL: una imagen vigente por documento de ruta, guardada en POS.
   Seleccionar POS-SmartK66_DEV y verificar el nombre antes de @Aplicar=1.
   No toca APK66/APP_TEST, rutas, roles ni permisos. Vista previa por defecto. */
DECLARE @BaseEsperada sysname=NULL;
DECLARE @Aplicar bit=0;

IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,
           OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') AS TablaImagen,
           OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') AS TablaAuditoria,
           N'VISTA PREVIA: no se crearon tablas ni se modificaron datos.' AS Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar la base POS destino exacta.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL
    THROW 51000,'Falta la instalacion de usuarios o pilotos POS.',1;
IF (OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NULL AND OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') IS NOT NULL)
   OR (OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') IS NULL)
    THROW 51000,'Instalacion parcial de imagenes; revisar manualmente.',1;
IF OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NOT NULL AND
   (COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'Ruta_Id') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'Detalle_RowId') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'Usuario_Id') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'Nombre') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'ContentType') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'Tamano') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'Contenido') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'HashSha256') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagen',N'FechaUtc') IS NULL)
    THROW 51000,'La tabla de imagenes existente no coincide con el contrato.',1;
IF OBJECT_ID(N'dbo.PilotoDocumentoImagenEvento',N'U') IS NOT NULL AND
   (COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'Ruta_Id') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'Detalle_RowId') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'Usuario_Id') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'Accion') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'HashAnterior') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'HashNueva') IS NULL OR
    COL_LENGTH(N'dbo.PilotoDocumentoImagenEvento',N'FechaUtc') IS NULL)
    THROW 51000,'La tabla de auditoria de imagenes existente no coincide con el contrato.',1;

SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.PilotoDocumentoImagen',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PilotoDocumentoImagen (
            Ruta_Id nvarchar(15) NOT NULL,
            Detalle_RowId int NOT NULL,
            Usuario_Id bigint NOT NULL,
            Nombre nvarchar(255) NOT NULL,
            ContentType nvarchar(50) NOT NULL,
            Tamano int NOT NULL,
            Contenido varbinary(max) NOT NULL,
            HashSha256 binary(32) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoDocumentoImagen_Fecha DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT PK_PilotoDocumentoImagen PRIMARY KEY(Ruta_Id,Detalle_RowId),
            CONSTRAINT FK_PilotoDocumentoImagen_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
            CONSTRAINT CK_PilotoDocumentoImagen_Tamano CHECK(Tamano BETWEEN 1 AND 10485760 AND DATALENGTH(Contenido)=Tamano),
            CONSTRAINT CK_PilotoDocumentoImagen_Tipo CHECK(ContentType IN(N'image/jpeg',N'image/png',N'image/webp'))
        );
        CREATE TABLE dbo.PilotoDocumentoImagenEvento (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PilotoDocumentoImagenEvento PRIMARY KEY,
            Ruta_Id nvarchar(15) NOT NULL,
            Detalle_RowId int NOT NULL,
            Usuario_Id bigint NOT NULL,
            Accion nvarchar(12) NOT NULL,
            HashAnterior binary(32) NULL,
            HashNueva binary(32) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoDocumentoImagenEvento_Fecha DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT FK_PilotoDocumentoImagenEvento_Imagen FOREIGN KEY(Ruta_Id,Detalle_RowId)
                REFERENCES dbo.PilotoDocumentoImagen(Ruta_Id,Detalle_RowId),
            CONSTRAINT FK_PilotoDocumentoImagenEvento_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
            CONSTRAINT CK_PilotoDocumentoImagenEvento_Accion CHECK(Accion IN(N'AGREGADA',N'REEMPLAZADA'))
        );
        CREATE INDEX IX_PilotoDocumentoImagenEvento_DocumentoFecha
            ON dbo.PilotoDocumentoImagenEvento(Ruta_Id,Detalle_RowId,FechaUtc DESC);
    END;
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT DB_NAME() AS BaseSeleccionada,
           N'INSTALADO: una imagen vigente por documento; eventos de reemplazo auditados.' AS Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
