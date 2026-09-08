/* =============================================================================
   SAP HANA — DIAGNÓSTICO DE DOCUMENTOS PREVIOS DE BORRADORES NC

   Objetivo
   --------
   Confirmar cómo INF_VRC_FACRNC identifica notas de crédito y devoluciones,
   y validar encabezados/renglones estándar antes de implementar su detalle.

   Seguridad
   ---------
   SOLO LECTURA. Este archivo contiene únicamente SELECT.
   No crea ni modifica objetos o datos en SAP HANA ni en POS-SmartK66.

   Caso de la captura
   ------------------
   Empresa/schema : GRACO / SBO_GRACO
   Factura        : 1037333
   Documento SAP  : 2000434

   Si el caso pertenece a otra empresa, reemplace SBO_GRACO en todo el archivo:
     FAES  -> SBOESCOCESA
     BOLIK -> SBOBOLIK

   Para otro caso, reemplace también 1037333 y 2000434.

   Ejecución
   ---------
   1. Ejecute el archivo COMPLETO en SAP HANA Studio/Database Explorer.
   2. Comparta todos los resultados H00 a H09, incluidos grids vacíos.
   3. Si una sección produce error, comparta el texto exacto y continúe con
      las secciones restantes.
   ============================================================================= */

/* H00 — Entorno ------------------------------------------------------------- */
SELECT
    'H00_ENTORNO' AS "SECCION",
    CURRENT_USER AS "USUARIO_HANA",
    CURRENT_SCHEMA AS "SCHEMA_ACTUAL",
    CURRENT_TIMESTAMP AS "FECHA_DIAGNOSTICO"
FROM DUMMY;

/* H01 — Contrato real de la vista personalizada ---------------------------- */
SELECT
    'H01_COLUMNAS_VISTA' AS "SECCION",
    "SCHEMA_NAME",
    "VIEW_NAME",
    "POSITION",
    "COLUMN_NAME",
    "DATA_TYPE_NAME",
    "LENGTH",
    "SCALE",
    "IS_NULLABLE"
FROM "SYS"."VIEW_COLUMNS"
WHERE "SCHEMA_NAME" = 'SBO_GRACO'
  AND "VIEW_NAME" = 'INF_VRC_FACRNC'
ORDER BY "POSITION";

/* H02 — Columnas necesarias de los documentos estándar -------------------- */
SELECT
    'H02_COLUMNAS_SAP' AS "SECCION",
    "SCHEMA_NAME",
    "TABLE_NAME",
    "POSITION",
    "COLUMN_NAME",
    "DATA_TYPE_NAME",
    "LENGTH",
    "SCALE",
    "IS_NULLABLE"
FROM "SYS"."TABLE_COLUMNS"
WHERE "SCHEMA_NAME" = 'SBO_GRACO'
  AND "TABLE_NAME" IN ('OINV', 'ORIN', 'RIN1', 'ORDN', 'RDN1')
  AND "COLUMN_NAME" IN
      ('DocEntry','DocNum','ObjType','CANCELED','DocStatus','DocType','DocDate',
       'TaxDate','CardCode','CardName','NumAtCard','DocCur','DocRate','DocTotal',
       'DocTotalFC','JrnlMemo','Comments','U_SERIE_FACE','U_NUMERO_DOCUMENTO',
       'U_FACE_PDFFILE','LineNum','BaseType','BaseEntry','BaseLine','BaseRef',
       'ItemCode','Dscription','Quantity','unitMsr','WhsCode','PriceBefDi',
       'DiscPrcnt','LineTotal','TotalFrgn','VatGroup','VatPrcnt','VatSum',
       'VatSumFrgn','LineStatus')
ORDER BY "TABLE_NAME", "POSITION";

/* H03 — Filas que hoy alimentan el aviso del borrador ---------------------- */
SELECT
    'H03_VISTA_FACTURA' AS "SECCION",
    "Tipo",
    "Factura",
    "Nota",
    "DocDate",
    "CardCode",
    "CardName",
    "DocCur",
    "DocTotal",
    "JrnlMemo",
    "Comments"
FROM "SBO_GRACO"."INF_VRC_FACRNC"
WHERE "Factura" = 1037333
ORDER BY "DocDate" DESC, "Nota", "Tipo";

/* H04 — Tipos reales presentes en la vista -------------------------------- */
SELECT
    'H04_TIPOS_VISTA' AS "SECCION",
    COALESCE("Tipo", '<NULL>') AS "TIPO",
    COUNT(*) AS "CANTIDAD",
    MIN("DocDate") AS "FECHA_MINIMA",
    MAX("DocDate") AS "FECHA_MAXIMA"
FROM "SBO_GRACO"."INF_VRC_FACRNC"
WHERE "Nota" IS NOT NULL
GROUP BY "Tipo"
ORDER BY "TIPO";

/* H05 — Localizar el documento mostrado: ORIN o ORDN ----------------------- */
SELECT
    'H05_ENCABEZADO' AS "SECCION",
    'NOTA_CREDITO' AS "CLASE_DOCUMENTO",
    H."DocEntry",
    H."DocNum",
    H."ObjType",
    H."CANCELED",
    H."DocStatus",
    H."DocType",
    H."DocDate",
    H."TaxDate",
    H."CardCode",
    H."CardName",
    H."NumAtCard",
    H."DocCur",
    H."DocRate",
    H."DocTotal",
    H."DocTotalFC",
    H."JrnlMemo",
    H."Comments"
