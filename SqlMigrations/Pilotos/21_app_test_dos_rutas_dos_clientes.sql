/* APP_TEST - CREAR DOS RUTAS DEMO CON DOS CLIENTES Y TRES FACTURAS POR CLIENTE.
   Usa una ruta existente solo como plantilla estructural.
   No ejecutar en APK66 productiva. Vista previa por defecto. */
SET NOCOUNT ON;

DECLARE @BaseEsperada sysname=N'APP_TEST';
DECLARE @RutaPlantilla nvarchar(15)=N'PC25-1000851';
DECLARE @FechaPrueba date=CONVERT(date,GETDATE());
DECLARE @Piloto nvarchar(90)=N'JULIO RIVAS';
DECLARE @Centro nvarchar(15)=N'PC';
DECLARE @Placa nvarchar(15)=N'C-951BRX';
DECLARE @Operador nvarchar(15)=N'mlopez';
DECLARE @Aplicar bit=0; -- Revisar primero; cambiar a 1 en una ventana nueva.
DECLARE @HuellaTriggerEsperada varchar(64)=N'FBA876E099138CB1F2D369EBB3446D99388FF52F1BF6060B70BEA5826E4A6CB1';
DECLARE @HuellaTriggerActual varchar(64);

IF DB_NAME()<>@BaseEsperada
    THROW 51000,'Este script solo puede ejecutarse en APP_TEST.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usa una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.RT_RUTAS',N'U') IS NULL OR OBJECT_ID(N'dbo.RT_RUTAS_DET',N'U') IS NULL
    THROW 51000,'Falta la estructura esperada de rutas.',1;

SELECT @HuellaTriggerActual=CONVERT(varchar(64),HASHBYTES('SHA2_256',m.definition),2)
FROM sys.triggers t JOIN sys.sql_modules m ON m.object_id=t.object_id
WHERE t.parent_id=OBJECT_ID(N'dbo.RT_RUTAS')
  AND t.name=N'INSERT_MA_CUST_ORDER_LISTAS' AND t.is_disabled=0;
IF @HuellaTriggerActual<>@HuellaTriggerEsperada OR @HuellaTriggerActual IS NULL
    THROW 51000,'El trigger corregido no coincide o esta deshabilitado.',1;

CREATE TABLE #RutasDemo
(
    Numero tinyint NOT NULL PRIMARY KEY,
    ID_RUTA nvarchar(15) NOT NULL UNIQUE
);
INSERT #RutasDemo(Numero,ID_RUTA)
VALUES (4,N'QA26-000004'),(5,N'QA26-000005');

IF NOT EXISTS
(
    SELECT 1 FROM dbo.RT_RUTAS
    WHERE ID_RUTA=@RutaPlantilla
      AND PILOTO=@Piloto AND CENTRO_DIST=@Centro AND PLACA=@Placa
)
    THROW 51000,'La ruta plantilla no coincide con piloto, centro y placa.',1;
IF (SELECT COUNT(*) FROM dbo.RT_RUTAS_DET WHERE ID_RUTA=@RutaPlantilla)<>4
    THROW 51000,'La ruta plantilla debe contener exactamente cuatro documentos.',1;
IF EXISTS
(
    SELECT 1 FROM dbo.RT_RUTAS_DET
    WHERE ID_RUTA=@RutaPlantilla AND TIPO<>N'FACTURA'
)
    THROW 51000,'Los cuatro documentos de la plantilla deben ser FACTURA.',1;
IF EXISTS
(
    SELECT 1 FROM dbo.RT_RUTAS r JOIN #RutasDemo q ON q.ID_RUTA=r.ID_RUTA
)
    THROW 51000,'Ya existe al menos una ruta demo. No se sobrescribio nada.',1;

