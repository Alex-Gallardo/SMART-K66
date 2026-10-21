/* Ejecutar manualmente en POS. Vista previa por defecto.
   Comprobar SELECT DB_NAME(), declarar ese nombre exacto en @BaseEsperada
   y cambiar @Aplicar a 1. La aplicación requiere esta tabla antes del despliegue. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: crear GaritaSalidaImagen en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NULL
    THROW 51000,'Primero aplicar la migración 27 de garita.',1;
IF OBJECT_ID(N'dbo.GaritaSalidaImagen',N'U') IS NOT NULL
    THROW 51000,'GaritaSalidaImagen ya existe. Revisar su definición; no sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.GaritaSalidaImagen
    (
        Evento_Id bigint NOT NULL CONSTRAINT PK_GaritaSalidaImagen PRIMARY KEY,
        Nombre nvarchar(255) NOT NULL,
        ContentType nvarchar(50) NOT NULL,
        Tamano int NOT NULL,
        Contenido varbinary(max) NOT NULL,
        HashSha256 binary(32) NOT NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_GaritaSalidaImagen_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_GaritaSalidaImagen_Evento FOREIGN KEY(Evento_Id) REFERENCES dbo.GaritaRutaEvento(Id),
        CONSTRAINT CK_GaritaSalidaImagen_Tamano CHECK(Tamano BETWEEN 1 AND 10485760 AND DATALENGTH(Contenido)=Tamano),
        CONSTRAINT CK_GaritaSalidaImagen_Tipo CHECK(ContentType IN(N'image/jpeg',N'image/png',N'image/webp'))
    );
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'INSTALADO' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
