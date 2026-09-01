/* =============================================================================
   BORRADORES NC — DOCUMENTACION OPCIONAL
   Destino autorizado: POS-SmartK66

   Crea dbo.BORR_NC_ADJUNTO para archivos privados y enlaces asociados a un
   borrador. Un borrador sin filas en esta tabla sigue siendo completamente
   valido.

   Garantias
   ---------
   - No elimina ni actualiza datos existentes.
   - No toca POS-SmartK66_DEV.
   - Es transaccional y reejecutable.
   - Si encuentra una tabla previa incompatible, se detiene sin repararla.
   - Los archivos se almacenan en SQL y solo se obtienen mediante el modulo.

   Ejecucion
   ---------
   1. Abra una ventana NUEVA de SSMS.
   2. Ejecute el archivo COMPLETO, sin seleccionar bloques parciales.
   3. Confirme BASE_ACTUAL = POS-SmartK66 y RESULTADO = OK.
   4. Comparta las salidas 04A a 04D y la pestana Messages.
   ============================================================================= */

USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    THROW 51400,
          'SEGURIDAD: esta migracion solo esta autorizada para POS-SmartK66.',
          1;
END;

IF CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) <> N'READ_WRITE'
BEGIN
    THROW 51401, 'La base autorizada no esta disponible para escritura.', 1;
END;

IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
BEGIN
    THROW 51402,
          'No existe dbo.BORR_NC_ENC. Ejecute primero la estructura base de Borradores NC.',
          1;
END;

IF OBJECT_ID(N'dbo.BORR_NC_ADJUNTO') IS NOT NULL
   AND OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NULL
BEGIN
    THROW 51403,
          'Existe dbo.BORR_NC_ADJUNTO pero no es una tabla. No se realizo ningun cambio.',
          1;
END;
GO

