/* MANUAL, revisar tamanos/planes y ventana de mantenimiento antes de aplicar.
   Crear indices puede bloquear y generar log. El timeout solo limita la espera
   por un bloqueo, no la duracion de la construccion. No reintentar en bucle.
   Usar una ventana NUEVA; seleccionar el catalogo de rutas exacto. */
DECLARE @BaseEsperada sysname=NULL, @Aplicar bit=0;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() AS BaseSeleccionada, N'VISTA PREVIA: indices para consultas y bloqueo por ruta; no hay cambios.' AS Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000, 'Declarar y seleccionar el catalogo de rutas destino exacto.', 1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000, 'Usar una ventana nueva sin transacciones ni opciones de sesion modificadas.', 1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RT_RUTAS') AND name=N'IX_PortalPiloto_Rutas')
 OR EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RT_RUTAS_DET') AND name=N'IX_PortalPiloto_Detalle')
 OR EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RT_EMPLEADOS') AND name=N'IX_PortalPiloto_Empleado')
    THROW 51000, 'Ya existe un indice del portal. Comparar su definicion antes de continuar.', 1;
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE INDEX IX_PortalPiloto_Rutas ON dbo.RT_RUTAS(PILOTO,FECHA_RUTA,ID_RUTA) INCLUDE(PLACA,CENTRO_DIST,STATUS);
    CREATE INDEX IX_PortalPiloto_Detalle ON dbo.RT_RUTAS_DET(ID_RUTA,ROWID);
    CREATE INDEX IX_PortalPiloto_Empleado ON dbo.RT_EMPLEADOS(CARGO,NOMBRE,ROWID) INCLUDE(ESTADO);
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
