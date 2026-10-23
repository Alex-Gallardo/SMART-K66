/* Ejecutar manualmente en POS, nunca en APK66. Vista previa por defecto.
   1. SELECT DB_NAME() AS BaseActual;
   2. Copiar el nombre exacto a @BaseEsperada y cambiar @Aplicar a 1.
   APK66 ya contiene RT_RUTAS.HR_SALIDA y HR_ENTRADA time(0) NULL. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,
           N'VISTA PREVIA: permitir evento I de entrada y crear su índice único en POS; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catálogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NULL
    THROW 51000,'Primero aplicar la migración 27 de garita en POS.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'CK_GaritaRutaEvento_Tipo')
    THROW 51000,'No se encuentra la restricción de tipos esperada. Revisar el esquema POS.',1;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GaritaRutaEvento')
          AND name=N'CK_GaritaRutaEvento_Tipo' AND
          (definition NOT LIKE N'%''S''%' OR definition NOT LIKE N'%''D''%' OR definition NOT LIKE N'%''R''%'))
    THROW 51000,'La restricción de tipos no corresponde a la migración 27. Revisar antes de continuar.',1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'UX_GaritaRutaEvento_Entrada'
          AND (is_unique<>1 OR filter_definition IS NULL OR filter_definition NOT LIKE N'%Tipo%I%'))
    THROW 51000,'El índice de entrada ya existe con otra definición. Revisar antes de aplicar.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GaritaRutaEvento')
              AND name=N'CK_GaritaRutaEvento_Tipo' AND definition NOT LIKE N'%''I''%')
    BEGIN
        ALTER TABLE dbo.GaritaRutaEvento DROP CONSTRAINT CK_GaritaRutaEvento_Tipo;
        ALTER TABLE dbo.GaritaRutaEvento WITH CHECK ADD CONSTRAINT CK_GaritaRutaEvento_Tipo
            CHECK(Tipo IN(N'S',N'D',N'R',N'I'));
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.GaritaRutaEvento') AND name=N'UX_GaritaRutaEvento_Entrada')
        CREATE UNIQUE INDEX UX_GaritaRutaEvento_Entrada ON dbo.GaritaRutaEvento(CatalogoRutas,Ruta_Id) WHERE Tipo=N'I';
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'INSTALADO' Estado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
