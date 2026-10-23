/* Ejecutar manualmente en APK66 despues de 33 para copiar salidas POS previas
   a rutas NO liquidadas que aun no tengan hora en RT_RUTAS.
   No modifica eventos POS ni valores APK66 existentes. Vista previa por defecto. */
DECLARE @BaseEsperada sysname=NULL,@PosEsperada sysname=NULL,@Aplicar bit=0;
IF @Aplicar=0 AND @PosEsperada IS NULL
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: declarar @PosEsperada para contar salidas pendientes; sin cambios.' Accion;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada OR @PosEsperada IS NULL OR DB_ID(@PosEsperada) IS NULL OR @PosEsperada=@BaseEsperada
    THROW 51000,'Declarar los catalogos exactos APK66 y POS antes de continuar.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF COL_LENGTH(N'dbo.RT_RUTAS',N'GARITA_FECHA_SALIDA') IS NULL OR
   OBJECT_ID(QUOTENAME(@PosEsperada)+N'.dbo.GaritaRutaEvento',N'U') IS NULL
    THROW 51000,'Falta la columna APK66 o la tabla de eventos POS.',1;
DECLARE @Sql nvarchar(max);
IF @Aplicar=0
BEGIN
    SET @Sql=N'SELECT COUNT(*) SalidasPOSPendientes FROM dbo.RT_RUTAS r
JOIN '+QUOTENAME(@PosEsperada)+N'.dbo.GaritaRutaEvento e ON e.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT
WHERE e.CatalogoRutas=@catalogo AND e.Tipo=N''S'' AND r.GARITA_FECHA_SALIDA IS NULL AND ISNULL(r.LIQUIDADO,0)=0;';
    EXEC sys.sp_executesql @Sql,N'@catalogo sysname',@catalogo=@BaseEsperada;
    RETURN;
END;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    SET @Sql=N'UPDATE r SET GARITA_FECHA_SALIDA=DATEADD(HOUR,-6,e.FechaUtc)
FROM dbo.RT_RUTAS r JOIN '+QUOTENAME(@PosEsperada)+N'.dbo.GaritaRutaEvento e ON e.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT
WHERE e.CatalogoRutas=@catalogo AND e.Tipo=N''S'' AND r.GARITA_FECHA_SALIDA IS NULL AND ISNULL(r.LIQUIDADO,0)=0;
SELECT @@ROWCOUNT RutasActualizadas;';
    EXEC sys.sp_executesql @Sql,N'@catalogo sysname',@catalogo=@BaseEsperada;
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
