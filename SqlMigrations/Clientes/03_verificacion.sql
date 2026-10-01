/* CLIENTES CRM: verificación de solo lectura después de 01 y 02. */
USE [POS-SmartK66_DEV];
GO
SET NOCOUNT ON;
SELECT N'OBJETOS' AS SECCION,name,type_desc
FROM sys.objects WHERE name IN (N'CRM_CLIENTE',N'CRM_CLIENTE_EMPRESA',N'CRM_SOLICITUD',
                                 N'CRM_ARCHIVO',N'CRM_AUDITORIA') ORDER BY name;
SELECT N'PERMISOS' AS SECCION,Nombre,Descripcion,Modulo
FROM dbo.Permiso WHERE Nombre LIKE N'Control.Clientes.%' ORDER BY Nombre;
SELECT N'ROLES' AS SECCION,R.Nombre AS Rol,RP.Permiso_Id AS Permiso
FROM dbo.Rol_Permiso RP JOIN dbo.Rol R ON R.Rol_Id=RP.Rol_Id
WHERE RP.Permiso_Id LIKE N'Control.Clientes.%' ORDER BY R.Nombre,RP.Permiso_Id;
SELECT N'MENU' AS SECCION,Menu_Id,Menu_Padre_Id,Titulo,Action,Controller,PermisoId
FROM dbo.Menu WHERE Nombre LIKE N'Clientes%' ORDER BY Menu_Padre_Id,Orden;
GO
