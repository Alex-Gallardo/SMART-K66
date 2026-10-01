/* Variante para POS-SmartK66 (PRUEBAS). No crea usuarios ni asigna permisos a roles. */
/* CLIENTES CRM: comprueba las columnas e índices que usa la aplicación.
   Solo lectura. No crea, modifica ni elimina registros. */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'POS-SmartK66'
    THROW 55304, 'Base incorrecta: seleccione POS-SmartK66.', 1;

DECLARE @Columnas TABLE (Tabla sysname, Columna sysname, Tipo sysname,
                        PRIMARY KEY (Tabla, Columna));
INSERT @Columnas VALUES
 (N'CRM_CLIENTE',N'ID',N'bigint'),
 (N'CRM_CLIENTE',N'NIT_CLAVE',N'nvarchar'),
 (N'CRM_CLIENTE',N'RAZON_SOCIAL',N'nvarchar'),
 (N'CRM_CLIENTE',N'NOMBRE_COMERCIAL',N'nvarchar'),
 (N'CRM_CLIENTE',N'VERSION',N'int'),
 (N'CRM_CLIENTE',N'ACTUALIZADO_POR',N'nvarchar'),
 (N'CRM_CLIENTE',N'ACTUALIZADO_EN',N'datetime2'),
 (N'CRM_CLIENTE_EMPRESA',N'CLIENTE_ID',N'bigint'),
 (N'CRM_CLIENTE_EMPRESA',N'EMPRESA',N'nvarchar'),
 (N'CRM_CLIENTE_EMPRESA',N'CODIGO_SAP',N'nvarchar'),
 (N'CRM_CLIENTE_EMPRESA',N'FICHA_JSON',N'nvarchar'),
 (N'CRM_CLIENTE_EMPRESA',N'ACTIVO',N'bit'),
 (N'CRM_CLIENTE_EMPRESA',N'COMPRA_ACTUAL',N'bit'),
 (N'CRM_CLIENTE_EMPRESA',N'MONEDA',N'nvarchar'),
 (N'CRM_CLIENTE_EMPRESA',N'VENTA_12M',N'decimal'),
 (N'CRM_CLIENTE_EMPRESA',N'POTENCIAL_ANUAL',N'decimal'),
 (N'CRM_CLIENTE_EMPRESA',N'OBJETIVO_12M',N'decimal'),
 (N'CRM_CLIENTE_EMPRESA',N'VERSION',N'int'),
 (N'CRM_CLIENTE_EMPRESA',N'ACTUALIZADO_POR',N'nvarchar'),
 (N'CRM_CLIENTE_EMPRESA',N'ACTUALIZADO_EN',N'datetime2'),
 (N'CRM_SOLICITUD',N'ID',N'bigint'),
 (N'CRM_SOLICITUD',N'CLIENTE_ID',N'bigint'),
 (N'CRM_SOLICITUD',N'EMPRESA',N'nvarchar'),
 (N'CRM_SOLICITUD',N'CODIGO_OPERADOR',N'nvarchar'),
 (N'CRM_SOLICITUD',N'AGENTE',N'nvarchar'),
 (N'CRM_SOLICITUD',N'RAZON_SOCIAL',N'nvarchar'),
 (N'CRM_SOLICITUD',N'NIT_CLAVE',N'nvarchar'),
 (N'CRM_SOLICITUD',N'ESTADO',N'nvarchar'),
 (N'CRM_SOLICITUD',N'FICHA_JSON',N'nvarchar'),
 (N'CRM_SOLICITUD',N'COMPRA_ACTUAL',N'bit'),
 (N'CRM_SOLICITUD',N'MONEDA',N'nvarchar'),
 (N'CRM_SOLICITUD',N'VENTA_12M',N'decimal'),
 (N'CRM_SOLICITUD',N'POTENCIAL_ANUAL',N'decimal'),
 (N'CRM_SOLICITUD',N'OBJETIVO_12M',N'decimal'),
 (N'CRM_SOLICITUD',N'CREADO_POR',N'nvarchar'),
 (N'CRM_SOLICITUD',N'CREADO_EN',N'datetime2'),
 (N'CRM_SOLICITUD',N'ENVIADO_EN',N'datetime2'),
 (N'CRM_SOLICITUD',N'RESUELTO_EN',N'datetime2'),
 (N'CRM_SOLICITUD',N'RESUELTO_POR',N'nvarchar'),
 (N'CRM_SOLICITUD',N'MOTIVO_RECHAZO',N'nvarchar'),
 (N'CRM_SOLICITUD',N'VERSION',N'int'),
 (N'CRM_ARCHIVO',N'ID',N'bigint'),
 (N'CRM_ARCHIVO',N'SOLICITUD_ID',N'bigint'),
 (N'CRM_ARCHIVO',N'CLIENTE_ID',N'bigint'),
 (N'CRM_ARCHIVO',N'EMPRESA',N'nvarchar'),
 (N'CRM_ARCHIVO',N'TIPO',N'nvarchar'),
 (N'CRM_ARCHIVO',N'NOMBRE',N'nvarchar'),
 (N'CRM_ARCHIVO',N'CONTENT_TYPE',N'nvarchar'),
 (N'CRM_ARCHIVO',N'TAMANO',N'int'),
 (N'CRM_ARCHIVO',N'CONTENIDO',N'varbinary'),
 (N'CRM_ARCHIVO',N'CREADO_EN',N'datetime2'),
 (N'CRM_AUDITORIA',N'ID',N'bigint'),
 (N'CRM_AUDITORIA',N'ENTIDAD',N'nvarchar'),
 (N'CRM_AUDITORIA',N'ENTIDAD_ID',N'bigint'),
 (N'CRM_AUDITORIA',N'EMPRESA',N'nvarchar'),
 (N'CRM_AUDITORIA',N'USUARIO',N'nvarchar'),
 (N'CRM_AUDITORIA',N'ACCION',N'nvarchar'),
 (N'CRM_AUDITORIA',N'DETALLE',N'nvarchar'),
 (N'CRM_AUDITORIA',N'ANTES_JSON',N'nvarchar'),
 (N'CRM_AUDITORIA',N'DESPUES_JSON',N'nvarchar'),
 (N'CRM_AUDITORIA',N'IP',N'nvarchar'),
 (N'CRM_AUDITORIA',N'FECHA',N'datetime2');

