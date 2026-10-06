/* Relación explícita NC SAP / borrador. Ejecutar en POS-SmartK66 antes de
   publicar la vista DetalleBorradorBNC. No modifica ninguna tabla existente. */
USE [POS-SmartK66];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
GO
IF DB_NAME() <> N'POS-SmartK66'
    THROW 57000, 'SEGURIDAD: este script solo corresponde a POS-SmartK66.', 1;
IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
    THROW 57001, 'Falta dbo.BORR_NC_ENC.', 1;
IF OBJECT_ID(N'dbo.BORR_NC_NC_VINCULO') IS NOT NULL
   AND OBJECT_ID(N'dbo.BORR_NC_NC_VINCULO', N'U') IS NULL
    THROW 57002, 'El nombre de la tabla está ocupado por otro objeto.', 1;
GO
BEGIN TRY
    -- SSMS puede continuar después de un GO aunque un lote previo falle.
    IF DB_NAME() <> N'POS-SmartK66'
        THROW 57000, 'SEGURIDAD: este script solo corresponde a POS-SmartK66.', 1;
    IF OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NULL
        THROW 57001, 'Falta dbo.BORR_NC_ENC.', 1;
    BEGIN TRANSACTION;
    DECLARE @Bloqueo int;
    EXEC @Bloqueo = sys.sp_getapplock
        @Resource=N'BorradorNc.NcVinculo', @LockMode=N'Exclusive',
        @LockOwner=N'Transaction', @LockTimeout=5000;
    IF @Bloqueo < 0 THROW 57003, 'No se obtuvo el bloqueo de migración.', 1;

    IF OBJECT_ID(N'dbo.BORR_NC_NC_VINCULO', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BORR_NC_NC_VINCULO
        (
            ID_EMPRESA  nvarchar(15) NOT NULL,
            ID_BORRADOR nvarchar(20) NOT NULL,
            DOC_ENTRY   int NOT NULL,
            DOCUMENTO   nvarchar(40) NOT NULL,
            FACTURA     nvarchar(40) NOT NULL,
            USUARIO     nvarchar(50) NOT NULL,
            REGISTRO    datetime2(3) NOT NULL
                CONSTRAINT DF_BNV_REGISTRO DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_BORR_NC_NC_VINCULO
                PRIMARY KEY CLUSTERED (ID_EMPRESA, ID_BORRADOR, DOC_ENTRY),
            CONSTRAINT UQ_BORR_NC_NC_VINCULO_SAP
                UNIQUE NONCLUSTERED (ID_EMPRESA, DOC_ENTRY),
            CONSTRAINT FK_BORR_NC_NC_VINCULO_ENC
                FOREIGN KEY (ID_EMPRESA, ID_BORRADOR)
                REFERENCES dbo.BORR_NC_ENC (ID_EMPRESA, ID_BORRADOR),
            CONSTRAINT CK_BNV_DOC_ENTRY CHECK (DOC_ENTRY > 0),
            CONSTRAINT CK_BNV_DOCUMENTO CHECK (LEN(LTRIM(RTRIM(DOCUMENTO))) > 0),
            CONSTRAINT CK_BNV_FACTURA CHECK (LEN(LTRIM(RTRIM(FACTURA))) > 0)
        );
    END;

    IF EXISTS (
        SELECT 1 FROM (VALUES
            (N'ID_EMPRESA',N'nvarchar',30), (N'ID_BORRADOR',N'nvarchar',40),
            (N'DOC_ENTRY',N'int',4), (N'DOCUMENTO',N'nvarchar',80),
            (N'FACTURA',N'nvarchar',80), (N'USUARIO',N'nvarchar',100),
            (N'REGISTRO',N'datetime2',8)
        ) AS esperado(nombre,tipo,largo)
        LEFT JOIN sys.columns C ON C.object_id=OBJECT_ID(N'dbo.BORR_NC_NC_VINCULO')
            AND C.name=esperado.nombre
        LEFT JOIN sys.types T ON T.user_type_id=C.user_type_id
        WHERE C.column_id IS NULL OR C.is_nullable=1
            OR T.name<>esperado.tipo OR C.max_length<>esperado.largo
    ) THROW 57004, 'La tabla existe con columnas incompatibles.', 1;

    IF (SELECT COUNT(*) FROM sys.objects O
        WHERE O.parent_object_id=OBJECT_ID(N'dbo.BORR_NC_NC_VINCULO')
          AND O.name IN (N'PK_BORR_NC_NC_VINCULO',N'UQ_BORR_NC_NC_VINCULO_SAP',
                         N'FK_BORR_NC_NC_VINCULO_ENC',N'CK_BNV_DOC_ENTRY',
                         N'CK_BNV_DOCUMENTO',N'CK_BNV_FACTURA',N'DF_BNV_REGISTRO')) <> 7
        THROW 57005, 'La tabla existe sin todas las restricciones esperadas.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT DB_NAME() AS BASE_ACTUAL, OBJECT_ID(N'dbo.BORR_NC_NC_VINCULO', N'U') AS TABLA,
    (SELECT COUNT_BIG(*) FROM dbo.BORR_NC_NC_VINCULO) AS VINCULOS;
GO