IF @Aplicar=0
BEGIN
    SELECT
        DB_NAME() AS BaseSeleccionada,
        q.Numero,
        q.ID_RUTA,
        @FechaPrueba AS FECHA_RUTA,
        N'E' AS STATUS,
        @Piloto AS PILOTO,
        @Centro AS CENTRO_DIST,
        @Placa AS PLACA,
        6 AS Facturas,
        @Aplicar AS Aplicar
    FROM #RutasDemo q
    ORDER BY q.Numero;

    ;WITH Plantilla AS
    (
        SELECT d.ROWID,d.ID_EMPRESA,d.ID_DOCUMENTO,
               ROW_NUMBER() OVER(ORDER BY d.ROWID) AS NumeroDocumento
        FROM dbo.RT_RUTAS_DET d
        WHERE d.ID_RUTA=@RutaPlantilla
    )
    SELECT
        q.ID_RUTA,
        n.NumeroDocumento,
        N'FACTURA' AS TIPO,
        p.ID_EMPRESA,
        N'QB'+RIGHT(N'00'+CONVERT(nvarchar(2),q.Numero),2)
             +RIGHT(N'00'+CONVERT(nvarchar(2),n.NumeroDocumento),2) AS ID_DOCUMENTO,
        N'CLIENTE FICTICIO '+CONVERT(nvarchar(2),q.Numero)
             +CASE WHEN n.NumeroDocumento<=3 THEN N' A' ELSE N' B' END AS CLIENTE,
        N'DIRECCION FICTICIA '+CASE WHEN n.NumeroDocumento<=3 THEN N'A' ELSE N'B' END+N' - NO ENTREGAR' AS DIR_DESPACHO
    FROM #RutasDemo q CROSS JOIN (VALUES(1),(2),(3),(4),(5),(6)) n(NumeroDocumento)
    JOIN Plantilla p ON p.NumeroDocumento=((n.NumeroDocumento-1)%4)+1
    ORDER BY q.Numero,n.NumeroDocumento;
    RETURN;
END;

DECLARE @ColumnasRuta nvarchar(max),@ValoresRuta nvarchar(max);
DECLARE @ColumnasDetalle nvarchar(max),@ValoresDetalle nvarchar(max);
DECLARE @Sql nvarchar(max);

/* Construye la copia con las columnas reales del catálogo y omite identity/computed/rowversion. */
SELECT @ColumnasRuta=STUFF
(
    (
        SELECT N','+QUOTENAME(c.name)
        FROM sys.columns c
        WHERE c.object_id=OBJECT_ID(N'dbo.RT_RUTAS')
          AND c.is_identity=0 AND c.is_computed=0 AND c.system_type_id<>189
        ORDER BY c.column_id
        FOR XML PATH(N''),TYPE
    ).value(N'.',N'nvarchar(max)'),1,1,N''
);
SELECT @ValoresRuta=STUFF
(
    (
        SELECT N','+CASE c.name
            WHEN N'ID_RUTA' THEN N'q.ID_RUTA'
            WHEN N'FECHA_RUTA' THEN N'@fecha'
            WHEN N'PLACA' THEN N'@placa'
            WHEN N'PILOTO' THEN N'@piloto'
            WHEN N'CENTRO_DIST' THEN N'@centro'
            WHEN N'STATUS' THEN N'N''E'''
            WHEN N'LIQUIDADO' THEN N'CONVERT(bit,0)'
            WHEN N'ID_USR' THEN N'@operador'
            WHEN N'USR_CON' THEN N'@operador'
            WHEN N'FECHA_CON' THEN N'GETDATE()'
            WHEN N'USR_MON' THEN N'NULL'
            WHEN N'FECHA_MON' THEN N'NULL'
            WHEN N'USR_LIQ' THEN N'NULL'
            WHEN N'FECHA_LIQ' THEN N'NULL'
            WHEN N'USR_MOD' THEN N'@operador'
            WHEN N'FECHA_MOD' THEN N'GETDATE()'
            WHEN N'OBSERVACIONES' THEN N'N''RUTA DEMO PORTAL PILOTOS'''
            WHEN N'OBSR_GENERAL' THEN N'N''RUTA DEMO PORTAL PILOTOS'''
            ELSE N'origen.'+QUOTENAME(c.name) END
        FROM sys.columns c
        WHERE c.object_id=OBJECT_ID(N'dbo.RT_RUTAS')
          AND c.is_identity=0 AND c.is_computed=0 AND c.system_type_id<>189
        ORDER BY c.column_id
        FOR XML PATH(N''),TYPE
    ).value(N'.',N'nvarchar(max)'),1,1,N''
);