DECLARE @Indices TABLE (Tabla sysname, Indice sysname,
                       PRIMARY KEY (Tabla, Indice));
INSERT @Indices VALUES
 (N'CRM_CLIENTE',N'UX_CRM_CLIENTE_NIT'),
 (N'CRM_CLIENTE_EMPRESA',N'UX_CRM_CE_CODIGO_SAP'),
 (N'CRM_SOLICITUD',N'IX_CRM_SOL_ESTADO'),
 (N'CRM_SOLICITUD',N'IX_CRM_SOL_CREADOR'),
 (N'CRM_SOLICITUD',N'IX_CRM_SOL_NIT'),
 (N'CRM_ARCHIVO',N'IX_CRM_ARCHIVO_SOL'),
 (N'CRM_ARCHIVO',N'IX_CRM_ARCHIVO_CLI'),
 (N'CRM_AUDITORIA',N'IX_CRM_AUD_ENTIDAD');

DECLARE @Errores TABLE (Objeto nvarchar(200), Problema nvarchar(200));
INSERT @Errores
SELECT E.Tabla + N'.' + E.Columna,
       CASE WHEN T.object_id IS NULL THEN N'Falta tabla'
            WHEN C.column_id IS NULL THEN N'Falta columna'
            ELSE N'Tipo esperado: ' + E.Tipo + N'; real: ' + TY.name END
FROM @Columnas E
LEFT JOIN sys.tables T ON T.name=E.Tabla AND T.schema_id=SCHEMA_ID(N'dbo')
LEFT JOIN sys.columns C ON C.object_id=T.object_id AND C.name=E.Columna
LEFT JOIN sys.types TY ON TY.user_type_id=C.user_type_id
WHERE T.object_id IS NULL OR C.column_id IS NULL OR TY.name<>E.Tipo;

INSERT @Errores
SELECT E.Tabla + N'.' + E.Indice, N'Falta índice'
FROM @Indices E
LEFT JOIN sys.tables T ON T.name=E.Tabla AND T.schema_id=SCHEMA_ID(N'dbo')
LEFT JOIN sys.indexes I ON I.object_id=T.object_id AND I.name=E.Indice
WHERE I.index_id IS NULL;

SELECT N'RESUMEN' AS SECCION, DB_NAME() AS BASE_ACTUAL,
       (SELECT COUNT(*) FROM @Columnas) AS COLUMNAS_ESPERADAS,
       (SELECT COUNT(*) FROM @Indices) AS INDICES_ESPERADOS,
       (SELECT COUNT(*) FROM @Errores) AS ERRORES,
       CASE WHEN EXISTS (SELECT 1 FROM @Errores) THEN N'REVISAR' ELSE N'OK' END AS ESTADO;
SELECT N'DETALLE' AS SECCION, Objeto, Problema FROM @Errores ORDER BY Objeto;
GO
