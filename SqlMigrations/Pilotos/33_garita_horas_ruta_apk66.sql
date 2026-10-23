/* Ejecutar manualmente en el catalogo de rutas APK66 antes de desplegar.
   Vista previa por defecto. Las fechas se guardan en hora Guatemala (UTC-6).
   MO_HR_ENTRADA de RT_RUTAS_DET corresponde a cada documento y no se modifica. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: agregar horas de garita a RT_RUTAS; sin cambios.' Accion;
    SELECT c.name Columna,t.name Tipo,c.scale,c.is_nullable
    FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
    WHERE c.object_id=OBJECT_ID(N'dbo.RT_RUTAS') AND c.name IN(N'GARITA_FECHA_SALIDA',N'GARITA_FECHA_ENTRADA');
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catalogo APK66 exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.RT_RUTAS',N'U') IS NULL
    THROW 51000,'No se encontro RT_RUTAS en el catalogo seleccionado.',1;
IF EXISTS(SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
          WHERE c.object_id=OBJECT_ID(N'dbo.RT_RUTAS') AND c.name IN(N'GARITA_FECHA_SALIDA',N'GARITA_FECHA_ENTRADA')
            AND (t.name<>N'datetime2' OR c.scale<>3 OR c.is_nullable<>1))
    THROW 51000,'Una columna de garita existe con otra definicion. Revisar sin sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.RT_RUTAS',N'GARITA_FECHA_SALIDA') IS NULL
        ALTER TABLE dbo.RT_RUTAS ADD GARITA_FECHA_SALIDA datetime2(3) NULL;
    IF COL_LENGTH(N'dbo.RT_RUTAS',N'GARITA_FECHA_ENTRADA') IS NULL
        ALTER TABLE dbo.RT_RUTAS ADD GARITA_FECHA_ENTRADA datetime2(3) NULL;
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'HORAS DE GARITA DISPONIBLES' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
