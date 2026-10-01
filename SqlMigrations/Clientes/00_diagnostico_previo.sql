/* CLIENTES CRM: consulta de solo lectura. Ejecutar en SSMS y compartir resultados. */
SET NOCOUNT ON;

SELECT N'ENTORNO' AS SECCION, DB_NAME() AS BASE_ACTUAL,
       CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
       CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL;

SELECT N'ROLES' AS SECCION, Rol_Id, Nombre
FROM dbo.Rol
WHERE Nombre LIKE N'%credit%' OR Nombre LIKE N'%crédit%'
   OR Nombre IN (N'VendedorK66', N'Telemarketing', N'Administrador', N'AdministradorK66')
ORDER BY Nombre;

SELECT N'MENU_REFERENCIA' AS SECCION, Menu_Id, Menu_Padre_Id, Titulo,
       Action, Controller, Orden, IconName, IsActive, PermisoId
FROM dbo.Menu
WHERE Controller IN (N'ReciboCaja', N'Cotizacion', N'Cliente')
ORDER BY Menu_Padre_Id, Orden;

SELECT N'EMPRESAS' AS SECCION, UE.Empresa_Id, E.Nombre,
       COUNT_BIG(*) AS ASIGNACIONES
FROM dbo.Usuario_Empresa UE
LEFT JOIN dbo.Empresa E ON E.Empresa_Id = UE.Empresa_Id
GROUP BY UE.Empresa_Id, E.Nombre
ORDER BY UE.Empresa_Id;

SELECT N'OBJETOS_CRM' AS SECCION, name, type_desc
FROM sys.objects
WHERE name LIKE N'CRM_CLIENTE%'
ORDER BY name;
