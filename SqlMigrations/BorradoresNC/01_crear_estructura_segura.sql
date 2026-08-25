/* =============================================================================
   BORRADORES NC — CREACION SEGURA DE ESTRUCTURA
   Destino autorizado: POS-SmartK66 (PRUEBAS)

   Alcance
   -------
   - Crea tres tablas nuevas: BORR_NC_SERIES, BORR_NC_ENC y BORR_NC_DET.
   - Crea la vista VW_BORR_NC_ACUMULADO.
   - Crea restricciones e indices propios del modulo.
   - Registra las series web BWB-, BWF- y BWG- si aun no existen.

   Garantias
   ---------
   - No elimina objetos.
   - No modifica Permiso, Menu, Rol_Permiso ni otras tablas existentes.
   - Usa una transaccion y revierte todo ante cualquier error.
   - Es reejecutable cuando los cuatro objetos ya existen y son compatibles.
   - Se detiene ante objetos parciales o incompatibles; nunca intenta repararlos
     silenciosamente.

   Ejecucion
   ---------
   1. Abra una ventana NUEVA en SSMS y conectese al servidor de pruebas.
   2. Ejecute el archivo COMPLETO, sin seleccionar bloques parciales.
   3. Confirme que la primera salida indica BASE_ACTUAL = POS-SmartK66.
   4. Envie todas las salidas 01A a 01E y la pestana Messages.
   ============================================================================= */

USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO

/* La marca de sesion evita que los lotes de salida reporten exito si el lote
   principal no llego a compilar o fue revertido. Se reinicia en cada corrida. */
EXEC sys.sp_set_session_context
     @key = N'BorrNcEstructuraValidada',
     @value = 0;
GO

IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    THROW 51000,
          'SEGURIDAD: esta migracion solo esta autorizada para POS-SmartK66.',
          1;
END;

IF CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) <> N'READ_WRITE'
BEGIN
    THROW 51001, 'La base autorizada no esta disponible para escritura.', 1;
END;
GO

/* 01A — Identidad inequívoca del entorno ---------------------------------- */
SELECT
    N'01A_ENTORNO' AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME() AS BASE_ACTUAL,
    CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL,
    CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) AS ACTUALIZABLE,
    SYSDATETIME() AS FECHA_EJECUCION;
GO

DECLARE @ObjetosEsperados TABLE
(
    ESQUEMA sysname NOT NULL,
    OBJETO  sysname NOT NULL,
    TIPO    char(2) NOT NULL,
    PRIMARY KEY (ESQUEMA, OBJETO)
);

INSERT INTO @ObjetosEsperados (ESQUEMA, OBJETO, TIPO)
VALUES
    (N'dbo', N'BORR_NC_SERIES',       'U'),
    (N'dbo', N'BORR_NC_ENC',          'U'),
    (N'dbo', N'BORR_NC_DET',          'U'),
    (N'dbo', N'VW_BORR_NC_ACUMULADO', 'V');

IF EXISTS
(
    SELECT 1
    FROM @ObjetosEsperados E
    JOIN sys.schemas S
      ON S.name COLLATE DATABASE_DEFAULT = E.ESQUEMA
    JOIN sys.objects O
      ON O.schema_id = S.schema_id
     AND O.name COLLATE DATABASE_DEFAULT = E.OBJETO
    WHERE O.type COLLATE DATABASE_DEFAULT <> E.TIPO
)
BEGIN
    THROW 51002,
          'Existe un objeto BORR_NC con un tipo incompatible. No se realizo ningun cambio.',
          1;
END;

DECLARE @ObjetosPresentes int =
(
    SELECT COUNT(*)
    FROM @ObjetosEsperados E
    JOIN sys.schemas S
      ON S.name COLLATE DATABASE_DEFAULT = E.ESQUEMA
    JOIN sys.objects O
      ON O.schema_id = S.schema_id
     AND O.name COLLATE DATABASE_DEFAULT = E.OBJETO
     AND O.type COLLATE DATABASE_DEFAULT = E.TIPO
);

