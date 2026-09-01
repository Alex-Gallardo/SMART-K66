/* =============================================================================
   BORRADORES NC — VALIDACION SOLO LECTURA DE DOCUMENTACION
   Destino exclusivo: POS-SmartK66

   No inserta, actualiza ni elimina datos. Puede ejecutarse despues de
   04_crear_adjuntos_seguro.sql y tras realizar pruebas desde la aplicacion.
   ============================================================================= */

USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
GO

IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    THROW 51420,
          'SEGURIDAD: este diagnostico solo esta autorizado para POS-SmartK66.',
          1;
END;
GO

SELECT
    N'04V_ENTORNO' AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME() AS BASE_ACTUAL,
    SYSDATETIME() AS FECHA_DIAGNOSTICO;

SELECT
    N'04V_COLUMNAS' AS SECCION,
    C.column_id AS POSICION,
    C.name AS COLUMNA,
    T.name AS TIPO,
    C.max_length AS LARGO_BYTES,
    C.is_nullable AS ES_NULLABLE,
    C.is_identity AS ES_IDENTIDAD
FROM sys.columns C
JOIN sys.types T ON T.user_type_id = C.user_type_id
WHERE C.object_id = OBJECT_ID(N'dbo.BORR_NC_ADJUNTO')
ORDER BY C.column_id;

SELECT
    N'04V_RESUMEN' AS SECCION,
    A.TIPO,
    COUNT_BIG(*) AS CANTIDAD,
    SUM(CASE WHEN A.TIPO = 'ARCHIVO' THEN A.TAMANO ELSE 0 END) AS BYTES_ARCHIVOS
FROM dbo.BORR_NC_ADJUNTO A
GROUP BY A.TIPO
ORDER BY A.TIPO;

SELECT
    N'04V_MUESTRA' AS SECCION,
    A.ADJUNTO_ID,
    A.ID_EMPRESA,
    A.ID_BORRADOR,
    A.TIPO,
    A.NOMBRE,
    A.EXTENSION,
    A.CONTENT_TYPE,
    A.TAMANO,
    A.URL,
    A.ORDEN,
    A.ID_USR,
    A.REGISTRO,
    CASE WHEN A.CONTENIDO IS NULL THEN 0 ELSE DATALENGTH(A.CONTENIDO) END AS BYTES_PERSISTIDOS
FROM dbo.BORR_NC_ADJUNTO A
ORDER BY A.REGISTRO DESC, A.ADJUNTO_ID DESC;

SELECT
    N'04V_INTEGRIDAD' AS SECCION,
    N'Adjuntos huerfanos' AS VALIDACION,
    COUNT_BIG(*) AS REAL,
    CASE WHEN COUNT_BIG(*) = 0 THEN N'OK' ELSE N'REVISAR' END AS RESULTADO
FROM dbo.BORR_NC_ADJUNTO A
LEFT JOIN dbo.BORR_NC_ENC E
  ON E.ID_EMPRESA = A.ID_EMPRESA
 AND E.ID_BORRADOR = A.ID_BORRADOR
WHERE E.ID_BORRADOR IS NULL;
GO

PRINT N'OK: diagnostico de adjuntos finalizado en POS-SmartK66.';
GO
