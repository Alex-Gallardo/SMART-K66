/* Ejecutar manualmente en POS despues de 33 en APK66 y antes de desplegar.
   Reutiliza los permisos GARITA existentes. Vista previa por defecto. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: habilitar evento I de ingreso y unicidad en POS; sin cambios.' Accion;
    SELECT name,definition FROM sys.check_constraints
    WHERE parent_object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'CK_GaritaRutaEvento_Tipo';
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catalogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NULL OR
   NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'CK_GaritaRutaEvento_Tipo')
    THROW 51000,'Falta la estructura original de eventos de garita.',1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'UX_GaritaRutaEvento_Entrada')
    THROW 51000,'El indice de entrada ya existe. Revisar su definicion; no sobrescribir.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    ALTER TABLE dbo.GaritaRutaEvento DROP CONSTRAINT CK_GaritaRutaEvento_Tipo;
    ALTER TABLE dbo.GaritaRutaEvento WITH CHECK ADD CONSTRAINT CK_GaritaRutaEvento_Tipo CHECK(Tipo IN(N'S',N'I',N'D',N'R'));
    CREATE UNIQUE INDEX UX_GaritaRutaEvento_Entrada ON dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id) WHERE Tipo=N'I';
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'ENTRADA DE GARITA DISPONIBLE' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
