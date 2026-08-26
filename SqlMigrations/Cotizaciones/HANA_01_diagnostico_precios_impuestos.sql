/* =============================================================================
   SAP HANA — SEGUNDO DIAGNÓSTICO DE SOLO LECTURA PARA COTIZACIONES
   Motivo: el primer diagnóstico confirmó OSPP con miles de filas. Este script
   determina vigencias, escalas por cantidad, impuestos y si existe un schema
   SAP para EMPAQUES. No crea ni modifica objetos.
   ============================================================================= */

-- H10: contrato completo de las tres capas de precios especiales.
SELECT 'H10_COLUMNAS_PRECIO' AS "SECCION", "SCHEMA_NAME", "TABLE_NAME",
       "POSITION", "COLUMN_NAME", "DATA_TYPE_NAME", "LENGTH", "SCALE",
       "IS_NULLABLE"
FROM "SYS"."TABLE_COLUMNS"
WHERE "SCHEMA_NAME" IN ('SBO_GRACO', 'SBOESCOCESA', 'SBOBOLIK')
  AND "TABLE_NAME" IN ('OSPP', 'SPP1', 'SPP2')
ORDER BY "SCHEMA_NAME", "TABLE_NAME", "POSITION";

-- H11: volumen por capa y precios ligados a un cliente real.
SELECT 'GRACO' AS "EMPRESA",
       (SELECT COUNT(*) FROM "SBO_GRACO"."OSPP") AS "OSPP",
       (SELECT COUNT(*) FROM "SBO_GRACO"."SPP1") AS "SPP1",
       (SELECT COUNT(*) FROM "SBO_GRACO"."SPP2") AS "SPP2",
       (SELECT COUNT(*) FROM "SBO_GRACO"."OSPP" S
         INNER JOIN "SBO_GRACO"."OCRD" C ON C."CardCode"=S."CardCode")
         AS "OSPP_CLIENTE_REAL"
FROM DUMMY
UNION ALL
SELECT 'FAES',
       (SELECT COUNT(*) FROM "SBOESCOCESA"."OSPP"),
       (SELECT COUNT(*) FROM "SBOESCOCESA"."SPP1"),
       (SELECT COUNT(*) FROM "SBOESCOCESA"."SPP2"),
       (SELECT COUNT(*) FROM "SBOESCOCESA"."OSPP" S
         INNER JOIN "SBOESCOCESA"."OCRD" C ON C."CardCode"=S."CardCode")
FROM DUMMY
UNION ALL
SELECT 'BOLIK',
       (SELECT COUNT(*) FROM "SBOBOLIK"."OSPP"),
       (SELECT COUNT(*) FROM "SBOBOLIK"."SPP1"),
       (SELECT COUNT(*) FROM "SBOBOLIK"."SPP2"),
       (SELECT COUNT(*) FROM "SBOBOLIK"."OSPP" S
         INNER JOIN "SBOBOLIK"."OCRD" C ON C."CardCode"=S."CardCode")
FROM DUMMY;

-- H12: muestras OSPP completas. SP.* deja visibles Valid/ValidFrom/ValidTo,
-- Expand, SrcPrice y cualquier campo propio de la versión instalada.
SELECT 'GRACO' AS "EMPRESA", C."CardName", I."ItemName", SP.*
FROM "SBO_GRACO"."OSPP" SP
LEFT JOIN "SBO_GRACO"."OCRD" C ON C."CardCode"=SP."CardCode"
LEFT JOIN "SBO_GRACO"."OITM" I ON I."ItemCode"=SP."ItemCode"
ORDER BY SP."CardCode", SP."ItemCode" LIMIT 25;

SELECT 'FAES' AS "EMPRESA", C."CardName", I."ItemName", SP.*
FROM "SBOESCOCESA"."OSPP" SP
LEFT JOIN "SBOESCOCESA"."OCRD" C ON C."CardCode"=SP."CardCode"
LEFT JOIN "SBOESCOCESA"."OITM" I ON I."ItemCode"=SP."ItemCode"
ORDER BY SP."CardCode", SP."ItemCode" LIMIT 25;

SELECT 'BOLIK' AS "EMPRESA", C."CardName", I."ItemName", SP.*
FROM "SBOBOLIK"."OSPP" SP
LEFT JOIN "SBOBOLIK"."OCRD" C ON C."CardCode"=SP."CardCode"
LEFT JOIN "SBOBOLIK"."OITM" I ON I."ItemCode"=SP."ItemCode"
ORDER BY SP."CardCode", SP."ItemCode" LIMIT 25;

