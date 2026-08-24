/* =============================================================================
   BORRADORES NC — DIAGNÓSTICO PREVIO DE SOLO LECTURA
   Destino autorizado: POS-SmartK66 (PRUEBAS)

   Objetivo
   --------
   Confirmar el estado real de objetos, columnas, índices, permisos, menú,
   roles, empresas y series antes de preparar cualquier cambio de esquema.

   Cómo ejecutarlo
   ---------------
   1. Abra una ventana NUEVA en SSMS.
   2. Confirme que está conectado al servidor de pruebas.
   3. Ejecute el archivo COMPLETO, sin seleccionar bloques parciales.
   4. No edite la línea USE: el guard clause exige POS-SmartK66.
   5. Envíe todos los result sets, conservando la columna SECCION.

   Garantía de alcance
   -------------------
   El código activo de este archivo contiene únicamente cambio de contexto,
   opciones de sesión y consultas. No crea, modifica ni elimina objetos o datos.
   ============================================================================= */

USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
GO

IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    THROW 51000, 'SEGURIDAD: este diagnostico solo esta autorizado para POS-SmartK66.', 1;
END;
GO

/* 00 — Identidad inequívoca del entorno ----------------------------------- */
SELECT
    N'00_ENTORNO'                              AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME()                                  AS BASE_ACTUAL,
    CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL,
    CONVERT(nvarchar(30), SERVERPROPERTY('Edition')) AS EDICION_SQL,
    CONVERT(nvarchar(30), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) AS ACTUALIZABLE,
    SYSDATETIME()                              AS FECHA_DIAGNOSTICO;
GO

/* 01 — Presencia de objetos requeridos ------------------------------------ */
;WITH ESPERADOS AS
(
    SELECT *
    FROM (VALUES
        ( 1, N'dbo', N'BORR_NC_SERIES',       N'USER_TABLE', N'MODULO_NUEVO'),
        ( 2, N'dbo', N'BORR_NC_ENC',          N'USER_TABLE', N'MODULO_NUEVO'),
        ( 3, N'dbo', N'BORR_NC_DET',          N'USER_TABLE', N'MODULO_NUEVO'),
        ( 4, N'dbo', N'VW_BORR_NC_ACUMULADO', N'VIEW',       N'MODULO_NUEVO'),
        (10, N'dbo', N'Permiso',               N'USER_TABLE', N'EXISTENTE'),
        (11, N'dbo', N'Menu',                  N'USER_TABLE', N'EXISTENTE'),
        (12, N'dbo', N'Rol',                   N'USER_TABLE', N'EXISTENTE'),
        (13, N'dbo', N'Rol_Permiso',           N'USER_TABLE', N'EXISTENTE'),
        (14, N'dbo', N'Usuario',               N'USER_TABLE', N'EXISTENTE'),
        (15, N'dbo', N'Usuario_Rol',           N'USER_TABLE', N'EXISTENTE'),
        (16, N'dbo', N'Usuario_Empresa',       N'USER_TABLE', N'EXISTENTE'),
        (17, N'dbo', N'Empresa',               N'USER_TABLE', N'EXISTENTE'),
        (18, N'dbo', N'REC_CAJA_SERIES',       N'USER_TABLE', N'EXISTENTE')
    ) V(ORDEN, ESQUEMA, OBJETO, TIPO_ESPERADO, GRUPO)
)
SELECT
    N'01_OBJETOS' AS SECCION,
    E.GRUPO,
    E.ESQUEMA,
    E.OBJETO,
    E.TIPO_ESPERADO,
    ISNULL(O.type_desc, N'AUSENTE') AS TIPO_REAL,
    CASE WHEN O.object_id IS NULL THEN N'AUSENTE' ELSE N'OK' END AS RESULTADO
FROM ESPERADOS E
LEFT JOIN sys.schemas S
       ON S.name = E.ESQUEMA
LEFT JOIN sys.objects O
       ON O.schema_id = S.schema_id
      AND O.name = E.OBJETO
      AND O.type IN ('U', 'V')
ORDER BY E.ORDEN;
GO

