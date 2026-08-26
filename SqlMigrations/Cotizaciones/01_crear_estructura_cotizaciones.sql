/* =============================================================================
   COTIZACIONES — ESTRUCTURA TRANSACCIONAL
   Destino autorizado: POS-SmartK66 (PRUEBAS)
   Reejecutable, sin DROP/DELETE y con rollback ante errores.
   ============================================================================= */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'POS-SmartK66'
    THROW 53000, 'SEGURIDAD: este script solo se ejecuta en POS-SmartK66.', 1;

IF CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) <> N'READ_WRITE'
    THROW 53001, 'La base de pruebas no está disponible para escritura.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.COT_SERIES', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.COT_SERIES
        (
            EMPRESA     nvarchar(15) NOT NULL,
            SERIE       nvarchar(15) NOT NULL,
            NUMERACION  bigint NOT NULL CONSTRAINT DF_COT_SERIES_NUM DEFAULT (0),
            ACTIVO      bit NOT NULL CONSTRAINT DF_COT_SERIES_ACT DEFAULT (1),
            MODIFICADO  datetime2(0) NOT NULL CONSTRAINT DF_COT_SERIES_MOD DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_COT_SERIES PRIMARY KEY CLUSTERED (EMPRESA),
            CONSTRAINT CK_COT_SERIES_NUM CHECK (NUMERACION >= 0),
            CONSTRAINT CK_COT_SERIES_PREFIJO CHECK (LEN(LTRIM(RTRIM(SERIE))) BETWEEN 1 AND 15)
        );
    END;

    IF OBJECT_ID(N'dbo.COT_ENC', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.COT_ENC
        (
            ID_COTIZACION     nvarchar(30) NOT NULL,
            ID_EMPRESA        nvarchar(15) NOT NULL,
            FECHA             date NOT NULL,
            VALIDA_HASTA      date NOT NULL,
            ID_CLIENTE        nvarchar(20) NOT NULL,
            NOMBRE_CLIENTE    nvarchar(200) NOT NULL,
            NIT               nvarchar(50) NULL,
            DIRECCION         nvarchar(300) NULL,
            CORREO            nvarchar(150) NULL,
            CODIGO_OPERADOR   nvarchar(128) NOT NULL,
            AGENTE            nvarchar(155) NOT NULL,
            MONEDA            nvarchar(5) NOT NULL,
            CONDICIONES_PAGO  nvarchar(250) NULL,
            TIEMPO_ENTREGA    nvarchar(250) NULL,
            OBSERVACIONES     nvarchar(1500) NULL,
            IMPORTE_BRUTO     decimal(20,2) NOT NULL,
            DESCUENTO_TOTAL   decimal(20,2) NOT NULL,
            SUBTOTAL          decimal(20,2) NOT NULL,
            IMPUESTO_TOTAL    decimal(20,2) NOT NULL,
            TOTAL             decimal(20,2) NOT NULL,
            ESTADO            varchar(20) NOT NULL,
            ID_USR            nvarchar(100) NOT NULL,
            REGISTRO          datetime2(0) NOT NULL CONSTRAINT DF_COT_ENC_REG DEFAULT (SYSDATETIME()),
            ANULADO_POR       nvarchar(100) NULL,
            FECHA_ANULACION   datetime2(0) NULL,
            MOTIVO_ANULACION  nvarchar(1000) NULL,
            CONSTRAINT PK_COT_ENC PRIMARY KEY CLUSTERED (ID_EMPRESA, ID_COTIZACION),
            CONSTRAINT CK_COT_ENC_FECHAS CHECK (VALIDA_HASTA >= FECHA),
            CONSTRAINT CK_COT_ENC_MONEDA CHECK (MONEDA IN (N'GTQ', N'USD', N'EUR')),
            CONSTRAINT CK_COT_ENC_ESTADO CHECK (ESTADO IN ('VIGENTE', 'ANULADA')),
            CONSTRAINT CK_COT_ENC_TOTALES CHECK
                (IMPORTE_BRUTO >= 0 AND DESCUENTO_TOTAL >= 0 AND SUBTOTAL >= 0
                 AND IMPUESTO_TOTAL >= 0 AND TOTAL >= 0)
        );

        CREATE INDEX IX_COT_ENC_FECHA
            ON dbo.COT_ENC (FECHA DESC, ID_EMPRESA)
            INCLUDE (ID_COTIZACION, ID_CLIENTE, NOMBRE_CLIENTE, AGENTE, MONEDA, TOTAL, ESTADO, ID_USR);
        CREATE INDEX IX_COT_ENC_USUARIO
            ON dbo.COT_ENC (ID_USR, FECHA DESC)
            INCLUDE (ID_EMPRESA, ID_COTIZACION, TOTAL, ESTADO);
        CREATE INDEX IX_COT_ENC_AGENTE
            ON dbo.COT_ENC (ID_EMPRESA, AGENTE, FECHA DESC)
            INCLUDE (ID_COTIZACION, ID_CLIENTE, TOTAL, ESTADO);
    END;

    IF OBJECT_ID(N'dbo.COT_DET', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.COT_DET
        (
            ROWID                    bigint IDENTITY(1,1) NOT NULL,
            ID_COTIZACION            nvarchar(30) NOT NULL,
            ID_EMPRESA               nvarchar(15) NOT NULL,
            LINEA                    int NOT NULL,
            ITEM_CODE                nvarchar(50) NOT NULL,
            ITEM_NAME                nvarchar(200) NOT NULL,
            DESCRIPCION              nvarchar(500) NOT NULL,
            GRUPO                    nvarchar(100) NULL,
            UNIDAD                   nvarchar(100) NULL,
            LISTA_PRECIO             int NOT NULL,
            EXISTENCIA               decimal(19,6) NOT NULL,
            DISPONIBLE               decimal(19,6) NOT NULL,
            CANTIDAD                 decimal(19,6) NOT NULL,
            PRECIO_LISTA             decimal(20,6) NOT NULL,
            PRECIO_UNITARIO          decimal(20,6) NOT NULL,
            DESCUENTO_PORCENTAJE     decimal(9,4) NOT NULL,
            GRUPO_IMPUESTO           nvarchar(8) NULL,
            IMPUESTO_PORCENTAJE      decimal(9,4) NOT NULL,
            IMPORTE_BRUTO            decimal(20,2) NOT NULL,
            DESCUENTO_MONTO          decimal(20,2) NOT NULL,
            SUBTOTAL                 decimal(20,2) NOT NULL,
            IMPUESTO_MONTO           decimal(20,2) NOT NULL,
            TOTAL                    decimal(20,2) NOT NULL,
            CONSTRAINT PK_COT_DET PRIMARY KEY CLUSTERED (ROWID),
            CONSTRAINT UQ_COT_DET_LINEA UNIQUE (ID_EMPRESA, ID_COTIZACION, LINEA),
            CONSTRAINT UQ_COT_DET_PRODUCTO UNIQUE (ID_EMPRESA, ID_COTIZACION, ITEM_CODE),
            CONSTRAINT FK_COT_DET_ENC FOREIGN KEY (ID_EMPRESA, ID_COTIZACION)
                REFERENCES dbo.COT_ENC (ID_EMPRESA, ID_COTIZACION),
            CONSTRAINT CK_COT_DET_LINEA CHECK (LINEA > 0),
            CONSTRAINT CK_COT_DET_CANTIDAD CHECK (CANTIDAD > 0),
            CONSTRAINT CK_COT_DET_PRECIO CHECK (PRECIO_LISTA >= 0 AND PRECIO_UNITARIO >= 0),
            CONSTRAINT CK_COT_DET_DESCUENTO CHECK (DESCUENTO_PORCENTAJE BETWEEN 0 AND 100),
            CONSTRAINT CK_COT_DET_IMPUESTO CHECK (IMPUESTO_PORCENTAJE BETWEEN 0 AND 100),
            CONSTRAINT CK_COT_DET_TOTALES CHECK
                (IMPORTE_BRUTO >= 0 AND DESCUENTO_MONTO >= 0 AND SUBTOTAL >= 0
                 AND IMPUESTO_MONTO >= 0 AND TOTAL >= 0)
        );

        CREATE INDEX IX_COT_DET_DOCUMENTO
            ON dbo.COT_DET (ID_EMPRESA, ID_COTIZACION, LINEA)
            INCLUDE (ITEM_CODE, ITEM_NAME, CANTIDAD, PRECIO_UNITARIO, TOTAL);
    END;

    DECLARE @ColumnasEsperadas TABLE (TABLA sysname, COLUMNA sysname);
    INSERT INTO @ColumnasEsperadas (TABLA, COLUMNA)
    VALUES
      (N'COT_SERIES', N'EMPRESA'), (N'COT_SERIES', N'SERIE'),
      (N'COT_SERIES', N'NUMERACION'), (N'COT_SERIES', N'ACTIVO'),
      (N'COT_ENC', N'ID_COTIZACION'), (N'COT_ENC', N'ID_EMPRESA'),
      (N'COT_ENC', N'FECHA'), (N'COT_ENC', N'VALIDA_HASTA'),
      (N'COT_ENC', N'ID_CLIENTE'), (N'COT_ENC', N'CODIGO_OPERADOR'),
      (N'COT_ENC', N'AGENTE'), (N'COT_ENC', N'TOTAL'),
      (N'COT_ENC', N'ESTADO'), (N'COT_ENC', N'ID_USR'),
      (N'COT_DET', N'ROWID'), (N'COT_DET', N'ID_COTIZACION'),
      (N'COT_DET', N'ID_EMPRESA'), (N'COT_DET', N'LINEA'),
      (N'COT_DET', N'ITEM_CODE'), (N'COT_DET', N'CANTIDAD'),
      (N'COT_DET', N'PRECIO_LISTA'), (N'COT_DET', N'PRECIO_UNITARIO'),
      (N'COT_DET', N'GRUPO_IMPUESTO'),
      (N'COT_DET', N'TOTAL');

    IF EXISTS
    (
        SELECT 1 FROM @ColumnasEsperadas E
        WHERE NOT EXISTS
        (
            SELECT 1 FROM sys.columns C
            WHERE C.object_id = OBJECT_ID(N'dbo.' + E.TABLA)
              AND C.name = E.COLUMNA
        )
    )
        THROW 53002, 'Existe una estructura Cotizaciones parcial o incompatible.', 1;

    INSERT INTO dbo.COT_SERIES (EMPRESA, SERIE, NUMERACION, ACTIVO)
    SELECT V.EMPRESA, V.SERIE, 0, 1
    FROM (VALUES
        (N'GRACO', N'COT-GR-'),
        (N'FAES',  N'COT-FA-'),
        (N'BOLIK', N'COT-BO-')
    ) V(EMPRESA, SERIE)
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.COT_SERIES S WITH (UPDLOCK, HOLDLOCK)
        WHERE S.EMPRESA = V.EMPRESA
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT N'SERIES' AS SECCION, EMPRESA, SERIE, NUMERACION, ACTIVO, MODIFICADO
FROM dbo.COT_SERIES ORDER BY EMPRESA;

SELECT N'TABLAS' AS SECCION, O.name AS TABLA, SUM(P.rows) AS FILAS
FROM sys.objects O
JOIN sys.partitions P ON P.object_id = O.object_id AND P.index_id IN (0,1)
WHERE O.name IN (N'COT_SERIES', N'COT_ENC', N'COT_DET')
GROUP BY O.name ORDER BY O.name;
GO
