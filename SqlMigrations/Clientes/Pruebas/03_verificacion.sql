/* Variante para POS-SmartK66 (PRUEBAS). No crea usuarios ni asigna permisos a roles. */
/* CLIENTES CRM: verificación de solo lectura después de 01 y 02. */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'POS-SmartK66'
    THROW 55303, 'Base incorrecta: seleccione POS-SmartK66.', 1;
SELECT N'OBJETOS' AS SECCION,name,type_desc
FROM sys.objects WHERE name IN (N'CRM_CLIENTE',N'CRM_CLIENTE_EMPRESA',N'CRM_SOLICITUD',
                                 N'CRM_ARCHIVO',N'CRM_AUDITORIA') ORDER BY name;
SELECT N'PERMISOS' AS SECCION,Nombre,Descripcion,Modulo
FROM dbo.Permiso WHERE Nombre LIKE N'Control.Clientes.%' ORDER BY Nombre;
SELECT N'MENU' AS SECCION,Menu_Id,Menu_Padre_Id,Titulo,Action,Controller,PermisoId
FROM dbo.Menu WHERE Nombre LIKE N'Clientes%' ORDER BY Menu_Padre_Id,Orden;
GO