/* 02 — Columnas reales de las tablas que usa el módulo -------------------- */
SELECT
    N'02_COLUMNAS' AS SECCION,
    S.name         AS ESQUEMA,
    T.name         AS TABLA,
    C.column_id    AS POSICION,
    C.name         AS COLUMNA,
    TY.name        AS TIPO,
    CASE
        WHEN TY.name IN (N'nvarchar', N'nchar') AND C.max_length > 0
            THEN C.max_length / 2
        ELSE C.max_length
    END            AS LARGO_CARACTERES,
    C.precision    AS PRECISION_NUMERICA,
    C.scale        AS ESCALA_NUMERICA,
    C.is_nullable  AS ACEPTA_NULL,
    C.is_identity  AS ES_IDENTITY,
    CASE WHEN DC.object_id IS NULL THEN NULL ELSE DC.definition END AS DEFAULT_SQL
FROM sys.tables T
JOIN sys.schemas S
  ON S.schema_id = T.schema_id
JOIN sys.columns C
  ON C.object_id = T.object_id
JOIN sys.types TY
  ON TY.user_type_id = C.user_type_id
LEFT JOIN sys.default_constraints DC
  ON DC.parent_object_id = C.object_id
 AND DC.parent_column_id = C.column_id
WHERE S.name = N'dbo'
  AND T.name IN
      (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET',
       N'Permiso', N'Menu', N'Rol', N'Rol_Permiso',
       N'Usuario', N'Usuario_Rol', N'Usuario_Empresa',
       N'Empresa', N'REC_CAJA_SERIES')
ORDER BY T.name, C.column_id;
GO

/* 03 — Restricciones del módulo si ya existe total o parcialmente ---------- */
SELECT
    N'03_RESTRICCIONES' AS SECCION,
    OBJECT_SCHEMA_NAME(O.parent_object_id) AS ESQUEMA,
    OBJECT_NAME(O.parent_object_id)        AS TABLA,
    O.name                                 AS RESTRICCION,
    O.type_desc                            AS TIPO,
    CASE
        WHEN O.type = 'C' THEN CC.definition
        ELSE NULL
    END                                    AS DEFINICION_CHECK
FROM sys.objects O
LEFT JOIN sys.check_constraints CC
       ON CC.object_id = O.object_id
