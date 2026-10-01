/* Clientes CRM: diagnóstico de POS-SmartK66 (PRUEBAS). Solo lectura.
   No consulta usuarios ni roles y no modifica datos. */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
IF DB_NAME() <> N'POS-SmartK66'
    THROW 55300, 'Base incorrecta: seleccione POS-SmartK66.', 1;

SELECT N'ENTORNO' AS SECCION, DB_NAME() AS BASE_ACTUAL,
       CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR;

DECLARE @Estado TABLE (TIPO nvarchar(20), REQUISITO nvarchar(100), ESTADO nvarchar(10));
INSERT @Estado (TIPO, REQUISITO, ESTADO)
SELECT N'ESTRUCTURA', V.Nombre,
       CASE WHEN OBJECT_ID(N'dbo.' + V.Nombre, N'U') IS NULL THEN N'FALTA' ELSE N'OK' END
FROM (VALUES (N'CRM_CLIENTE'), (N'CRM_CLIENTE_EMPRESA'), (N'CRM_SOLICITUD'),
             (N'CRM_ARCHIVO'), (N'CRM_AUDITORIA')) V(Nombre);

INSERT @Estado (TIPO, REQUISITO, ESTADO)
SELECT N'PERMISO', V.Nombre,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.Permiso P WHERE P.Nombre=V.Nombre)
            THEN N'OK' ELSE N'FALTA' END
FROM (VALUES (N'Control.Clientes.Modulo'), (N'Control.Clientes.Ver'),
             (N'Control.Clientes.Crear'), (N'Control.Clientes.Dashboard'),
             (N'Control.Clientes.Administrar')) V(Nombre);

INSERT @Estado (TIPO, REQUISITO, ESTADO)
SELECT N'MENU', V.Nombre,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.Menu M WHERE M.Nombre=V.Nombre
                          AND (V.Nombre=N'ClientesCrm' OR M.Controller=N'ClientesCrm'))
            THEN N'OK' ELSE N'FALTA' END
FROM (VALUES (N'ClientesCrm'), (N'ClientesSolicitudes'),
             (N'ClientesDashboard'), (N'ClientesCartera')) V(Nombre);

SELECT TIPO, REQUISITO, ESTADO FROM @Estado ORDER BY TIPO, REQUISITO;
SELECT N'RESUMEN' AS SECCION,
       CASE WHEN EXISTS (SELECT 1 FROM @Estado WHERE TIPO=N'ESTRUCTURA' AND ESTADO=N'FALTA')
            THEN N'Ejecutar 01 y 02 de Pruebas antes de 05'
            WHEN EXISTS (SELECT 1 FROM @Estado WHERE ESTADO=N'FALTA')
            THEN N'Ejecutar 02 de Pruebas antes de 05'
            ELSE N'Listo para ejecutar 05 de Pruebas' END AS SIGUIENTE_PASO,
       CASE WHEN OBJECT_ID(N'dbo.CRM_CLIENTE_USUARIO', N'U') IS NULL
            THEN N'PENDIENTE' ELSE N'INSTALADA' END AS MIGRACION_05,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.Permiso
                         WHERE Nombre=N'Control.Clientes.CarteraGlobal')
            THEN N'CREADO' ELSE N'PENDIENTE' END AS PERMISO_GLOBAL;
GO