SELECT @ColumnasDetalle=STUFF
(
    (
        SELECT N','+QUOTENAME(c.name)
        FROM sys.columns c
        WHERE c.object_id=OBJECT_ID(N'dbo.RT_RUTAS_DET')
          AND c.is_identity=0 AND c.is_computed=0 AND c.system_type_id<>189
        ORDER BY c.column_id
        FOR XML PATH(N''),TYPE
    ).value(N'.',N'nvarchar(max)'),1,1,N''
);
SELECT @ValoresDetalle=STUFF
(
    (
        SELECT N','+CASE c.name
            WHEN N'ID_RUTA' THEN N'q.ID_RUTA'
            WHEN N'TIPO' THEN N'N''FACTURA'''
            WHEN N'ID_DOCUMENTO' THEN N'N''QB''+RIGHT(N''00''+CONVERT(nvarchar(2),q.Numero),2)+RIGHT(N''00''+CONVERT(nvarchar(2),d.NumeroDocumento),2)'
            WHEN N'F_DOCTO' THEN N'@fecha'
            WHEN N'CLIENTE' THEN N'N''CLIENTE FICTICIO ''+CONVERT(nvarchar(2),q.Numero)+CASE WHEN d.NumeroDocumento<=3 THEN N'' A'' ELSE N'' B'' END'
            WHEN N'NOM_DESPACHO' THEN N'N''ENTREGA FICTICIA ''+CONVERT(nvarchar(2),q.Numero)+CASE WHEN d.NumeroDocumento<=3 THEN N'' A'' ELSE N'' B'' END'
            WHEN N'DIR_DESPACHO' THEN N'N''DIRECCION FICTICIA ''+CASE WHEN d.NumeroDocumento<=3 THEN N''A'' ELSE N''B'' END+N'' - NO ENTREGAR'''
            WHEN N'MO_VISITO' THEN N'NULL'
            WHEN N'MO_ENTREGA' THEN N'NULL'
            WHEN N'MO_HR_SALIDA' THEN N'NULL'
            WHEN N'MO_HR_ENTRADA' THEN N'NULL'
            WHEN N'MO_OBSER' THEN N'NULL'
            WHEN N'MO_MOTIVO' THEN N'NULL'
            WHEN N'U_Usuario_liquida' THEN N'NULL'
            WHEN N'U_U_FechaLiquidacion' THEN N'NULL'
            WHEN N'U_Observa_Liquida_Fac' THEN N'NULL'
            ELSE N'd.'+QUOTENAME(c.name) END
        FROM sys.columns c
        WHERE c.object_id=OBJECT_ID(N'dbo.RT_RUTAS_DET')
          AND c.is_identity=0 AND c.is_computed=0 AND c.system_type_id<>189
        ORDER BY c.column_id
        FOR XML PATH(N''),TYPE
    ).value(N'.',N'nvarchar(max)'),1,1,N''
);

IF NULLIF(@ColumnasRuta,N'') IS NULL OR NULLIF(@ValoresRuta,N'') IS NULL
   OR NULLIF(@ColumnasDetalle,N'') IS NULL OR NULLIF(@ValoresDetalle,N'') IS NULL
    THROW 51000,'No se pudo leer la estructura de las tablas.',1;

SET @Sql=N'
INSERT dbo.RT_RUTAS('+@ColumnasRuta+N')
SELECT '+@ValoresRuta+N'
FROM #RutasDemo q
CROSS JOIN dbo.RT_RUTAS origen
WHERE origen.ID_RUTA=@plantilla;

;WITH PlantillaDetalle AS
(
    SELECT d.*,ROW_NUMBER() OVER(ORDER BY d.ROWID) AS PlantillaNumero
    FROM dbo.RT_RUTAS_DET d
    WHERE d.ID_RUTA=@plantilla
), Seis AS
(
    SELECT d.*,n.NumeroDocumento
    FROM PlantillaDetalle d CROSS JOIN (VALUES(1),(2),(3),(4),(5),(6)) n(NumeroDocumento)
    WHERE d.PlantillaNumero=((n.NumeroDocumento-1)%4)+1
)
INSERT dbo.RT_RUTAS_DET('+@ColumnasDetalle+N')
SELECT '+@ValoresDetalle+N'
FROM #RutasDemo q CROSS JOIN Seis d;';

SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    /* Revalida bajo bloqueo para impedir duplicados entre vista previa y aplicación. */
    IF EXISTS
    (
        SELECT 1 FROM dbo.RT_RUTAS WITH(UPDLOCK,HOLDLOCK)
        WHERE ID_RUTA IN(N'QA26-000004',N'QA26-000005')
    )
        THROW 51000,'Una ruta demo fue creada por otra sesión. No se insertó nada.',1;

    EXEC sys.sp_executesql @Sql,
        N'@plantilla nvarchar(15),@fecha date,@piloto nvarchar(90),@centro nvarchar(15),@placa nvarchar(15),@operador nvarchar(15)',
        @plantilla=@RutaPlantilla,@fecha=@FechaPrueba,@piloto=@Piloto,
        @centro=@Centro,@placa=@Placa,@operador=@Operador;

    IF (SELECT COUNT(*) FROM dbo.RT_RUTAS WHERE ID_RUTA IN(N'QA26-000004',N'QA26-000005'))<>2
        THROW 51000,'No se crearon exactamente dos cabeceras.',1;
    IF (SELECT COUNT(*) FROM dbo.RT_RUTAS_DET WHERE ID_RUTA IN(N'QA26-000004',N'QA26-000005'))<>12
        THROW 51000,'No se crearon exactamente doce facturas.',1;
    IF EXISTS
    (
        SELECT 1 FROM dbo.RT_RUTAS_DET
        WHERE ID_RUTA IN(N'QA26-000004',N'QA26-000005')
          AND (TIPO<>N'FACTURA' OR MO_VISITO IS NOT NULL OR MO_ENTREGA IS NOT NULL
               OR MO_MOTIVO IS NOT NULL OR MO_OBSER IS NOT NULL)
    )
        THROW 51000,'Una factura demo no quedó limpia.',1;
    IF EXISTS (SELECT 1 FROM dbo.RT_RUTAS_DET d WHERE d.ID_RUTA IN(N'QA26-000004',N'QA26-000005')
               GROUP BY d.ID_RUTA,d.CLIENTE,d.DIR_DESPACHO HAVING COUNT(*)<>3)
       OR (SELECT COUNT(*) FROM (SELECT d.ID_RUTA,d.CLIENTE,d.DIR_DESPACHO FROM dbo.RT_RUTAS_DET d
           WHERE d.ID_RUTA IN(N'QA26-000004',N'QA26-000005') GROUP BY d.ID_RUTA,d.CLIENTE,d.DIR_DESPACHO) c)<>4
        THROW 51000,'Se esperaban dos clientes con tres facturas en cada ruta.',1;

    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;

    SELECT N'RUTAS DEMO CREADAS' AS Resultado,r.ID_RUTA,r.FECHA_RUTA,r.STATUS,
           r.PILOTO,r.CENTRO_DIST,r.PLACA,r.LIQUIDADO,COUNT(d.ROWID) AS Facturas
    FROM dbo.RT_RUTAS r JOIN dbo.RT_RUTAS_DET d ON d.ID_RUTA=r.ID_RUTA
    WHERE r.ID_RUTA IN(N'QA26-000004',N'QA26-000005')
    GROUP BY r.ID_RUTA,r.FECHA_RUTA,r.STATUS,r.PILOTO,r.CENTRO_DIST,r.PLACA,r.LIQUIDADO
    ORDER BY r.ID_RUTA;

    SELECT d.ID_RUTA,d.ROWID,d.TIPO,d.ID_EMPRESA,d.ID_DOCUMENTO,
           d.CLIENTE,d.MO_VISITO,d.MO_ENTREGA,d.MO_MOTIVO,d.MO_OBSER
    FROM dbo.RT_RUTAS_DET d
    WHERE d.ID_RUTA IN(N'QA26-000004',N'QA26-000005')
    ORDER BY d.ID_RUTA,d.ROWID;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
