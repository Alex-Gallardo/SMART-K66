/* Ejecutar en POS-SmartK66 antes de publicar esta versión de la aplicación.
   No modifica SAP. Seguro para repetir. */
USE [POS-SmartK66];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() <> N'POS-SmartK66'
    THROW 55800, 'Base incorrecta para la actualización CRM.', 1;
IF OBJECT_ID(N'dbo.CRM_SOLICITUD', N'U') IS NULL OR
   OBJECT_ID(N'dbo.CRM_CLIENTE_USUARIO', N'U') IS NULL
    THROW 55801, 'Falta la estructura CRM previa.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.CRM_SOLICITUD', N'TIPO_SOLICITUD') IS NULL
        ALTER TABLE dbo.CRM_SOLICITUD ADD TIPO_SOLICITUD nvarchar(15) NOT NULL
            CONSTRAINT DF_CRM_SOL_TIPO DEFAULT (N'ALTA') WITH VALUES;
    IF COL_LENGTH(N'dbo.CRM_SOLICITUD', N'ORIGEN_CLIENTE_ID') IS NULL
        ALTER TABLE dbo.CRM_SOLICITUD ADD ORIGEN_CLIENTE_ID bigint NULL;
    IF COL_LENGTH(N'dbo.CRM_SOLICITUD', N'ORIGEN_VERSION') IS NULL
        ALTER TABLE dbo.CRM_SOLICITUD ADD ORIGEN_VERSION int NULL;
    IF COL_LENGTH(N'dbo.CRM_SOLICITUD', N'ORIGEN_CODIGO_SAP') IS NULL
        ALTER TABLE dbo.CRM_SOLICITUD ADD ORIGEN_CODIGO_SAP nvarchar(50) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD')
                     AND name=N'CK_CRM_SOL_TIPO')
        EXEC(N'ALTER TABLE dbo.CRM_SOLICITUD ADD CONSTRAINT CK_CRM_SOL_TIPO
            CHECK (TIPO_SOLICITUD IN (N''ALTA'', N''ACTUALIZACION''))');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD')
                     AND name=N'CK_CRM_SOL_ORIGEN')
        EXEC(N'ALTER TABLE dbo.CRM_SOLICITUD ADD CONSTRAINT CK_CRM_SOL_ORIGEN CHECK
            ((TIPO_SOLICITUD=N''ALTA'' AND ORIGEN_CLIENTE_ID IS NULL AND ORIGEN_VERSION IS NULL
              AND ORIGEN_CODIGO_SAP IS NULL) OR
             (TIPO_SOLICITUD=N''ACTUALIZACION'' AND
              ((ORIGEN_CLIENTE_ID IS NOT NULL AND ORIGEN_VERSION IS NOT NULL) OR
               (ORIGEN_CLIENTE_ID IS NULL AND ORIGEN_VERSION IS NULL
                AND NULLIF(ORIGEN_CODIGO_SAP,N'''') IS NOT NULL)))))');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE parent_object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD')
                     AND name=N'FK_CRM_SOL_ORIGEN')
        EXEC(N'ALTER TABLE dbo.CRM_SOLICITUD ADD CONSTRAINT FK_CRM_SOL_ORIGEN
            FOREIGN KEY (ORIGEN_CLIENTE_ID) REFERENCES dbo.CRM_CLIENTE(ID)');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id=OBJECT_ID(N'dbo.CRM_SOLICITUD')
                     AND name=N'IX_CRM_SOL_ACTUALIZACION')
        EXEC(N'CREATE INDEX IX_CRM_SOL_ACTUALIZACION ON dbo.CRM_SOLICITUD
            (EMPRESA,TIPO_SOLICITUD,ORIGEN_CLIENTE_ID,ESTADO)');

    DECLARE @Permisos TABLE (Nombre nvarchar(100) PRIMARY KEY, Descripcion nvarchar(500));
    INSERT @Permisos VALUES
      (N'Control.Clientes.Actualizar',N'Crear y editar solicitudes de actualización propias'),
      (N'Control.Clientes.ActualizacionGlobal',N'Ver clientes de todos los usuarios en Actualización de Cliente'),
      (N'Control.Clientes.ControlCreditos',N'Consultar y resolver altas y actualizaciones en Control Créditos');
    IF EXISTS (SELECT 1 FROM @Permisos E JOIN dbo.Permiso P ON P.Nombre=E.Nombre
               WHERE P.Modulo<>N'Clientes' OR P.Descripcion<>E.Descripcion)
        THROW 55802, 'Un permiso nuevo existe con otro contrato.', 1;
    INSERT dbo.Permiso (Nombre,Descripcion,Modulo)
    SELECT E.Nombre,E.Descripcion,N'Clientes' FROM @Permisos E
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Permiso P WITH (UPDLOCK,HOLDLOCK)
                      WHERE P.Nombre=E.Nombre);

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE Nombre=N'ClientesDashboard'
                   AND Controller=N'ClientesCrm' AND Action IN (N'Dashboard',N'ControlCreditos'))
        THROW 55803, 'No se encontró el menú anterior de Créditos.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE Nombre=N'ClientesCartera'
                   AND Controller=N'ClientesCrm' AND Action IN (N'Clientes',N'Actualizacion'))
        THROW 55804, 'No se encontró el menú anterior de clientes.', 1;
    UPDATE dbo.Menu SET Titulo=N'Control Créditos',Action=N'ControlCreditos',
        PermisoId=N'Control.Clientes.ControlCreditos'
    WHERE Nombre=N'ClientesDashboard' AND Controller=N'ClientesCrm';
    UPDATE dbo.Menu SET Titulo=N'Actualización de Cliente',Action=N'Actualizacion',
        PermisoId=N'Control.Clientes.Modulo'
    WHERE Nombre=N'ClientesCartera' AND Controller=N'ClientesCrm';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
