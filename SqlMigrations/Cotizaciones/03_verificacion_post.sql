/* =============================================================================
   COTIZACIONES — VERIFICACIÓN POSTERIOR DE SOLO LECTURA
   Ejecutar después de 01 y 02 en POS-SmartK66 y enviar todos los resultados.
   No crea ni modifica objetos.
   ============================================================================= */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;

IF DB_NAME() <> N'POS-SmartK66'
    THROW 53200, 'SEGURIDAD: este script solo se ejecuta en POS-SmartK66.', 1;

SELECT N'V01_TABLAS' AS SECCION, S.name AS ESQUEMA, O.name AS TABLA,
       SUM(CASE WHEN P.index_id IN (0,1) THEN P.rows ELSE 0 END) AS FILAS
FROM sys.objects O
JOIN sys.schemas S ON S.schema_id = O.schema_id
LEFT JOIN sys.partitions P ON P.object_id = O.object_id
WHERE O.type = 'U' AND O.name IN (N'COT_SERIES', N'COT_ENC', N'COT_DET')
GROUP BY S.name, O.name
ORDER BY O.name;

SELECT N'V02_COLUMNAS' AS SECCION, O.name AS TABLA, C.column_id,
       C.name AS COLUMNA, T.name AS TIPO, C.max_length, C.precision,
       C.scale, C.is_nullable
FROM sys.objects O
JOIN sys.columns C ON C.object_id = O.object_id
JOIN sys.types T ON T.user_type_id = C.user_type_id
WHERE O.name IN (N'COT_SERIES', N'COT_ENC', N'COT_DET')
ORDER BY O.name, C.column_id;

SELECT N'V03_REGLAS' AS SECCION, O.name AS TABLA, X.name AS REGLA,
       X.type_desc AS TIPO
FROM sys.objects O
JOIN sys.objects X ON X.parent_object_id = O.object_id
WHERE O.name IN (N'COT_SERIES', N'COT_ENC', N'COT_DET')
  AND X.type IN ('PK', 'UQ', 'F', 'C', 'D')
ORDER BY O.name, X.type_desc, X.name;

SELECT N'V04_INDICES' AS SECCION, O.name AS TABLA, I.name AS INDICE,
       I.type_desc, I.is_unique, I.is_primary_key
FROM sys.objects O
JOIN sys.indexes I ON I.object_id = O.object_id
WHERE O.name IN (N'COT_SERIES', N'COT_ENC', N'COT_DET')
  AND I.index_id > 0
ORDER BY O.name, I.index_id;

SELECT N'V05_SERIES' AS SECCION, EMPRESA, SERIE, NUMERACION, ACTIVO, MODIFICADO
FROM dbo.COT_SERIES
ORDER BY EMPRESA;

SELECT N'V06_INTEGRIDAD' AS SECCION,
       (SELECT COUNT_BIG(*) FROM dbo.COT_ENC) AS ENCABEZADOS,
       (SELECT COUNT_BIG(*) FROM dbo.COT_DET) AS DETALLES,
       (SELECT COUNT_BIG(*)
          FROM dbo.COT_DET D
          LEFT JOIN dbo.COT_ENC E
            ON E.ID_EMPRESA = D.ID_EMPRESA
           AND E.ID_COTIZACION = D.ID_COTIZACION
         WHERE E.ID_COTIZACION IS NULL) AS DETALLES_HUERFANOS;

SELECT N'V07_PERMISOS' AS SECCION, Nombre, Descripcion, Modulo
FROM dbo.Permiso
WHERE Nombre LIKE N'Control.Cotizaciones.%'
ORDER BY Nombre;

SELECT N'V08_ROLES' AS SECCION, R.Nombre AS ROL, RP.Permiso_Id
FROM dbo.Rol R
JOIN dbo.Rol_Permiso RP ON RP.Rol_Id = R.Rol_Id
WHERE RP.Permiso_Id LIKE N'Control.Cotizaciones.%'
ORDER BY R.Nombre, RP.Permiso_Id;

SELECT N'V09_MENU' AS SECCION, Menu_Id, Menu_Padre_Id, Nombre, Titulo,
       Action, Controller, Orden, IconName, IsActive, PermisoId
FROM dbo.Menu
WHERE Controller = N'Cotizacion'
ORDER BY Menu_Id;
GO