-- H13: muestras de periodos y cantidades. Si una tabla está vacía, devuelve 0 filas.
SELECT 'GRACO_SPP1' AS "SECCION", P.* FROM "SBO_GRACO"."SPP1" P
ORDER BY P."CardCode", P."ItemCode", P."LINENUM" LIMIT 25;
SELECT 'GRACO_SPP2' AS "SECCION", P.* FROM "SBO_GRACO"."SPP2" P
ORDER BY P."CardCode", P."ItemCode", P."SPP1LNum", P."SPP2LNum" LIMIT 25;

SELECT 'FAES_SPP1' AS "SECCION", P.* FROM "SBOESCOCESA"."SPP1" P
ORDER BY P."CardCode", P."ItemCode", P."LINENUM" LIMIT 25;
SELECT 'FAES_SPP2' AS "SECCION", P.* FROM "SBOESCOCESA"."SPP2" P
ORDER BY P."CardCode", P."ItemCode", P."SPP1LNum", P."SPP2LNum" LIMIT 25;

SELECT 'BOLIK_SPP1' AS "SECCION", P.* FROM "SBOBOLIK"."SPP1" P
ORDER BY P."CardCode", P."ItemCode", P."LINENUM" LIMIT 25;
SELECT 'BOLIK_SPP2' AS "SECCION", P.* FROM "SBOBOLIK"."SPP2" P
ORDER BY P."CardCode", P."ItemCode", P."SPP1LNum", P."SPP2LNum" LIMIT 25;

-- H14: tasas de impuesto realmente utilizadas por los artículos de venta.
SELECT 'GRACO' AS "EMPRESA", I."VatGourpSa" AS "GRUPO_IVA",
       T."Rate" AS "TASA", COUNT(*) AS "ARTICULOS"
FROM "SBO_GRACO"."OITM" I
LEFT JOIN "SBO_GRACO"."OVTG" T ON T."Code"=I."VatGourpSa"
WHERE I."SellItem"='Y' AND I."validFor"='Y'
GROUP BY I."VatGourpSa", T."Rate"
UNION ALL
SELECT 'FAES', I."VatGourpSa", T."Rate", COUNT(*)
FROM "SBOESCOCESA"."OITM" I
LEFT JOIN "SBOESCOCESA"."OVTG" T ON T."Code"=I."VatGourpSa"
WHERE I."SellItem"='Y' AND I."validFor"='Y'
GROUP BY I."VatGourpSa", T."Rate"
UNION ALL
SELECT 'BOLIK', I."VatGourpSa", T."Rate", COUNT(*)
FROM "SBOBOLIK"."OITM" I
LEFT JOIN "SBOBOLIK"."OVTG" T ON T."Code"=I."VatGourpSa"
WHERE I."SellItem"='Y' AND I."validFor"='Y'
GROUP BY I."VatGourpSa", T."Rate"
ORDER BY "EMPRESA", "GRUPO_IVA";

-- H15: descuento general configurado en clientes.
SELECT 'GRACO' AS "EMPRESA", COUNT(*) AS "CLIENTES",
       SUM(CASE WHEN COALESCE("Discount",0)<>0 THEN 1 ELSE 0 END) AS "CON_DESCUENTO",
       MIN(COALESCE("Discount",0)) AS "MINIMO",
       MAX(COALESCE("Discount",0)) AS "MAXIMO"
FROM "SBO_GRACO"."OCRD" WHERE "CardType"='C'
UNION ALL
SELECT 'FAES', COUNT(*),
       SUM(CASE WHEN COALESCE("Discount",0)<>0 THEN 1 ELSE 0 END),
       MIN(COALESCE("Discount",0)), MAX(COALESCE("Discount",0))
FROM "SBOESCOCESA"."OCRD" WHERE "CardType"='C'
UNION ALL
SELECT 'BOLIK', COUNT(*),
       SUM(CASE WHEN COALESCE("Discount",0)<>0 THEN 1 ELSE 0 END),
       MIN(COALESCE("Discount",0)), MAX(COALESCE("Discount",0))
FROM "SBOBOLIK"."OCRD" WHERE "CardType"='C';

-- H16: busca si EMPAQUES vive en este mismo HANA. ReciboCaja hoy la excluye,
-- pero Usuario_Empresa confirmó que sí existen asignaciones para esa empresa.
SELECT 'H16_SCHEMA_EMPAQUES' AS "SECCION", "SCHEMA_NAME"
FROM "SYS"."SCHEMAS"
WHERE UPPER("SCHEMA_NAME") LIKE '%EMPA%'
ORDER BY "SCHEMA_NAME";

SELECT 'H16_TABLAS_EMPAQUES' AS "SECCION", "SCHEMA_NAME", "TABLE_NAME"
FROM "SYS"."TABLES"
WHERE UPPER("SCHEMA_NAME") LIKE '%EMPA%'
  AND "TABLE_NAME" IN ('OITM','OCRD','ITM1','OITB','OSPP','SPP1','SPP2','OVTG')
ORDER BY "SCHEMA_NAME", "TABLE_NAME";
