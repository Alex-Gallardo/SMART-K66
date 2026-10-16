/* Ejecutar manualmente en POS después de revisar el catálogo destino.
   Vista previa por defecto; la aplicación nunca ejecuta este archivo. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: crear PilotoRutaHistorial en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
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
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
