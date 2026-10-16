/* Ejecutar manualmente en el catálogo POS, después de revisar el destino.
   1. En esta misma conexión y con POS seleccionado, ejecutar SELECT DB_NAME() AS BaseActual.
   2. Copiar el nombre exacto de BaseActual a @BaseEsperada (por ejemplo, N'NombrePOS').
   3. Cambiar @Aplicar a 1 y ejecutar este archivo completo.
   Vista previa por defecto; la aplicación nunca ejecuta este archivo. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: crear PilotoRutaHistorial en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada,@BaseEsperada AS BaseEsperada,
           N'NO APLICADO: seleccionar POS y declarar su nombre exacto en @BaseEsperada.' AS Accion;
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
END;
IF OBJECT_ID(N'dbo.PilotoRutaHistorial',N'U') IS NOT NULL
    THROW 51000,'PilotoRutaHistorial ya existe. Revisar su definición; no sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE dbo.PilotoRutaHistorial
    (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PilotoRutaHistorial PRIMARY KEY,
        CatalogoRutas nvarchar(128) NOT NULL,
        Ruta_Id nvarchar(15) NOT NULL,
        Actor nvarchar(128) NOT NULL,
        Motivo nvarchar(250) NOT NULL,
        Antes xml NOT NULL,
        Despues xml NOT NULL,
        FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoRutaHistorial_FechaUtc DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_PilotoRutaHistorial_Ruta ON dbo.PilotoRutaHistorial(CatalogoRutas,Ruta_Id,FechaUtc DESC);
    COMMIT;
    SELECT DB_NAME() AS BaseInstalada,N'INSTALADO' AS Estado,
           OBJECT_ID(N'dbo.PilotoRutaHistorial',N'U') AS TablaId;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