FROM "SBO_GRACO"."ORIN" H
WHERE H."DocNum" = 2000434
UNION ALL
SELECT
    'H05_ENCABEZADO',
    'DEVOLUCION',
    H."DocEntry",
    H."DocNum",
    H."ObjType",
    H."CANCELED",
    H."DocStatus",
    H."DocType",
    H."DocDate",
    H."TaxDate",
    H."CardCode",
    H."CardName",
    H."NumAtCard",
    H."DocCur",
    H."DocRate",
    H."DocTotal",
    H."DocTotalFC",
    H."JrnlMemo",
    H."Comments"
FROM "SBO_GRACO"."ORDN" H
WHERE H."DocNum" = 2000434
ORDER BY "CLASE_DOCUMENTO";

/* H06 — Renglones si es una nota de crédito (ORIN + RIN1) ----------------- */
SELECT
    'H06_LINEAS_NOTA_CREDITO' AS "SECCION",
    H."DocEntry",
    H."DocNum",
    H."ObjType",
    L."LineNum",
    L."BaseType",
    L."BaseEntry",
    L."BaseLine",
    L."BaseRef",
    L."ItemCode",
    L."Dscription",
    L."Quantity",
    L."unitMsr",
    L."WhsCode",
    L."PriceBefDi",
    L."DiscPrcnt",
    L."LineTotal",
    L."TotalFrgn",
    L."VatGroup",
    L."VatPrcnt",
    L."VatSum",
    L."VatSumFrgn",
    L."LineStatus"
FROM "SBO_GRACO"."ORIN" H
INNER JOIN "SBO_GRACO"."RIN1" L
        ON L."DocEntry" = H."DocEntry"
WHERE H."DocNum" = 2000434
ORDER BY L."LineNum";

/* H07 — Renglones si es una devolución (ORDN + RDN1) ---------------------- */
SELECT
    'H07_LINEAS_DEVOLUCION' AS "SECCION",
    H."DocEntry",
    H."DocNum",
    H."ObjType",
    L."LineNum",
    L."BaseType",
    L."BaseEntry",
    L."BaseLine",
    L."BaseRef",
    L."ItemCode",
    L."Dscription",
    L."Quantity",
    L."unitMsr",
    L."WhsCode",
    L."PriceBefDi",
    L."DiscPrcnt",
    L."LineTotal",
    L."TotalFrgn",
    L."VatGroup",
    L."VatPrcnt",
    L."VatSum",
    L."VatSumFrgn",
    L."LineStatus"
FROM "SBO_GRACO"."ORDN" H
INNER JOIN "SBO_GRACO"."RDN1" L
        ON L."DocEntry" = H."DocEntry"
WHERE H."DocNum" = 2000434
ORDER BY L."LineNum";

/* H08 — Resolver cada fila de la vista contra su encabezado estándar ------- */
SELECT
    'H08_RESOLUCION' AS "SECCION",
    V."Tipo",
    V."Factura",
    V."Nota",
    V."DocDate" AS "FECHA_VISTA",
    C."DocEntry" AS "ORIN_DOCENTRY",
    C."CANCELED" AS "ORIN_CANCELADA",
    D."DocEntry" AS "ORDN_DOCENTRY",
    D."CANCELED" AS "ORDN_CANCELADA",
    CASE
        WHEN C."DocEntry" IS NOT NULL AND D."DocEntry" IS NULL THEN 'NOTA_CREDITO'
        WHEN C."DocEntry" IS NULL AND D."DocEntry" IS NOT NULL THEN 'DEVOLUCION'
        WHEN C."DocEntry" IS NOT NULL AND D."DocEntry" IS NOT NULL THEN 'AMBIGUO'
        ELSE 'NO_ENCONTRADO'
    END AS "CLASE_RESUELTA"
FROM "SBO_GRACO"."INF_VRC_FACRNC" V
LEFT JOIN "SBO_GRACO"."ORIN" C
       ON TO_NVARCHAR(C."DocNum") = TO_NVARCHAR(V."Nota")
LEFT JOIN "SBO_GRACO"."ORDN" D
       ON TO_NVARCHAR(D."DocNum") = TO_NVARCHAR(V."Nota")
WHERE V."Factura" = 1037333
ORDER BY V."DocDate" DESC, V."Nota", V."Tipo";

/* H09 — Detectar duplicados del caso y su significado ---------------------- */
SELECT
    'H09_DUPLICADOS' AS "SECCION",
    "Factura",
    "Nota",
    COUNT(*) AS "FILAS",
    COUNT(DISTINCT "Tipo") AS "TIPOS_DISTINTOS",
    MIN("DocDate") AS "FECHA_MINIMA",
    MAX("DocDate") AS "FECHA_MAXIMA",
    MIN("DocTotal") AS "TOTAL_MINIMO",
    MAX("DocTotal") AS "TOTAL_MAXIMO"
FROM "SBO_GRACO"."INF_VRC_FACRNC"
WHERE "Factura" = 1037333
  AND "Nota" IS NOT NULL
GROUP BY "Factura", "Nota"
ORDER BY "Nota";

/* FIN — Comparta H00 a H09, incluidos resultados vacíos y mensajes de error. */
