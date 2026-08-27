/* =============================================================================
   BORRADORES NC — VALIDACIÓN DE PRODUCTOS DE FACTURAS (SOLO LECTURA)
   Destino autorizado: POS-SmartK66

   Objetivo
   --------
   1. Elegir un borrador real que tenga facturas asociadas.
   2. Mostrar las claves que la aplicación enviará a SAP HANA.
   3. Generar una consulta HANA lista para copiar y ejecutar, con el schema,
      cliente y DocNum exactos del borrador seleccionado.

   Cómo ejecutarlo
   ---------------
   1. Abra una ventana NUEVA en SSMS y ejecute el archivo COMPLETO.
   2. Deje @Empresa e @IdBorrador en NULL para usar el borrador más reciente,
      o complete AMBOS valores para validar uno específico.
   3. Confirme que 03D_RESUMEN marque OK.
   4. Copie la celda CONSULTA_HANA de 03E_CONSULTA_HANA y ejecútela en la
      consola SQL de SAP HANA.
   5. Comparta las salidas 03A a 03E y el resultado obtenido en HANA.

   Garantía de alcance
   -------------------
   El código activo contiene únicamente USE, opciones de sesión, variables y
   SELECT. No crea, modifica ni elimina objetos o datos.
   ============================================================================= */

USE [POS-SmartK66];
GO

SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
GO

IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    THROW 51000, 'SEGURIDAD: este diagnostico solo esta autorizado para POS-SmartK66.', 1;
END;
GO

/* Complete ambos valores o deje ambos en NULL. ----------------------------- */
DECLARE @Empresa     nvarchar(15) = NULL;
DECLARE @IdBorrador  nvarchar(20) = NULL;

IF (@Empresa IS NULL AND @IdBorrador IS NOT NULL)
   OR (@Empresa IS NOT NULL AND @IdBorrador IS NULL)
BEGIN
    THROW 51001, 'Complete @Empresa y @IdBorrador, o deje ambos en NULL.', 1;
END;

/* Si no se indicó uno, selecciona el borrador más reciente con DocNum válido. */
IF @Empresa IS NULL
BEGIN
    SELECT TOP (1)
        @Empresa    = E.ID_EMPRESA,
        @IdBorrador = E.ID_BORRADOR
    FROM dbo.BORR_NC_ENC E
    WHERE EXISTS
    (
        SELECT 1
        FROM dbo.BORR_NC_DET D
        WHERE D.ID_EMPRESA = E.ID_EMPRESA
          AND D.ID_BORRADOR = E.ID_BORRADOR
          AND TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(D.DOCUMENTO)), N'')) IS NOT NULL
    )
    ORDER BY E.REGISTRO DESC, E.ID_BORRADOR DESC;
END;

IF @Empresa IS NULL OR @IdBorrador IS NULL
BEGIN
    THROW 51002, 'No existe un borrador con facturas numéricas para validar.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.BORR_NC_ENC
    WHERE ID_EMPRESA = @Empresa
      AND ID_BORRADOR = @IdBorrador
)
BEGIN
    THROW 51003, 'El borrador solicitado no existe en POS-SmartK66.', 1;
END;

DECLARE @SchemaHana nvarchar(128) =
    CASE UPPER(LTRIM(RTRIM(@Empresa)))
        WHEN N'GRACO' THEN N'SBO_GRACO'
        WHEN N'FAES'  THEN N'SBOESCOCESA'
        WHEN N'BOLIK' THEN N'SBOBOLIK'
        ELSE NULL
    END;

IF @SchemaHana IS NULL
BEGIN
    THROW 51004, 'La empresa seleccionada no tiene schema HANA configurado.', 1;
END;

/* 03A — Entorno y selección ------------------------------------------------ */
SELECT
    N'03A_SELECCION' AS SECCION,
    CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) AS SERVIDOR,
    DB_NAME() AS BASE_ACTUAL,
    @Empresa AS EMPRESA,
    @IdBorrador AS ID_BORRADOR,
    @SchemaHana AS SCHEMA_HANA,
    SYSDATETIME() AS FECHA_DIAGNOSTICO;

/* 03B — Encabezado que consume el módulo ---------------------------------- */
SELECT
    N'03B_ENCABEZADO' AS SECCION,
    E.ID_BORRADOR,
    E.ID_EMPRESA,
    E.ID_CLIENTE,
    E.NOMBRE,
    E.AGENTE,
    E.MONEDA,
    E.TOTAL,
    E.ESTADO,
    E.REGISTRO
FROM dbo.BORR_NC_ENC E
WHERE E.ID_EMPRESA = @Empresa
  AND E.ID_BORRADOR = @IdBorrador;

/* 03C — Facturas locales que se cruzarán con OINV + INV1 ------------------ */
SELECT
    N'03C_FACTURAS' AS SECCION,
    D.DOCUMENTO AS DOCNUM,
    TRY_CONVERT(bigint, D.DOCUMENTO) AS DOCNUM_NUMERICO,
    D.FECHA_DOC,
    D.SERIE AS SERIE_FEL,
    D.NUMERO AS NUMERO_FEL,
    D.CONCEPTO,
    D.MONEDA,
    D.TOTAL_FACT,
    D.IMPORTE
FROM dbo.BORR_NC_DET D
WHERE D.ID_EMPRESA = @Empresa
  AND D.ID_BORRADOR = @IdBorrador
