/* Ejecutar manualmente en POS, antes de publicar la aplicación.
   Vista previa por defecto: comprobar DB_NAME(), declarar el catálogo exacto
   en @BaseEsperada y cambiar @Aplicar a 1. No ejecutar en APK66. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: crear GaritaEntradaImagen en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NULL OR OBJECT_ID(N'dbo.GaritaSalidaImagen',N'U') IS NULL
    THROW 51000,'Aplicar primero las migraciones 27 y 29 de garita en POS.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'UX_GaritaRutaEvento_Entrada')
    THROW 51000,'Aplicar primero la migración 33 de entrada de garita en POS.',1;
IF OBJECT_ID(N'dbo.GaritaEntradaImagen',N'U') IS NOT NULL
    THROW 51000,'GaritaEntradaImagen ya existe. Revisar su definición; no sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.GaritaEntradaImagen
    (
        Evento_Id bigint NOT NULL CONSTRAINT PK_GaritaEntradaImagen PRIMARY KEY,
        Nombre nvarchar(255) NOT NULL,
        ContentType nvarchar(50) NOT NULL,
        Tamano int NOT NULL,
        Contenido varbinary(max) NOT NULL,
        HashSha256 binary(32) NOT NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_GaritaEntradaImagen_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_GaritaEntradaImagen_Evento FOREIGN KEY(Evento_Id) REFERENCES dbo.GaritaRutaEvento(Id),
        CONSTRAINT CK_GaritaEntradaImagen_Tamano CHECK(Tamano BETWEEN 1 AND 10485760 AND DATALENGTH(Contenido)=Tamano),
        CONSTRAINT CK_GaritaEntradaImagen_Tipo CHECK(ContentType IN(N'image/jpeg',N'image/png',N'image/webp'))
    );
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'INSTALADO' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
