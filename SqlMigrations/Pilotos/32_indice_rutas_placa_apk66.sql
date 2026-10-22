/* Opcional: acelerar la consulta de rutas activas por placa en APK66.
   Revisar tamano, plan y ventana de mantenimiento: crear el indice puede bloquear.
   Vista previa por defecto; declarar el catalogo de rutas exacto antes de aplicar. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: indice RT_RUTAS por placa y estado; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catalogo APK66 exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.RT_RUTAS',N'U') IS NULL
    THROW 51000,'No se encontro RT_RUTAS en el catalogo seleccionado.',1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RT_RUTAS') AND name=N'IX_PortalPiloto_RutasPlaca')
    THROW 51000,'El indice ya existe. Revisar su definicion; no sobrescribir.',1;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    CREATE INDEX IX_PortalPiloto_RutasPlaca ON dbo.RT_RUTAS(PLACA,STATUS,FECHA_RUTA DESC,ID_RUTA DESC)
        INCLUDE(PILOTO,CENTRO_DIST,LIQUIDADO);
    SELECT DB_NAME() BaseInstalada,N'INDICE INSTALADO' Estado;
END TRY
BEGIN CATCH
    THROW;
END CATCH;