ORDER BY TRY_CONVERT(bigint, D.DOCUMENTO), D.DOCUMENTO;

DECLARE @CantidadFacturas int;
DECLARE @DocumentosInvalidos int;
DECLARE @DocumentosDuplicados int;

SELECT
    @CantidadFacturas = COUNT(*),
    @DocumentosInvalidos = SUM(CASE
        WHEN TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(DOCUMENTO)), N'')) IS NULL THEN 1
        ELSE 0
    END)
FROM dbo.BORR_NC_DET
WHERE ID_EMPRESA = @Empresa
  AND ID_BORRADOR = @IdBorrador;

SELECT @DocumentosDuplicados = COUNT(*)
FROM
(
    SELECT DOCUMENTO
    FROM dbo.BORR_NC_DET
    WHERE ID_EMPRESA = @Empresa
      AND ID_BORRADOR = @IdBorrador
    GROUP BY DOCUMENTO
    HAVING COUNT(*) > 1
) X;

/* 03D — La consulta HANA requiere DocNum numérico y una fila por factura. -- */
SELECT
    N'03D_RESUMEN' AS SECCION,
    N'Facturas del borrador' AS VALIDACION,
    1 AS MINIMO,
    @CantidadFacturas AS REAL,
    CASE WHEN @CantidadFacturas > 0 THEN N'OK' ELSE N'REVISAR' END AS RESULTADO
UNION ALL
SELECT N'03D_RESUMEN', N'DocNum no numéricos', 0, @DocumentosInvalidos,
       CASE WHEN @DocumentosInvalidos = 0 THEN N'OK' ELSE N'REVISAR' END
UNION ALL
SELECT N'03D_RESUMEN', N'DocNum repetidos dentro del borrador', 0, @DocumentosDuplicados,
       CASE WHEN @DocumentosDuplicados = 0 THEN N'OK' ELSE N'REVISAR' END;

DECLARE @Cliente nvarchar(20);
DECLARE @DocNums nvarchar(max);
DECLARE @ConsultaHana nvarchar(max);

SELECT @Cliente = ID_CLIENTE
FROM dbo.BORR_NC_ENC
WHERE ID_EMPRESA = @Empresa
  AND ID_BORRADOR = @IdBorrador;

SELECT @DocNums = STRING_AGG(CONVERT(nvarchar(max), DOCNUM), N',')
FROM
(
    SELECT DISTINCT TRY_CONVERT(bigint, DOCUMENTO) AS DOCNUM
    FROM dbo.BORR_NC_DET
    WHERE ID_EMPRESA = @Empresa
      AND ID_BORRADOR = @IdBorrador
      AND TRY_CONVERT(bigint, DOCUMENTO) IS NOT NULL
) D;

SET @ConsultaHana =
    N'SELECT ' + CHAR(13) + CHAR(10) +
    N'  ''' + REPLACE(@Empresa, N'''', N'''''') + N''' AS "EMPRESA",' + CHAR(13) + CHAR(10) +
    N'  H."DocEntry", H."DocNum", H."Series", H."CardCode", H."CardName",' + CHAR(13) + CHAR(10) +
    N'  H."CANCELED", H."DocStatus", H."DocCur",' + CHAR(13) + CHAR(10) +
    N'  L."LineNum", COALESCE(L."ItemCode", '''') AS "ItemCode",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."Dscription", '''') AS "Dscription",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."Quantity", 0) AS "Quantity",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."unitMsr", '''') AS "unitMsr",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."PriceBefDi", 0) AS "PriceBefDi",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."DiscPrcnt", 0) AS "DiscPrcnt",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."LineTotal", 0) AS "LineTotal",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."TotalFrgn", 0) AS "TotalFrgn",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."VatGroup", '''') AS "VatGroup",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."VatPrcnt", 0) AS "VatPrcnt",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."VatSum", 0) AS "VatSum",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."VatSumFrgn", 0) AS "VatSumFrgn",' + CHAR(13) + CHAR(10) +
    N'  COALESCE(L."WhsCode", '''') AS "WhsCode"' + CHAR(13) + CHAR(10) +
    N'FROM "' + REPLACE(@SchemaHana, N'"', N'""') + N'"."OINV" H' + CHAR(13) + CHAR(10) +
    N'INNER JOIN "' + REPLACE(@SchemaHana, N'"', N'""') + N'"."INV1" L' + CHAR(13) + CHAR(10) +
    N'        ON L."DocEntry" = H."DocEntry"' + CHAR(13) + CHAR(10) +
    N'WHERE H."CardCode" = ''' + REPLACE(@Cliente, N'''', N'''''') + N'''' + CHAR(13) + CHAR(10) +
    N'  AND H."DocNum" IN (' + COALESCE(@DocNums, N'NULL') + N')' + CHAR(13) + CHAR(10) +
    N'ORDER BY H."DocNum", H."DocEntry", L."LineNum";';

/* 03E — Copie la celda completa y ejecútela en SAP HANA ------------------- */
SELECT
    N'03E_CONSULTA_HANA' AS SECCION,
    @Empresa AS EMPRESA,
    @IdBorrador AS ID_BORRADOR,
    @Cliente AS CARDCODE,
    @DocNums AS DOCNUMS,
    @ConsultaHana AS CONSULTA_HANA;
GO

/* FIN: comparta 03A–03E y la salida de CONSULTA_HANA. */
