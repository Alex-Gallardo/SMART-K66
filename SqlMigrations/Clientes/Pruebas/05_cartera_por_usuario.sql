/* Variante para POS-SmartK66 (PRUEBAS). No ejecutar el archivo homólogo de la carpeta superior. */
/* Ejecutar una vez después de 01 y 02. Seguro para repetir.
   Crea la propiedad de las fichas y el permiso global sin asignarlo a roles. */
USE [POS-SmartK66];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() <> N'POS-SmartK66'
    THROW 55200, 'Base incorrecta para Clientes CRM.', 1;
IF OBJECT_ID(N'dbo.CRM_CLIENTE_EMPRESA', N'U') IS NULL OR
   OBJECT_ID(N'dbo.CRM_SOLICITUD', N'U') IS NULL OR
   OBJECT_ID(N'dbo.CRM_AUDITORIA', N'U') IS NULL
    THROW 55201, 'Ejecute primero 01_estructura_clientes.sql.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.CRM_CLIENTE_USUARIO', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CRM_CLIENTE_USUARIO
        (
            CLIENTE_ID bigint NOT NULL,
            EMPRESA nvarchar(15) NOT NULL,
            USUARIO nvarchar(100) NOT NULL,
            ORIGEN nvarchar(20) NOT NULL,
            CREADO_EN datetime2(0) NOT NULL
                CONSTRAINT DF_CRM_CU_CREADO DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_CRM_CLIENTE_USUARIO PRIMARY KEY
                (CLIENTE_ID, EMPRESA, USUARIO),
            CONSTRAINT FK_CRM_CU_FICHA FOREIGN KEY (CLIENTE_ID, EMPRESA)
                REFERENCES dbo.CRM_CLIENTE_EMPRESA (CLIENTE_ID, EMPRESA),
            CONSTRAINT CK_CRM_CU_ORIGEN CHECK
                (ORIGEN IN (N'SOLICITUD', N'DIRECTA'))
        );
        CREATE INDEX IX_CRM_CU_USUARIO ON dbo.CRM_CLIENTE_USUARIO
            (USUARIO, EMPRESA, CLIENTE_ID);
    END;

    INSERT dbo.CRM_CLIENTE_USUARIO (CLIENTE_ID, EMPRESA, USUARIO, ORIGEN)
    SELECT DISTINCT S.CLIENTE_ID, S.EMPRESA, S.CREADO_POR, N'SOLICITUD'
    FROM dbo.CRM_SOLICITUD S
    JOIN dbo.CRM_CLIENTE_EMPRESA E
      ON E.CLIENTE_ID=S.CLIENTE_ID AND E.EMPRESA=S.EMPRESA
    WHERE S.ESTADO=N'APROBADA' AND S.CLIENTE_ID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.CRM_CLIENTE_USUARIO U WITH (UPDLOCK,HOLDLOCK)
                      WHERE U.CLIENTE_ID=S.CLIENTE_ID AND U.EMPRESA=S.EMPRESA
                        AND U.USUARIO=S.CREADO_POR);

    INSERT dbo.CRM_CLIENTE_USUARIO (CLIENTE_ID, EMPRESA, USUARIO, ORIGEN)
    SELECT DISTINCT A.ENTIDAD_ID, A.EMPRESA, A.USUARIO, N'DIRECTA'
    FROM dbo.CRM_AUDITORIA A
    JOIN dbo.CRM_CLIENTE_EMPRESA E
      ON E.CLIENTE_ID=A.ENTIDAD_ID AND E.EMPRESA=A.EMPRESA
    WHERE A.ENTIDAD=N'CLIENTE' AND A.ACCION=N'CREAR'
      AND A.ENTIDAD_ID IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.CRM_CLIENTE_USUARIO U WITH (UPDLOCK,HOLDLOCK)
                      WHERE U.CLIENTE_ID=A.ENTIDAD_ID AND U.EMPRESA=A.EMPRESA
                        AND U.USUARIO=A.USUARIO);

    IF EXISTS (SELECT 1 FROM dbo.Permiso
               WHERE Nombre=N'Control.Clientes.CarteraGlobal'
                 AND (Modulo<>N'Clientes' OR
                      Descripcion<>N'Ver la cartera CRM de todos los usuarios'))
        THROW 55202, 'El permiso CarteraGlobal existente no coincide.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Permiso WITH (UPDLOCK,HOLDLOCK)
                   WHERE Nombre=N'Control.Clientes.CarteraGlobal')
        INSERT dbo.Permiso (Nombre,Descripcion,Modulo)
        VALUES (N'Control.Clientes.CarteraGlobal',
                N'Ver la cartera CRM de todos los usuarios',N'Clientes');

    IF NOT EXISTS (SELECT 1 FROM dbo.Menu
                   WHERE Nombre=N'ClientesCartera' AND Controller=N'ClientesCrm'
                     AND Action=N'Clientes')
        THROW 55203, 'No se encontró la entrada de menú Cartera CRM.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Menu
               WHERE Nombre=N'ClientesCartera'
                 AND PermisoId NOT IN (N'Control.Clientes.Dashboard',N'Control.Clientes.Modulo'))
        THROW 55204, 'La entrada de Cartera CRM tiene un permiso inesperado.', 1;
    UPDATE dbo.Menu SET PermisoId=N'Control.Clientes.Modulo'
    WHERE Nombre=N'ClientesCartera' AND PermisoId<>N'Control.Clientes.Modulo';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT N'RESUMEN' AS SECCION,
       (SELECT COUNT(*) FROM dbo.CRM_CLIENTE_USUARIO) AS VINCULOS_USUARIO_CLIENTE,
       (SELECT COUNT(*) FROM dbo.CRM_CLIENTE_EMPRESA E WHERE NOT EXISTS
           (SELECT 1 FROM dbo.CRM_CLIENTE_USUARIO U
            WHERE U.CLIENTE_ID=E.CLIENTE_ID AND U.EMPRESA=E.EMPRESA)) AS FICHAS_SIN_PROPIETARIO,
       (SELECT COUNT(*) FROM dbo.Permiso
        WHERE Nombre=N'Control.Clientes.CarteraGlobal') AS PERMISO_GLOBAL_CREADO;
GO
