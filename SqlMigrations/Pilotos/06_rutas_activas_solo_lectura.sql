/* DIAGNOSTICO DE SOLO LECTURA: rutas abiertas (A) y en ruta (E).
   En SSMS: ventana NUEVA en la base de rutas. Completar @BaseEsperada.
   Devuelve como maximo 20 cabeceras de los ultimos 14 dias; sin documentos,
   clientes, telefonos ni credenciales. No crea tablas ni ejecuta procedimientos.
   LOCK_TIMEOUT limita esperas por bloqueo, no la duracion total de la consulta.
   Si hay timeout/error, detenerse y compartir el mensaje; no reintentar en bucle. */
DECLARE @BaseEsperada sysname=NULL; -- Escribir localmente el nombre exacto de la base de rutas.
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar la base de rutas correcta antes de consultar.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
DECLARE @Hasta date=CONVERT(date,GETDATE());
DECLARE @Desde date=DATEADD(day,-13,@Hasta);
SET LOCK_TIMEOUT 3000;
BEGIN TRY
    SELECT TOP (20) r.ID_RUTA,r.FECHA_RUTA,r.STATUS,r.PLACA,r.PILOTO,r.CENTRO_DIST
    FROM dbo.RT_RUTAS r
    WHERE r.FECHA_RUTA>=@Desde AND r.FECHA_RUTA<DATEADD(day,1,@Hasta)
      AND r.STATUS IN(N'A',N'E')
    ORDER BY r.FECHA_RUTA DESC,r.ID_RUTA DESC
    OPTION(MAXDOP 1);
    SET LOCK_TIMEOUT -1;
    SELECT @Desde AS DesdeIncluido,@Hasta AS HastaIncluido,
       N'A=Abierta; E=En ruta. Sin filas significa que no se encontraron rutas activas en este periodo. No ampliar permisos ni cambiar estados.' AS Nota;
END TRY
BEGIN CATCH
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