SELECT
    N'04A_ENTORNO' AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME() AS BASE_ACTUAL,
    CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL,
    CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) AS ACTUALIZABLE,
    SYSDATETIME() AS FECHA_EJECUCION;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BORR_NC_ADJUNTO
        (
            ADJUNTO_ID   bigint IDENTITY(1,1) NOT NULL,
            ID_BORRADOR  nvarchar(20) NOT NULL,
            ID_EMPRESA   nvarchar(15) NOT NULL,
            TIPO         varchar(10) NOT NULL,
            NOMBRE       nvarchar(255) NOT NULL,
            EXTENSION    nvarchar(10) NULL,
            CONTENT_TYPE nvarchar(150) NULL,
            TAMANO       bigint NOT NULL
                CONSTRAINT DF_BNA_TAMANO DEFAULT (0),
            CONTENIDO    varbinary(max) NULL,
            URL          nvarchar(2048) NULL,
            HASH_SHA256  binary(32) NOT NULL,
            ORDEN        smallint NOT NULL,
            ID_USR       nvarchar(50) NOT NULL,
            REGISTRO     datetime2(3) NOT NULL
                CONSTRAINT DF_BNA_REGISTRO DEFAULT (SYSDATETIME()),

            CONSTRAINT PK_BORR_NC_ADJUNTO
                PRIMARY KEY CLUSTERED (ADJUNTO_ID),

            CONSTRAINT FK_BORR_NC_ADJUNTO_ENC
                FOREIGN KEY (ID_EMPRESA, ID_BORRADOR)
                REFERENCES dbo.BORR_NC_ENC (ID_EMPRESA, ID_BORRADOR)
                ON DELETE CASCADE,

            CONSTRAINT CK_BNA_TIPO
                CHECK (TIPO IN ('ARCHIVO', 'ENLACE')),

            CONSTRAINT CK_BNA_NOMBRE
                CHECK (LEN(LTRIM(RTRIM(NOMBRE))) > 0),

            CONSTRAINT CK_BNA_ORDEN
                CHECK (ORDEN > 0),

            CONSTRAINT CK_BNA_CONTENIDO
                CHECK
                (
                    (TIPO = 'ARCHIVO'
                     AND CONTENIDO IS NOT NULL
                     AND URL IS NULL
                     AND TAMANO BETWEEN 1 AND 10485760
                     AND DATALENGTH(CONTENIDO) = TAMANO
                     AND EXTENSION IS NOT NULL
                     AND CONTENT_TYPE IS NOT NULL)
                    OR
                    (TIPO = 'ENLACE'
                     AND CONTENIDO IS NULL
                     AND URL IS NOT NULL
                     AND (URL LIKE 'http://%' OR URL LIKE 'https://%')
                     AND TAMANO = 0
                     AND EXTENSION IS NULL
                     AND CONTENT_TYPE IS NULL)
                ),

            CONSTRAINT UQ_BORR_NC_ADJUNTO_HASH
                UNIQUE NONCLUSTERED
                    (ID_EMPRESA, ID_BORRADOR, TIPO, HASH_SHA256)
        );

        CREATE NONCLUSTERED INDEX IX_BORR_NC_ADJUNTO_BORRADOR_ORDEN
            ON dbo.BORR_NC_ADJUNTO (ID_EMPRESA, ID_BORRADOR, ORDEN)
            INCLUDE (ADJUNTO_ID, TIPO, NOMBRE, EXTENSION, CONTENT_TYPE,
                     TAMANO, URL, ID_USR, REGISTRO);
    END;

    /* Una tabla preexistente solo se acepta si coincide con el contrato. */
    DECLARE @ColumnasEsperadas TABLE
    (
        NOMBRE sysname NOT NULL PRIMARY KEY,
        TIPO sysname NOT NULL,
        LARGO smallint NOT NULL,
        ES_NULLABLE bit NOT NULL,
        ES_IDENTIDAD bit NOT NULL
    );

    INSERT INTO @ColumnasEsperadas (NOMBRE, TIPO, LARGO, ES_NULLABLE, ES_IDENTIDAD)
    VALUES
        (N'ADJUNTO_ID',   N'bigint',         8, 0, 1),
        (N'ID_BORRADOR',  N'nvarchar',      40, 0, 0),
        (N'ID_EMPRESA',   N'nvarchar',      30, 0, 0),
        (N'TIPO',         N'varchar',       10, 0, 0),
        (N'NOMBRE',       N'nvarchar',     510, 0, 0),
        (N'EXTENSION',    N'nvarchar',      20, 1, 0),
        (N'CONTENT_TYPE', N'nvarchar',     300, 1, 0),
        (N'TAMANO',       N'bigint',         8, 0, 0),
        (N'CONTENIDO',    N'varbinary',      -1, 1, 0),
        (N'URL',          N'nvarchar',    4096, 1, 0),
        (N'HASH_SHA256',  N'binary',        32, 0, 0),
        (N'ORDEN',        N'smallint',       2, 0, 0),
        (N'ID_USR',       N'nvarchar',     100, 0, 0),
        (N'REGISTRO',     N'datetime2',      7, 0, 0);

    IF EXISTS
    (
        SELECT 1
        FROM @ColumnasEsperadas E
        LEFT JOIN sys.columns C
          ON C.object_id = OBJECT_ID(N'dbo.BORR_NC_ADJUNTO')
         AND C.name COLLATE DATABASE_DEFAULT = E.NOMBRE
        LEFT JOIN sys.types T
          ON T.user_type_id = C.user_type_id
        WHERE C.column_id IS NULL
           OR T.name COLLATE DATABASE_DEFAULT <> E.TIPO
           OR C.max_length <> E.LARGO
           OR C.is_nullable <> E.ES_NULLABLE
           OR C.is_identity <> E.ES_IDENTIDAD
    )
    BEGIN
        THROW 51404,
              'dbo.BORR_NC_ADJUNTO existe con columnas incompatibles. Se revirtio la operacion.',
              1;
    END;

    IF EXISTS
    (
        SELECT N.NOMBRE
        FROM (VALUES
            (N'PK_BORR_NC_ADJUNTO'),
            (N'FK_BORR_NC_ADJUNTO_ENC'),
            (N'CK_BNA_TIPO'),
            (N'CK_BNA_NOMBRE'),
            (N'CK_BNA_ORDEN'),
            (N'CK_BNA_CONTENIDO'),
            (N'UQ_BORR_NC_ADJUNTO_HASH'),
            (N'DF_BNA_TAMANO'),
            (N'DF_BNA_REGISTRO')
        ) N(NOMBRE)
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM sys.objects O
            WHERE O.parent_object_id = OBJECT_ID(N'dbo.BORR_NC_ADJUNTO')
              AND O.name COLLATE DATABASE_DEFAULT = N.NOMBRE
        )
    )
    BEGIN
        THROW 51405,
              'dbo.BORR_NC_ADJUNTO existe sin todas las restricciones esperadas. Se revirtio la operacion.',
              1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.BORR_NC_ADJUNTO')
          AND name = N'IX_BORR_NC_ADJUNTO_BORRADOR_ORDEN'
          AND is_disabled = 0
    )
    BEGIN
        THROW 51406,
              'Falta el indice de consulta de adjuntos. Se revirtio la operacion.',
              1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT
    N'04B_OBJETO' AS SECCION,
    S.name AS ESQUEMA,
    O.name AS OBJETO,
    O.type_desc AS TIPO,
    O.create_date AS FECHA_CREACION,
    O.modify_date AS FECHA_MODIFICACION
FROM sys.objects O
JOIN sys.schemas S ON S.schema_id = O.schema_id
WHERE O.object_id = OBJECT_ID(N'dbo.BORR_NC_ADJUNTO');

SELECT
    N'04C_RESTRICCIONES' AS SECCION,
    O.name AS RESTRICCION,
    O.type_desc AS TIPO
FROM sys.objects O
WHERE O.parent_object_id = OBJECT_ID(N'dbo.BORR_NC_ADJUNTO')
ORDER BY O.type_desc, O.name;

SELECT
    N'04D_RESUMEN' AS SECCION,
    N'Tabla de documentacion opcional' AS VALIDACION,
    1 AS ESPERADO,
    CASE WHEN OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NOT NULL THEN 1 ELSE 0 END AS REAL,
    CASE WHEN OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NOT NULL THEN N'OK' ELSE N'REVISAR' END AS RESULTADO;
GO

PRINT N'OK: estructura de adjuntos validada en POS-SmartK66.';
GO
