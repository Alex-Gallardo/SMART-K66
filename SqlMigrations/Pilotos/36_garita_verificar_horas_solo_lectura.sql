/* Diagnostico SOLO LECTURA. Ejecutar en POS tras las migraciones.
   Declarar el catalogo APK66 exacto y una ruta real de prueba.
   Compara ambas horas RT_RUTAS con sus eventos POS sin mostrar clientes. */
DECLARE @CatalogoRutas sysname=NULL,@RutaId nvarchar(15)=NULL;
IF @CatalogoRutas IS NULL OR DB_ID(@CatalogoRutas) IS NULL OR @RutaId IS NULL OR LEN(LTRIM(RTRIM(@RutaId)))=0
    THROW 51000,'Declarar el catalogo de rutas y el ID de una ruta para verificar.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1
    THROW 51000,'Usar una ventana nueva sin transaccion ni opciones modificadas.',1;
IF OBJECT_ID(QUOTENAME(@CatalogoRutas)+N'.dbo.RT_RUTAS',N'U') IS NULL OR
   OBJECT_ID(N'dbo.GaritaRutaEvento',N'U') IS NULL
    THROW 51000,'Falta RT_RUTAS o la tabla de eventos POS.',1;
DECLARE @Sql nvarchar(max)=N'SELECT r.ID_RUTA,r.STATUS,r.LIQUIDADO,
       r.GARITA_FECHA_SALIDA SalidaAPK66,s.FechaUtc SalidaPOSUtc,DATEADD(HOUR,-6,s.FechaUtc) SalidaPOSGuatemala,
       r.GARITA_FECHA_ENTRADA EntradaAPK66,i.FechaUtc EntradaPOSUtc,DATEADD(HOUR,-6,i.FechaUtc) EntradaPOSGuatemala,
       CASE WHEN r.GARITA_FECHA_SALIDA IS NULL AND s.FechaUtc IS NULL THEN N''SIN SALIDA''
            WHEN r.GARITA_FECHA_SALIDA=DATEADD(HOUR,-6,s.FechaUtc) THEN N''COINCIDE'' ELSE N''REVISAR'' END ComparacionSalida,
       CASE WHEN r.GARITA_FECHA_ENTRADA IS NULL AND i.FechaUtc IS NULL THEN N''SIN ENTRADA''
            WHEN r.GARITA_FECHA_ENTRADA=DATEADD(HOUR,-6,i.FechaUtc) THEN N''COINCIDE'' ELSE N''REVISAR'' END ComparacionEntrada
FROM '+QUOTENAME(@CatalogoRutas)+N'.dbo.RT_RUTAS r
OUTER APPLY (SELECT TOP (1) e.FechaUtc FROM dbo.GaritaRutaEvento e WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT AND e.Tipo=N''S'') s
OUTER APPLY (SELECT TOP (1) e.FechaUtc FROM dbo.GaritaRutaEvento e WHERE e.CatalogoRutas=@catalogo AND e.Ruta_Id=r.ID_RUTA COLLATE DATABASE_DEFAULT AND e.Tipo=N''I'') i
WHERE r.ID_RUTA=@ruta;';
EXEC sys.sp_executesql @Sql,N'@catalogo sysname,@ruta nvarchar(15)',@catalogo=@CatalogoRutas,@ruta=@RutaId;
