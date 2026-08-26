/* COTIZACIONES — DIAGNÓSTICO SQL DE SOLO LECTURA
   Ejecutar completo en SSMS y enviar todos los resultados. */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;

SELECT N'ENTORNO' AS SECCION,
       CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
       DB_NAME() AS BASE_ACTUAL,
       CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL,
       CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) AS ACTUALIZABLE;

SELECT N'OBJETOS_COTIZACIONES' AS SECCION, S.name AS ESQUEMA, O.name AS OBJETO,
       O.type_desc AS TIPO
FROM sys.objects O
JOIN sys.schemas S ON S.schema_id = O.schema_id
WHERE O.name IN (N'COT_SERIES', N'COT_ENC', N'COT_DET')
ORDER BY O.name;

SELECT N'USUARIO_EMPRESA_COLUMNAS' AS SECCION, C.column_id, C.name AS COLUMNA,
       T.name AS TIPO, C.max_length, C.precision, C.scale, C.is_nullable
FROM sys.columns C
JOIN sys.types T ON T.user_type_id = C.user_type_id
WHERE C.object_id = OBJECT_ID(N'dbo.Usuario_Empresa')
ORDER BY C.column_id;

SELECT N'EMPRESAS_ASIGNADAS' AS SECCION, UE.Empresa_Id, E.Nombre,
       COUNT_BIG(*) AS ASIGNACIONES,
       COUNT(DISTINCT NULLIF(LTRIM(RTRIM(UE.Codigo)), N'')) AS CODIGOS_DISTINTOS
FROM dbo.Usuario_Empresa UE
LEFT JOIN dbo.Empresa E ON E.Empresa_Id = UE.Empresa_Id
GROUP BY UE.Empresa_Id, E.Nombre
ORDER BY UE.Empresa_Id;

SELECT N'MENU_REFERENCIA' AS SECCION, Menu_Id, Menu_Padre_Id, Nombre, Titulo,
       Action, Controller, Orden, IconName, IsActive, PermisoId
FROM dbo.Menu
WHERE Controller IN (N'ReciboCaja', N'BorradorNc', N'Cotizacion')
ORDER BY Menu_Padre_Id, Orden, Menu_Id;

SELECT N'ROLES_REFERENCIA' AS SECCION, Rol_Id, Nombre
FROM dbo.Rol
WHERE Nombre IN (N'Administrador', N'AdministradorK66', N'Telemarketing', N'VendedorK66')
ORDER BY Nombre;
GO