IF @ObjetosPresentes NOT IN (0, 4)
BEGIN
    THROW 51003,
          'Se encontro una instalacion parcial de BORR_NC. No se realizo ningun cambio.',
          1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ObjetosPresentes = 0
    BEGIN
        /* Series: una fila por empresa, independiente del desktop. */
        CREATE TABLE dbo.BORR_NC_SERIES
        (
            EMPRESA    nvarchar(15) NOT NULL,
            SERIE      nvarchar(10) NOT NULL,
            NUMERACION int          NOT NULL
                CONSTRAINT DF_BNS_NUMERACION DEFAULT (0),
            ACTIVO     bit          NOT NULL
                CONSTRAINT DF_BNS_ACTIVO DEFAULT (1),

            CONSTRAINT PK_BORR_NC_SERIES
                PRIMARY KEY CLUSTERED (EMPRESA),
            CONSTRAINT CK_BNS_EMPRESA
                CHECK (NULLIF(LTRIM(RTRIM(EMPRESA)), N'') IS NOT NULL),
            CONSTRAINT CK_BNS_SERIE
                CHECK (NULLIF(LTRIM(RTRIM(SERIE)), N'') IS NOT NULL),
            CONSTRAINT CK_BNS_NUMERACION
                CHECK (NUMERACION BETWEEN 0 AND 99999)
        );

        /* Encabezado del borrador. */
        CREATE TABLE dbo.BORR_NC_ENC
        (
            ID_BORRADOR       nvarchar(20)   NOT NULL,
            ID_EMPRESA        nvarchar(15)   NOT NULL,
            FECHA             date           NOT NULL,
            ID_CLIENTE        nvarchar(20)   NOT NULL,
            NOMBRE            nvarchar(200)  NOT NULL,
            NIT               nvarchar(50)   NULL,
            DIRECCION         nvarchar(200)  NULL,
            CORREO            nvarchar(100)  NULL,
            AGENTE            nvarchar(155)  NOT NULL,
            MONEDA            nvarchar(5)    NOT NULL,
            TOTAL             decimal(20, 3) NOT NULL,
            ESTADO            varchar(20)    NOT NULL
                CONSTRAINT DF_BNE_ESTADO DEFAULT ('PENDIENTE'),
            ID_USR            nvarchar(50)   NOT NULL,
            DEPTO             nvarchar(50)   NULL,
            CODIGO_OPERADOR   nvarchar(50)   NULL,
            REGISTRO          datetime2(0)   NOT NULL
                CONSTRAINT DF_BNE_REGISTRO DEFAULT (SYSDATETIME()),
            RESUELTO_POR      nvarchar(50)   NULL,
            FECHA_RESOLUCION  datetime2(0)   NULL,
            MOTIVO_RESOLUCION nvarchar(1000) NULL,

            CONSTRAINT PK_BORR_NC_ENC
                PRIMARY KEY CLUSTERED (ID_EMPRESA, ID_BORRADOR),
            CONSTRAINT CK_BNE_ESTADO
                CHECK (ESTADO IN ('PENDIENTE', 'AUTORIZADO', 'RECHAZADO', 'ANULADO')),
            CONSTRAINT CK_BNE_TOTAL
                CHECK (TOTAL > 0),
            CONSTRAINT CK_BNE_MONEDA
                CHECK (MONEDA IN (N'GTQ', N'USD', N'EUR')),
            CONSTRAINT CK_BNE_RESOLUCION
                CHECK
                (
                    (ESTADO = 'PENDIENTE'
                     AND RESUELTO_POR IS NULL
                     AND FECHA_RESOLUCION IS NULL
                     AND MOTIVO_RESOLUCION IS NULL)
                    OR
                    (ESTADO = 'AUTORIZADO'
                     AND NULLIF(LTRIM(RTRIM(RESUELTO_POR)), N'') IS NOT NULL
                     AND FECHA_RESOLUCION IS NOT NULL)
                    OR
                    (ESTADO IN ('RECHAZADO', 'ANULADO')
                     AND NULLIF(LTRIM(RTRIM(RESUELTO_POR)), N'') IS NOT NULL
                     AND FECHA_RESOLUCION IS NOT NULL
                     AND NULLIF(LTRIM(RTRIM(MOTIVO_RESOLUCION)), N'') IS NOT NULL)
                )
        );

        /* Lineas del borrador. */
        CREATE TABLE dbo.BORR_NC_DET
        (
            ROWID         bigint IDENTITY(1, 1) NOT NULL,
            ID_BORRADOR   nvarchar(20)   NOT NULL,
            ID_EMPRESA    nvarchar(15)   NOT NULL,
            CONCEPTO      nvarchar(20)   NOT NULL,
            DOCUMENTO     nvarchar(50)   NOT NULL,
            FECHA_DOC     date           NOT NULL,
            SERIE         nvarchar(20)   NULL,
            NUMERO        nvarchar(150)  NULL,
            TOTAL_FACT    decimal(20, 3) NOT NULL,
            PAGADO        decimal(20, 3) NOT NULL
                CONSTRAINT DF_BND_PAGADO DEFAULT (0),
            NC_PREVIA_SAP decimal(20, 3) NOT NULL
                CONSTRAINT DF_BND_NC_PREVIA_SAP DEFAULT (0),
            MONEDA        nvarchar(5)    NOT NULL,
            DESCRIPCION   nvarchar(500)  NOT NULL,
            IMPORTE       decimal(20, 3) NOT NULL,

            CONSTRAINT PK_BORR_NC_DET
                PRIMARY KEY CLUSTERED (ROWID),
            CONSTRAINT FK_BORR_NC_DET_ENC
                FOREIGN KEY (ID_EMPRESA, ID_BORRADOR)
                REFERENCES dbo.BORR_NC_ENC (ID_EMPRESA, ID_BORRADOR)
                ON DELETE CASCADE,
            CONSTRAINT UQ_BORR_NC_DET_DOC
                UNIQUE (ID_EMPRESA, ID_BORRADOR, DOCUMENTO),
            CONSTRAINT CK_BND_CONCEPTO
                CHECK (CONCEPTO IN (N'DEVOLUCION', N'DESCUENTO', N'OTROS')),
            CONSTRAINT CK_BND_MONEDA
                CHECK (MONEDA IN (N'GTQ', N'USD', N'EUR')),
            CONSTRAINT CK_BND_MONTOS
                CHECK
                (
                    TOTAL_FACT > 0
                    AND PAGADO >= 0
                    AND NC_PREVIA_SAP >= 0
                    AND IMPORTE > 0
                    AND IMPORTE <= TOTAL_FACT
                )
        );

        /* Indices alineados con bandeja, seguimiento y control de acumulados. */
        CREATE NONCLUSTERED INDEX IX_BORR_NC_ENC_EMPRESA_ESTADO_FECHA
            ON dbo.BORR_NC_ENC (ID_EMPRESA, ESTADO, FECHA DESC)
            INCLUDE (ID_BORRADOR, ID_USR, AGENTE, TOTAL, REGISTRO);

        CREATE NONCLUSTERED INDEX IX_BORR_NC_ENC_USR_ESTADO_FECHA
            ON dbo.BORR_NC_ENC (ID_USR, ESTADO, FECHA DESC)
            INCLUDE (ID_EMPRESA, ID_BORRADOR);

        CREATE NONCLUSTERED INDEX IX_BORR_NC_ENC_AGENTE_ESTADO_FECHA
            ON dbo.BORR_NC_ENC (AGENTE, ESTADO, FECHA DESC)
            INCLUDE (ID_EMPRESA, ID_BORRADOR);

        CREATE NONCLUSTERED INDEX IX_BORR_NC_DET_EMPRESA_DOCUMENTO
            ON dbo.BORR_NC_DET (ID_EMPRESA, DOCUMENTO)
            INCLUDE (ID_BORRADOR, IMPORTE);

        /* CREATE VIEW debe iniciar su propio lote; el SQL dinamico conserva
           la misma transaccion y permite mantener una sola unidad atomica. */
        EXEC sys.sp_executesql N'
            CREATE VIEW dbo.VW_BORR_NC_ACUMULADO
            AS
            SELECT
                D.ID_EMPRESA,
                D.DOCUMENTO,
                SUM(D.IMPORTE) AS ACUMULADO,
                COUNT_BIG(*) AS LINEAS
            FROM dbo.BORR_NC_DET D
            INNER JOIN dbo.BORR_NC_ENC E
                    ON E.ID_EMPRESA = D.ID_EMPRESA
                   AND E.ID_BORRADOR = D.ID_BORRADOR
            WHERE E.ESTADO IN (''PENDIENTE'', ''AUTORIZADO'')
            GROUP BY D.ID_EMPRESA, D.DOCUMENTO;';
    END
    ELSE
    BEGIN
        PRINT 'Los cuatro objetos BORR_NC ya existen; se validara su compatibilidad.';
    END;

    /* Las series son nuevas y deliberadamente distintas de REC_CAJA_SERIES. */
    DECLARE @SeriesEsperadas TABLE
    (
        EMPRESA nvarchar(15) NOT NULL PRIMARY KEY,
        SERIE   nvarchar(10) NOT NULL
    );

    INSERT INTO @SeriesEsperadas (EMPRESA, SERIE)
    VALUES
        (N'BOLIK', N'BWB-'),
        (N'FAES',  N'BWF-'),
        (N'GRACO', N'BWG-');

    IF EXISTS
    (
        SELECT 1
        FROM @SeriesEsperadas E
        JOIN dbo.BORR_NC_SERIES S
          ON S.EMPRESA = E.EMPRESA
        WHERE S.SERIE <> E.SERIE
           OR S.NUMERACION < 0
    )
    BEGIN
        THROW 51004,
              'Una serie BORR_NC existente no coincide con la configuracion esperada.',
              1;
    END;

    INSERT INTO dbo.BORR_NC_SERIES (EMPRESA, SERIE, NUMERACION, ACTIVO)
    SELECT E.EMPRESA, E.SERIE, 0, 1
    FROM @SeriesEsperadas E
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.BORR_NC_SERIES S WITH (UPDLOCK, HOLDLOCK)
        WHERE S.EMPRESA = E.EMPRESA
    );

    /* Validacion estructural para la primera ejecucion y las reejecuciones. */
    IF
    (
        SELECT COUNT(*)
        FROM @ObjetosEsperados E
        JOIN sys.schemas S
          ON S.name COLLATE DATABASE_DEFAULT = E.ESQUEMA
        JOIN sys.objects O
          ON O.schema_id = S.schema_id
         AND O.name COLLATE DATABASE_DEFAULT = E.OBJETO
         AND O.type COLLATE DATABASE_DEFAULT = E.TIPO
    ) <> 4
    BEGIN
        THROW 51005, 'La creacion de los objetos BORR_NC no quedo completa.', 1;
    END;

    DECLARE @ColumnasEsperadas TABLE
    (
        TABLA  sysname NOT NULL,
        COLUMNA sysname NOT NULL,
        PRIMARY KEY (TABLA, COLUMNA)
    );

    INSERT INTO @ColumnasEsperadas (TABLA, COLUMNA)
    VALUES
        (N'BORR_NC_SERIES', N'EMPRESA'),
        (N'BORR_NC_SERIES', N'SERIE'),
        (N'BORR_NC_SERIES', N'NUMERACION'),
        (N'BORR_NC_SERIES', N'ACTIVO'),
        (N'BORR_NC_ENC', N'ID_BORRADOR'),
        (N'BORR_NC_ENC', N'ID_EMPRESA'),
        (N'BORR_NC_ENC', N'FECHA'),
        (N'BORR_NC_ENC', N'ID_CLIENTE'),
        (N'BORR_NC_ENC', N'NOMBRE'),
        (N'BORR_NC_ENC', N'NIT'),
        (N'BORR_NC_ENC', N'DIRECCION'),
        (N'BORR_NC_ENC', N'CORREO'),
        (N'BORR_NC_ENC', N'AGENTE'),
        (N'BORR_NC_ENC', N'MONEDA'),
        (N'BORR_NC_ENC', N'TOTAL'),
        (N'BORR_NC_ENC', N'ESTADO'),
        (N'BORR_NC_ENC', N'ID_USR'),
        (N'BORR_NC_ENC', N'DEPTO'),
        (N'BORR_NC_ENC', N'CODIGO_OPERADOR'),
        (N'BORR_NC_ENC', N'REGISTRO'),
        (N'BORR_NC_ENC', N'RESUELTO_POR'),
        (N'BORR_NC_ENC', N'FECHA_RESOLUCION'),
        (N'BORR_NC_ENC', N'MOTIVO_RESOLUCION'),
        (N'BORR_NC_DET', N'ROWID'),
        (N'BORR_NC_DET', N'ID_BORRADOR'),
        (N'BORR_NC_DET', N'ID_EMPRESA'),
        (N'BORR_NC_DET', N'CONCEPTO'),
        (N'BORR_NC_DET', N'DOCUMENTO'),
        (N'BORR_NC_DET', N'FECHA_DOC'),
        (N'BORR_NC_DET', N'SERIE'),
        (N'BORR_NC_DET', N'NUMERO'),
        (N'BORR_NC_DET', N'TOTAL_FACT'),
        (N'BORR_NC_DET', N'PAGADO'),
        (N'BORR_NC_DET', N'NC_PREVIA_SAP'),
        (N'BORR_NC_DET', N'MONEDA'),
        (N'BORR_NC_DET', N'DESCRIPCION'),
        (N'BORR_NC_DET', N'IMPORTE');

    IF EXISTS
    (
        SELECT 1
        FROM @ColumnasEsperadas E
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM sys.columns C
            JOIN sys.tables T
              ON T.object_id = C.object_id
            JOIN sys.schemas S
              ON S.schema_id = T.schema_id
            WHERE S.name = N'dbo'
              AND T.name COLLATE DATABASE_DEFAULT = E.TABLA
              AND C.name COLLATE DATABASE_DEFAULT = E.COLUMNA
        )
    )
    OR
    (
        SELECT COUNT(*)
        FROM sys.columns C
        JOIN sys.tables T
          ON T.object_id = C.object_id
        JOIN sys.schemas S
          ON S.schema_id = T.schema_id
        WHERE S.name = N'dbo'
          AND T.name IN (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET')
    ) <> 37
    BEGIN
        THROW 51006, 'Las columnas BORR_NC no coinciden con el contrato esperado.', 1;
    END;

    DECLARE @RestriccionesEsperadas TABLE
    (
        TABLA       sysname NOT NULL,
        RESTRICCION sysname NOT NULL,
        PRIMARY KEY (TABLA, RESTRICCION)
    );

    INSERT INTO @RestriccionesEsperadas (TABLA, RESTRICCION)
    VALUES
        (N'BORR_NC_SERIES', N'PK_BORR_NC_SERIES'),
        (N'BORR_NC_SERIES', N'DF_BNS_NUMERACION'),
        (N'BORR_NC_SERIES', N'DF_BNS_ACTIVO'),
        (N'BORR_NC_SERIES', N'CK_BNS_EMPRESA'),
        (N'BORR_NC_SERIES', N'CK_BNS_SERIE'),
        (N'BORR_NC_SERIES', N'CK_BNS_NUMERACION'),
        (N'BORR_NC_ENC', N'PK_BORR_NC_ENC'),
        (N'BORR_NC_ENC', N'DF_BNE_ESTADO'),
        (N'BORR_NC_ENC', N'DF_BNE_REGISTRO'),
        (N'BORR_NC_ENC', N'CK_BNE_ESTADO'),
        (N'BORR_NC_ENC', N'CK_BNE_TOTAL'),
        (N'BORR_NC_ENC', N'CK_BNE_MONEDA'),
        (N'BORR_NC_ENC', N'CK_BNE_RESOLUCION'),
        (N'BORR_NC_DET', N'PK_BORR_NC_DET'),
        (N'BORR_NC_DET', N'FK_BORR_NC_DET_ENC'),
        (N'BORR_NC_DET', N'UQ_BORR_NC_DET_DOC'),
        (N'BORR_NC_DET', N'DF_BND_PAGADO'),
        (N'BORR_NC_DET', N'DF_BND_NC_PREVIA_SAP'),
        (N'BORR_NC_DET', N'CK_BND_CONCEPTO'),
        (N'BORR_NC_DET', N'CK_BND_MONEDA'),
        (N'BORR_NC_DET', N'CK_BND_MONTOS');

    IF EXISTS
    (
        SELECT 1
        FROM @RestriccionesEsperadas E
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM sys.objects O
            JOIN sys.tables T
              ON T.object_id = O.parent_object_id
            JOIN sys.schemas S
              ON S.schema_id = T.schema_id
            WHERE S.name = N'dbo'
              AND T.name COLLATE DATABASE_DEFAULT = E.TABLA
              AND O.name COLLATE DATABASE_DEFAULT = E.RESTRICCION
              AND O.type IN ('PK', 'UQ', 'F', 'C', 'D')
        )
    )
    BEGIN
        THROW 51007, 'Faltan restricciones requeridas de BORR_NC.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints C
        WHERE C.parent_object_id IN
              (OBJECT_ID(N'dbo.BORR_NC_SERIES'),
               OBJECT_ID(N'dbo.BORR_NC_ENC'),
               OBJECT_ID(N'dbo.BORR_NC_DET'))
          AND (C.is_disabled = 1 OR C.is_not_trusted = 1)
    )
    OR EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys F
        WHERE F.parent_object_id = OBJECT_ID(N'dbo.BORR_NC_DET')
          AND (F.is_disabled = 1 OR F.is_not_trusted = 1)
    )
    BEGIN
        THROW 51008, 'Hay restricciones BORR_NC deshabilitadas o no confiables.', 1;
    END;

    DECLARE @IndicesEsperados TABLE
    (
        TABLA  sysname NOT NULL,
        INDICE sysname NOT NULL,
        PRIMARY KEY (TABLA, INDICE)
    );

    INSERT INTO @IndicesEsperados (TABLA, INDICE)
    VALUES
        (N'BORR_NC_SERIES', N'PK_BORR_NC_SERIES'),
        (N'BORR_NC_ENC', N'PK_BORR_NC_ENC'),
        (N'BORR_NC_ENC', N'IX_BORR_NC_ENC_EMPRESA_ESTADO_FECHA'),
        (N'BORR_NC_ENC', N'IX_BORR_NC_ENC_USR_ESTADO_FECHA'),
        (N'BORR_NC_ENC', N'IX_BORR_NC_ENC_AGENTE_ESTADO_FECHA'),
        (N'BORR_NC_DET', N'PK_BORR_NC_DET'),
        (N'BORR_NC_DET', N'UQ_BORR_NC_DET_DOC'),
        (N'BORR_NC_DET', N'IX_BORR_NC_DET_EMPRESA_DOCUMENTO');

    IF EXISTS
    (
        SELECT 1
        FROM @IndicesEsperados E
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes I
            JOIN sys.tables T
              ON T.object_id = I.object_id
            JOIN sys.schemas S
              ON S.schema_id = T.schema_id
            WHERE S.name = N'dbo'
              AND T.name COLLATE DATABASE_DEFAULT = E.TABLA
              AND I.name COLLATE DATABASE_DEFAULT = E.INDICE
              AND I.is_disabled = 0
              AND I.is_hypothetical = 0
        )
    )
    BEGIN
        THROW 51009, 'Faltan indices requeridos de BORR_NC.', 1;
    END;

    DECLARE @DefinicionVista nvarchar(max) =
        OBJECT_DEFINITION(OBJECT_ID(N'dbo.VW_BORR_NC_ACUMULADO', N'V'));

    IF @DefinicionVista IS NULL
       OR @DefinicionVista NOT LIKE N'%BORR_NC_DET%'
       OR @DefinicionVista NOT LIKE N'%BORR_NC_ENC%'
       OR @DefinicionVista NOT LIKE N'%PENDIENTE%'
       OR @DefinicionVista NOT LIKE N'%AUTORIZADO%'
    BEGIN
        THROW 51010, 'La vista BORR_NC no coincide con el contrato esperado.', 1;
    END;

    IF
    (
        SELECT COUNT(*)
        FROM @SeriesEsperadas E
        JOIN dbo.BORR_NC_SERIES S
          ON S.EMPRESA = E.EMPRESA
         AND S.SERIE = E.SERIE
    ) <> 3
    BEGIN
        THROW 51011, 'No quedaron configuradas las tres series BORR_NC.', 1;
    END;

    COMMIT TRANSACTION;

    EXEC sys.sp_set_session_context
         @key = N'BorrNcEstructuraValidada',
         @value = 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

