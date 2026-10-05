/* Ejecutar después de 08. Solo lectura. */
USE [POS-SmartK66];
GO
SELECT N'CONTRATO' AS SECCION, DB_NAME() AS BASE_ACTUAL,
  CASE WHEN COL_LENGTH(N'dbo.CRM_SOLICITUD',N'TIPO_SOLICITUD') IS NOT NULL
        AND COL_LENGTH(N'dbo.CRM_SOLICITUD',N'ORIGEN_CLIENTE_ID') IS NOT NULL
        AND COL_LENGTH(N'dbo.CRM_SOLICITUD',N'ORIGEN_VERSION') IS NOT NULL
        AND COL_LENGTH(N'dbo.CRM_SOLICITUD',N'ORIGEN_CODIGO_SAP') IS NOT NULL
       THEN N'OK' ELSE N'FALTA' END AS COLUMNAS,
  CASE WHEN EXISTS (SELECT 1 FROM dbo.Menu WHERE Nombre=N'ClientesDashboard'
                    AND Titulo=N'Control Créditos' AND Action=N'ControlCreditos'
                    AND PermisoId=N'Control.Clientes.ControlCreditos')
        AND EXISTS (SELECT 1 FROM dbo.Menu WHERE Nombre=N'ClientesCartera'
                    AND Titulo=N'Actualización de Cliente' AND Action=N'Actualizacion'
                    AND PermisoId=N'Control.Clientes.Modulo')
       THEN N'OK' ELSE N'FALTA' END AS MENU,
  CASE WHEN EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_CRM_SOL_TIPO'
                    AND parent_object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD'))
        AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_CRM_SOL_ORIGEN'
                    AND parent_object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD'))
        AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CRM_SOL_ORIGEN'
                    AND parent_object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD'))
        AND EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_CRM_SOL_ACTUALIZACION'
                    AND object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD'))
       THEN N'OK' ELSE N'FALTA' END AS RESTRICCIONES,
  (SELECT COUNT(*) FROM dbo.Permiso WHERE Nombre IN
      (N'Control.Clientes.Actualizar',N'Control.Clientes.ActualizacionGlobal',
       N'Control.Clientes.ControlCreditos')) AS PERMISOS_NUEVOS;
SELECT TIPO_SOLICITUD,ESTADO,COUNT(*) AS TOTAL
FROM dbo.CRM_SOLICITUD GROUP BY TIPO_SOLICITUD,ESTADO
ORDER BY TIPO_SOLICITUD,ESTADO;
SELECT N'ORIGEN_INCOMPLETO' AS SECCION,COUNT(*) AS TOTAL
FROM dbo.CRM_SOLICITUD WHERE TIPO_SOLICITUD=N'ACTUALIZACION'
  AND ORIGEN_CLIENTE_ID IS NULL AND NULLIF(ORIGEN_CODIGO_SAP,N'') IS NULL;
GO