WHERE O.type IN ('PK', 'UQ', 'F', 'C', 'D')
  AND OBJECT_NAME(O.parent_object_id) IN
      (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET')
ORDER BY TABLA, TIPO, RESTRICCION;
GO

/* 04 — Índices reales; PK y UNIQUE también aparecen aquí ------------------ */
SELECT
    N'04_INDICES' AS SECCION,
    OBJECT_SCHEMA_NAME(I.object_id) AS ESQUEMA,
    OBJECT_NAME(I.object_id)        AS TABLA,
    I.name                          AS INDICE,
    I.type_desc                     AS TIPO,
    I.is_unique                     AS ES_UNICO,
    I.is_primary_key                AS ES_PK,
    STUFF
    (
        (
            SELECT N', ' + QUOTENAME(C.name) +
                   CASE WHEN IC.is_descending_key = 1 THEN N' DESC' ELSE N' ASC' END
            FROM sys.index_columns IC
            JOIN sys.columns C
              ON C.object_id = IC.object_id
             AND C.column_id = IC.column_id
            WHERE IC.object_id = I.object_id
              AND IC.index_id = I.index_id
              AND IC.is_included_column = 0
            ORDER BY IC.key_ordinal
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, N''
    ) AS COLUMNAS_CLAVE,
    STUFF
    (
        (
            SELECT N', ' + QUOTENAME(C.name)
            FROM sys.index_columns IC
            JOIN sys.columns C
              ON C.object_id = IC.object_id
             AND C.column_id = IC.column_id
            WHERE IC.object_id = I.object_id
              AND IC.index_id = I.index_id
              AND IC.is_included_column = 1
            ORDER BY C.column_id
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, N''
    ) AS COLUMNAS_INCLUIDAS
FROM sys.indexes I
WHERE OBJECT_NAME(I.object_id) IN
      (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET')
  AND I.name IS NOT NULL
ORDER BY TABLA, I.index_id;
GO

/* 05 — Permisos actuales: referencia ReciboCaja y filas del módulo -------- */
SELECT
    N'05A_PERMISOS' AS SECCION,
    P.Nombre,
    P.Descripcion,
    P.Modulo
FROM dbo.Permiso P
WHERE P.Nombre LIKE N'Control.ReciboCaja%'
   OR P.Nombre LIKE N'Control.BorradorNC%'
ORDER BY P.Nombre;
GO

/* 06 — Menú actual y padre usado por ReciboCaja --------------------------- */
SELECT
    N'06_MENU' AS SECCION,
    M.Menu_Id,
    M.Menu_Padre_Id,
    M.Nombre,
    M.Titulo,
    M.Action,
    M.Controller,
    M.Orden,
    M.IconName,
    M.IsActive,
    M.PermisoId
FROM dbo.Menu M
WHERE M.Controller IN (N'ReciboCaja', N'BorradorNc')
ORDER BY M.Menu_Padre_Id, M.Orden, M.Menu_Id;
GO

/* 07 — Roles disponibles, cantidad de usuarios y permisos BorradorNC ------ */
SELECT
    N'07A_ROLES' AS SECCION,
    R.Rol_Id,
    R.Nombre,
    COUNT(DISTINCT UR.Usuario_Id) AS USUARIOS_ASIGNADOS
FROM dbo.Rol R
LEFT JOIN dbo.Usuario_Rol UR
       ON UR.Rol_Id = R.Rol_Id
GROUP BY R.Rol_Id, R.Nombre
ORDER BY R.Nombre;

SELECT
    N'07B_ROL_PERMISO_BORRADOR' AS SECCION,
    R.Rol_Id,
    R.Nombre AS ROL,
    RP.Permiso_Id
FROM dbo.Rol R
JOIN dbo.Rol_Permiso RP
  ON RP.Rol_Id = R.Rol_Id
WHERE RP.Permiso_Id LIKE N'Control.BorradorNC%'
ORDER BY R.Nombre, RP.Permiso_Id;
GO

/* 08 — Empresas que el código C# reconoce y cobertura Usuario_Empresa ----- */
;WITH EMPRESAS_CODIGO AS
(
    SELECT *
    FROM (VALUES
        (CONVERT(bigint, 20210705001), N'BOLIK'),
        (CONVERT(bigint, 20210705003), N'FAES'),
        (CONVERT(bigint, 20210705004), N'GRACO')
    ) V(Empresa_Id, NOMBRE_CODIGO)
)
SELECT
    N'08A_EMPRESAS_CODIGO' AS SECCION,
    EC.Empresa_Id,
    EC.NOMBRE_CODIGO,
    E.Nombre AS NOMBRE_SQL,
    E.Nombre_Comercial,
    CASE WHEN E.Empresa_Id IS NULL THEN N'AUSENTE' ELSE N'OK' END AS RESULTADO
FROM EMPRESAS_CODIGO EC
LEFT JOIN dbo.Empresa E
       ON E.Empresa_Id = EC.Empresa_Id
ORDER BY EC.NOMBRE_CODIGO;

SELECT
    N'08B_COBERTURA_USUARIO_EMPRESA' AS SECCION,
    UE.Empresa_Id,
    E.Nombre,
    COUNT(DISTINCT UE.Usuario_Id) AS USUARIOS,
    COUNT(*) AS ASIGNACIONES,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(UE.Codigo, N''))), N'') IS NULL
             THEN 1 ELSE 0 END) AS CODIGOS_VACIOS,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(UE.DEPTO_RECIBO, N''))), N'') IS NULL
             THEN 1 ELSE 0 END) AS DEPTOS_VACIOS
FROM dbo.Usuario_Empresa UE
LEFT JOIN dbo.Empresa E
       ON E.Empresa_Id = UE.Empresa_Id
WHERE UE.Empresa_Id IN (20210705001, 20210705003, 20210705004)
GROUP BY UE.Empresa_Id, E.Nombre
ORDER BY E.Nombre;

SELECT DISTINCT TOP (100)
    N'08C_CODIGOS_OPERADOR' AS SECCION,
    UE.Empresa_Id,
    E.Nombre,
    UE.Codigo,
    UE.DEPTO_RECIBO,
    UE.SERIE_SAP
FROM dbo.Usuario_Empresa UE
LEFT JOIN dbo.Empresa E
       ON E.Empresa_Id = UE.Empresa_Id
WHERE UE.Empresa_Id IN (20210705001, 20210705003, 20210705004)
ORDER BY E.Nombre, UE.Codigo;
GO

/* 09 — Series de recibos, usadas como referencia de configuración ---------- */
SELECT TOP (200)
    N'09_SERIES_RECIBO' AS SECCION,
    S.*