/* La salida completa vive en un solo lote. Si la marca no fue establecida o
   falta algun objeto, THROW cancela el lote antes de consultar tablas nuevas
   y antes de imprimir el mensaje final de exito. */
IF ISNULL(TRY_CONVERT(int,
       SESSION_CONTEXT(N'BorrNcEstructuraValidada')), 0) <> 1
   OR OBJECT_ID(N'dbo.BORR_NC_SERIES', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BORR_NC_DET', N'U') IS NULL
   OR OBJECT_ID(N'dbo.VW_BORR_NC_ACUMULADO', N'V') IS NULL
BEGIN
    THROW 51012,
          'La migracion BORR_NC no finalizo correctamente; no se reportara exito.',
          1;
END;

/* 01B — Objetos creados ---------------------------------------------------- */
SELECT
    N'01B_OBJETOS' AS SECCION,
    S.name AS ESQUEMA,
    O.name AS OBJETO,
    O.type_desc AS TIPO,
    O.create_date AS FECHA_CREACION,
    O.modify_date AS FECHA_MODIFICACION
FROM sys.objects O
JOIN sys.schemas S
  ON S.schema_id = O.schema_id
WHERE S.name = N'dbo'
  AND O.name IN
      (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET',
       N'VW_BORR_NC_ACUMULADO')
ORDER BY O.name;

/* 01C — Series configuradas ------------------------------------------------ */
/* SQL dinamico evita resolver la tabla antes de que el guard clause anterior
   pueda detener una corrida cuyo lote principal no compilo. */
EXEC sys.sp_executesql N'
    SELECT
        N''01C_SERIES'' AS SECCION,
        EMPRESA,
        SERIE,
        NUMERACION,
        ACTIVO
    FROM dbo.BORR_NC_SERIES
    WHERE EMPRESA IN (N''BOLIK'', N''FAES'', N''GRACO'')
    ORDER BY EMPRESA;';

/* 01D — Restricciones ------------------------------------------------------ */
SELECT
    N'01D_RESTRICCIONES' AS SECCION,
    OBJECT_NAME(O.parent_object_id) AS TABLA,
    O.name AS RESTRICCION,
    O.type_desc AS TIPO
FROM sys.objects O
WHERE O.parent_object_id IN
      (OBJECT_ID(N'dbo.BORR_NC_SERIES'),
       OBJECT_ID(N'dbo.BORR_NC_ENC'),
       OBJECT_ID(N'dbo.BORR_NC_DET'))
  AND O.type IN ('PK', 'UQ', 'F', 'C', 'D')
ORDER BY TABLA, TIPO, RESTRICCION;

/* 01E — Indices ------------------------------------------------------------ */
SELECT
    N'01E_INDICES' AS SECCION,
    OBJECT_NAME(I.object_id) AS TABLA,
    I.name AS INDICE,
    I.type_desc AS TIPO,
    I.is_unique AS ES_UNICO,
    I.is_primary_key AS ES_PK,
    I.is_disabled AS DESHABILITADO
FROM sys.indexes I
WHERE I.object_id IN
      (OBJECT_ID(N'dbo.BORR_NC_SERIES'),
       OBJECT_ID(N'dbo.BORR_NC_ENC'),
       OBJECT_ID(N'dbo.BORR_NC_DET'))
  AND I.name IS NOT NULL
ORDER BY TABLA, I.index_id;

PRINT 'OK: estructura BORR_NC validada en POS-SmartK66.';
GO
