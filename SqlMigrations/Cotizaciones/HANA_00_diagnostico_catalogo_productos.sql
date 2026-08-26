/* =============================================================================
   SAP HANA — DIAGNÓSTICO DE SOLO LECTURA PARA COTIZACIONES
   Ejecutar completo en HANA Studio/Database Explorer y enviar cada resultado.
   No crea ni modifica objetos.
   ============================================================================= */

-- H00: columnas reales de las tablas usadas por el contrato de productos.
SELECT 'H00_COLUMNAS' AS "SECCION", "SCHEMA_NAME", "TABLE_NAME",
       "POSITION", "COLUMN_NAME", "DATA_TYPE_NAME", "LENGTH", "SCALE",
       "IS_NULLABLE"
FROM "SYS"."TABLE_COLUMNS"
WHERE "SCHEMA_NAME" IN ('SBO_GRACO', 'SBOESCOCESA', 'SBOBOLIK')
  AND "TABLE_NAME" IN ('OITM', 'OITB', 'OCRD', 'ITM1', 'OSPP', 'SPP1', 'SPP2', 'OVTG')
  AND "COLUMN_NAME" IN
      ('ItemCode','ItemName','ItmsGrpCod','ItmsGrpNam','SalUnitMsr','InvntryUom',
       'SellItem','validFor','OnHand','IsCommited','OnOrder','CardCode','CardType',
       'ListNum','Currency','PriceList','Price','VatGourpSa','Code','Rate',
       'Discount','FromQty','Amount','UomEntry','LINENUM','SPP1LNum','SPP2LNum',
       'FromDate','ToDate','Valid','ValidFrom','ValidTo','EXPAND')
ORDER BY "SCHEMA_NAME", "TABLE_NAME", "POSITION";

-- H01: cobertura general. Usa subconsultas independientes; no hace el producto
-- cartesiano artículos × clientes.
SELECT 'GRACO' AS "EMPRESA",
       (SELECT COUNT(*) FROM "SBO_GRACO"."OITM" WHERE "SellItem"='Y' AND "validFor"='Y') AS "ARTICULOS_VENTA",
       (SELECT COUNT(*) FROM "SBO_GRACO"."OCRD" WHERE "CardType"='C') AS "CLIENTES",
       (SELECT COUNT(DISTINCT "ListNum") FROM "SBO_GRACO"."OCRD" WHERE "CardType"='C') AS "LISTAS_USADAS"
FROM DUMMY
UNION ALL
SELECT 'FAES',
       (SELECT COUNT(*) FROM "SBOESCOCESA"."OITM" WHERE "SellItem"='Y' AND "validFor"='Y'),
       (SELECT COUNT(*) FROM "SBOESCOCESA"."OCRD" WHERE "CardType"='C'),
       (SELECT COUNT(DISTINCT "ListNum") FROM "SBOESCOCESA"."OCRD" WHERE "CardType"='C')
FROM DUMMY
UNION ALL
SELECT 'BOLIK',
       (SELECT COUNT(*) FROM "SBOBOLIK"."OITM" WHERE "SellItem"='Y' AND "validFor"='Y'),
       (SELECT COUNT(*) FROM "SBOBOLIK"."OCRD" WHERE "CardType"='C'),
       (SELECT COUNT(DISTINCT "ListNum") FROM "SBOBOLIK"."OCRD" WHERE "CardType"='C')
FROM DUMMY;

-- H02: muestra clientes/listas/monedas por empresa. Son tres resultados
-- separados para conservar LIMIT sin depender de subconsultas con ORDER BY.
SELECT 'GRACO' AS "EMPRESA", "CardCode", "CardName", "ListNum", "Currency"
FROM "SBO_GRACO"."OCRD"
WHERE "CardType" = 'C'
ORDER BY "CardCode" LIMIT 10;

SELECT 'FAES' AS "EMPRESA", "CardCode", "CardName", "ListNum", "Currency"
FROM "SBOESCOCESA"."OCRD"
WHERE "CardType" = 'C'
ORDER BY "CardCode" LIMIT 10;

SELECT 'BOLIK' AS "EMPRESA", "CardCode", "CardName", "ListNum", "Currency"
FROM "SBOBOLIK"."OCRD"
WHERE "CardType" = 'C'
ORDER BY "CardCode" LIMIT 10;

-- H03: muestra productos y campos de inventario. Confirma los nombres usados por C#.
SELECT 'GRACO' AS "EMPRESA", I."ItemCode", I."ItemName", G."ItmsGrpNam",
       I."SalUnitMsr", I."InvntryUom", I."OnHand", I."IsCommited", I."OnOrder"
FROM "SBO_GRACO"."OITM" I
LEFT JOIN "SBO_GRACO"."OITB" G ON G."ItmsGrpCod" = I."ItmsGrpCod"
WHERE I."SellItem" = 'Y' AND I."validFor" = 'Y'
ORDER BY I."ItemCode" LIMIT 20;

-- H04: volumen de precios especiales; permite decidir si OSPP/SPP1 debe
-- complementar el precio de ITM1 en la siguiente iteración.
SELECT 'GRACO' AS "EMPRESA", COUNT(*) AS "PRECIOS_ESPECIALES" FROM "SBO_GRACO"."OSPP"
UNION ALL SELECT 'FAES', COUNT(*) FROM "SBOESCOCESA"."OSPP"
UNION ALL SELECT 'BOLIK', COUNT(*) FROM "SBOBOLIK"."OSPP";