FROM dbo.REC_CAJA_SERIES S
WHERE S.EMPRESA IN (N'BOLIK', N'FAES', N'GRACO')
ORDER BY S.EMPRESA, S.DEPTO;
GO

/* 10 — Estado de datos BorradorNC si hubo un intento anterior -------------- */
IF OBJECT_ID(N'dbo.BORR_NC_SERIES', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''10A_SERIES_BORRADOR'' AS SECCION,
               EMPRESA, SERIE, NUMERACION, ACTIVO
        FROM dbo.BORR_NC_SERIES
        ORDER BY EMPRESA;';
END
ELSE
BEGIN
    SELECT N'10A_SERIES_BORRADOR' AS SECCION,
           N'NO EXISTE dbo.BORR_NC_SERIES' AS RESULTADO;
END;

IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''10B_VOLUMEN_BORRADOR'' AS SECCION,
               E.ID_EMPRESA,
               E.ESTADO,
               COUNT_BIG(*) AS ENCABEZADOS,
               MIN(E.FECHA) AS DESDE,
               MAX(E.FECHA) AS HASTA,
               SUM(E.TOTAL) AS MONTO
        FROM dbo.BORR_NC_ENC E
        GROUP BY E.ID_EMPRESA, E.ESTADO
        ORDER BY E.ID_EMPRESA, E.ESTADO;

        SELECT N''10C_SALUD_BORRADOR'' AS SECCION,
               N''Detalles huerfanos'' AS VALIDACION,
               COUNT_BIG(*) AS CASOS
        FROM dbo.BORR_NC_DET D
        WHERE NOT EXISTS
              (SELECT 1
               FROM dbo.BORR_NC_ENC E
               WHERE E.ID_EMPRESA = D.ID_EMPRESA
                 AND E.ID_BORRADOR = D.ID_BORRADOR)
        UNION ALL
        SELECT N''10C_SALUD_BORRADOR'',
               N''Total encabezado distinto al detalle'',
               COUNT_BIG(*)
        FROM
        (
            SELECT E.ID_EMPRESA, E.ID_BORRADOR
            FROM dbo.BORR_NC_ENC E
            LEFT JOIN dbo.BORR_NC_DET D
                   ON D.ID_EMPRESA = E.ID_EMPRESA
                  AND D.ID_BORRADOR = E.ID_BORRADOR
            GROUP BY E.ID_EMPRESA, E.ID_BORRADOR, E.TOTAL
            HAVING ABS(E.TOTAL - ISNULL(SUM(D.IMPORTE), 0)) > 0.005
        ) X;';
END
ELSE
BEGIN
    SELECT N'10B_VOLUMEN_BORRADOR' AS SECCION,
           N'NO EXISTEN AMBAS TABLAS BORR_NC_ENC / BORR_NC_DET' AS RESULTADO;
END;
GO

/* 11 — Resumen compacto para decidir el siguiente paso -------------------- */
SELECT
    N'11_RESUMEN' AS SECCION,
    V.VALIDACION,
    V.ESPERADO,
    V.REAL,
    CASE WHEN V.REAL = V.ESPERADO THEN N'OK' ELSE N'REVISAR' END AS RESULTADO
FROM
(
    SELECT N'Objetos nuevos presentes' AS VALIDACION,
           CONVERT(bigint, 4) AS ESPERADO,
           CONVERT(bigint, COUNT(*)) AS REAL
    FROM sys.objects O
    JOIN sys.schemas S ON S.schema_id = O.schema_id
    WHERE S.name = N'dbo'
      AND O.name IN
          (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET', N'VW_BORR_NC_ACUMULADO')
      AND O.type IN ('U', 'V')

    UNION ALL

    SELECT N'Permisos BorradorNC', 5, COUNT_BIG(*)
    FROM dbo.Permiso
    WHERE Nombre LIKE N'Control.BorradorNC%'

    UNION ALL

    SELECT N'Entradas de menu BorradorNc', 2, COUNT_BIG(*)
    FROM dbo.Menu
    WHERE Controller = N'BorradorNc'

    UNION ALL

    SELECT N'Empresas C# presentes', 3, COUNT_BIG(*)
    FROM dbo.Empresa
    WHERE Empresa_Id IN (20210705001, 20210705003, 20210705004)
) V
ORDER BY V.VALIDACION;
GO

/* FIN: si todas las pestañas de resultados tienen SECCION, envíelas completas. */
