/* =============================================================================
   SAP HANA — DIAGNÓSTICO FINAL DE IVA Y PRECIO EFECTIVO PARA COTIZACIONES

   Objetivo:
   1. Identificar de dónde toma SAP el grupo/tasa de IVA cuando OITM.VatGourpSa
      está vacío.
   2. Confirmar la configuración que decide si SAP usa el precio de lista, el
      especial, el menor o el mayor precio disponible.
   3. Comparar esa configuración con cotizaciones y facturas reales recientes.

   Solo lectura: todos los bloques son SELECT. No crea ni modifica objetos.
   Ejecutar el archivo completo en SAP HANA Studio y compartir todos los grids.
   ============================================================================= */

-- H20: contrato de las columnas que intervienen en IVA, precio efectivo y
-- descuentos. Este bloque permite interpretar de forma inequívoca los demás.
SELECT 'H20_COLUMNAS_CONFIG' AS "SECCION", "SCHEMA_NAME", "TABLE_NAME",
       "POSITION", "COLUMN_NAME", "DATA_TYPE_NAME", "LENGTH", "SCALE",
       "IS_NULLABLE"
FROM "SYS"."TABLE_COLUMNS"
WHERE "SCHEMA_NAME" IN ('SBO_GRACO', 'SBOESCOCESA', 'SBOBOLIK')
  AND "TABLE_NAME" IN
      ('OCRD','OITM','OWHS','OADM','OVTG','OSTC',
       'OEDG','EDG1','OSPG','OINV','INV1','OQUT','QUT1')
  AND "COLUMN_NAME" IN
      ('CardCode','CardType','ListNum','Currency','Discount','GroupCode',
       'VatGroup','ECVatGroup','VatStatus','PriceMode','EffecPrice','EffcAllSrc',
       'ItemCode','DfltWH','VatGourpSa','ItmsGrpCod','UgpEntry',
       'WhsCode','DflTaxCode','TaxCodeCst','DfSVatExmp','MainCurncy',
       'Code','Name','Category','Rate','Inactive','TaxType',
       'AbsEntry','ObjType','Type','BaseObject','DiscountObj','DiscountType',
       'DocEntry','DocNum','DocDate','CANCELED','DocCur','Quantity',
       'PriceBefDi','Price','DiscPrcnt','VatPrcnt','TaxCode','TaxStatus',
       'PriceAfVAT','LineVat','UomEntry','unitMsr','WhsCode')
ORDER BY "SCHEMA_NAME", "TABLE_NAME", "POSITION";

-- H21: opciones de determinación de precio e IVA configuradas por cliente.
-- EffecPrice: D=precio predeterminado, L=menor, H=mayor (según SAP B1).
-- EffcAllSrc indica si SAP compara todas las fuentes de precio disponibles.
SELECT 'GRACO' AS "EMPRESA", COALESCE("VatGroup", '') AS "IVA_CLIENTE",
       COALESCE("ECVatGroup", '') AS "IVA_UE_CLIENTE",
       COALESCE("VatStatus", '') AS "ESTADO_IVA",
       COALESCE("PriceMode", '') AS "MODO_PRECIO",
       COALESCE("EffecPrice", '') AS "PRECIO_EFECTIVO",
       COALESCE("EffcAllSrc", '') AS "TODAS_FUENTES",
       COUNT(*) AS "CLIENTES"
FROM "SBO_GRACO"."OCRD"
WHERE "CardType"='C'
GROUP BY "VatGroup", "ECVatGroup", "VatStatus", "PriceMode",
         "EffecPrice", "EffcAllSrc"
UNION ALL
SELECT 'FAES', COALESCE("VatGroup", ''), COALESCE("ECVatGroup", ''),
       COALESCE("VatStatus", ''), COALESCE("PriceMode", ''),
       COALESCE("EffecPrice", ''), COALESCE("EffcAllSrc", ''), COUNT(*)
FROM "SBOESCOCESA"."OCRD"
WHERE "CardType"='C'
GROUP BY "VatGroup", "ECVatGroup", "VatStatus", "PriceMode",
         "EffecPrice", "EffcAllSrc"
UNION ALL
SELECT 'BOLIK', COALESCE("VatGroup", ''), COALESCE("ECVatGroup", ''),
       COALESCE("VatStatus", ''), COALESCE("PriceMode", ''),
       COALESCE("EffecPrice", ''), COALESCE("EffcAllSrc", ''), COUNT(*)
FROM "SBOBOLIK"."OCRD"
WHERE "CardType"='C'
GROUP BY "VatGroup", "ECVatGroup", "VatStatus", "PriceMode",
         "EffecPrice", "EffcAllSrc"
ORDER BY "EMPRESA", "CLIENTES" DESC;

-- H22: defaults fiscales de compañía y almacén. El default del almacén puede
-- ser la fuente cuando el artículo no trae VatGourpSa.
SELECT 'GRACO' AS "EMPRESA", "DflTaxCode" AS "IVA_PREDETERMINADO",
       "TaxCodeCst" AS "IVA_COMPRAS", "DfSVatExmp" AS "IVA_EXENTO_VENTAS",
       "MainCurncy" AS "MONEDA_LOCAL"
FROM "SBO_GRACO"."OADM"
UNION ALL
SELECT 'FAES', "DflTaxCode", "TaxCodeCst", "DfSVatExmp", "MainCurncy"
FROM "SBOESCOCESA"."OADM"
UNION ALL
SELECT 'BOLIK', "DflTaxCode", "TaxCodeCst", "DfSVatExmp", "MainCurncy"
FROM "SBOBOLIK"."OADM";

SELECT 'GRACO' AS "EMPRESA", "WhsCode" AS "ALMACEN",
       COALESCE("VatGroup", '') AS "GRUPO_IVA", COUNT(*) AS "REGISTROS"
FROM "SBO_GRACO"."OWHS"
GROUP BY "WhsCode", "VatGroup"
UNION ALL
SELECT 'FAES', "WhsCode", COALESCE("VatGroup", ''), COUNT(*)
FROM "SBOESCOCESA"."OWHS"
GROUP BY "WhsCode", "VatGroup"
UNION ALL
SELECT 'BOLIK', "WhsCode", COALESCE("VatGroup", ''), COUNT(*)
FROM "SBOBOLIK"."OWHS"
GROUP BY "WhsCode", "VatGroup"
ORDER BY "EMPRESA", "ALMACEN";

-- H23: catálogo completo de grupos de IVA. SELECT * evita asumir nombres de
-- columnas que cambian entre localizaciones/versiones de SAP Business One.
SELECT 'GRACO_OVTG' AS "SECCION", T.* FROM "SBO_GRACO"."OVTG" T
ORDER BY T."Code" LIMIT 100;
SELECT 'FAES_OVTG' AS "SECCION", T.* FROM "SBOESCOCESA"."OVTG" T
ORDER BY T."Code" LIMIT 100;
SELECT 'BOLIK_OVTG' AS "SECCION", T.* FROM "SBOBOLIK"."OVTG" T
ORDER BY T."Code" LIMIT 100;

SELECT 'GRACO_OSTC' AS "SECCION", T.* FROM "SBO_GRACO"."OSTC" T
LIMIT 100;
SELECT 'FAES_OSTC' AS "SECCION", T.* FROM "SBOESCOCESA"."OSTC" T
LIMIT 100;
SELECT 'BOLIK_OSTC' AS "SECCION", T.* FROM "SBOBOLIK"."OSTC" T
LIMIT 100;

-- H24: IVA usado realmente en facturas de los últimos 24 meses. Se agrupa por
-- configuración del cliente y por el grupo/tasa que SAP grabó en la línea.
SELECT 'GRACO' AS "EMPRESA", COALESCE(C."VatGroup", '') AS "IVA_CLIENTE",
       COALESCE(C."ECVatGroup", '') AS "IVA_UE_CLIENTE",
       COALESCE(L."VatGroup", '') AS "IVA_LINEA", L."VatPrcnt" AS "TASA",
       COUNT(*) AS "LINEAS", MAX(H."DocDate") AS "ULTIMA_FECHA"
FROM "SBO_GRACO"."OINV" H
INNER JOIN "SBO_GRACO"."INV1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBO_GRACO"."OCRD" C ON C."CardCode"=H."CardCode"
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
GROUP BY C."VatGroup", C."ECVatGroup", L."VatGroup", L."VatPrcnt"
UNION ALL
SELECT 'FAES', COALESCE(C."VatGroup", ''), COALESCE(C."ECVatGroup", ''),
       COALESCE(L."VatGroup", ''), L."VatPrcnt", COUNT(*), MAX(H."DocDate")
FROM "SBOESCOCESA"."OINV" H
INNER JOIN "SBOESCOCESA"."INV1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBOESCOCESA"."OCRD" C ON C."CardCode"=H."CardCode"
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
GROUP BY C."VatGroup", C."ECVatGroup", L."VatGroup", L."VatPrcnt"
UNION ALL
SELECT 'BOLIK', COALESCE(C."VatGroup", ''), COALESCE(C."ECVatGroup", ''),
       COALESCE(L."VatGroup", ''), L."VatPrcnt", COUNT(*), MAX(H."DocDate")
FROM "SBOBOLIK"."OINV" H
INNER JOIN "SBOBOLIK"."INV1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBOBOLIK"."OCRD" C ON C."CardCode"=H."CardCode"
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
GROUP BY C."VatGroup", C."ECVatGroup", L."VatGroup", L."VatPrcnt"
ORDER BY "EMPRESA", "LINEAS" DESC;

-- H25: IVA usado en cotizaciones SAP recientes. Si el sistema no utiliza OQUT,
-- los bloques correspondientes simplemente devuelven cero filas.
SELECT 'GRACO' AS "EMPRESA", COALESCE(C."VatGroup", '') AS "IVA_CLIENTE",
       COALESCE(C."ECVatGroup", '') AS "IVA_UE_CLIENTE",
       COALESCE(L."VatGroup", '') AS "IVA_LINEA", L."VatPrcnt" AS "TASA",
       COUNT(*) AS "LINEAS", MAX(H."DocDate") AS "ULTIMA_FECHA"
FROM "SBO_GRACO"."OQUT" H
INNER JOIN "SBO_GRACO"."QUT1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBO_GRACO"."OCRD" C ON C."CardCode"=H."CardCode"
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
GROUP BY C."VatGroup", C."ECVatGroup", L."VatGroup", L."VatPrcnt"
UNION ALL
SELECT 'FAES', COALESCE(C."VatGroup", ''), COALESCE(C."ECVatGroup", ''),
       COALESCE(L."VatGroup", ''), L."VatPrcnt", COUNT(*), MAX(H."DocDate")
FROM "SBOESCOCESA"."OQUT" H
INNER JOIN "SBOESCOCESA"."QUT1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBOESCOCESA"."OCRD" C ON C."CardCode"=H."CardCode"
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
GROUP BY C."VatGroup", C."ECVatGroup", L."VatGroup", L."VatPrcnt"
UNION ALL
SELECT 'BOLIK', COALESCE(C."VatGroup", ''), COALESCE(C."ECVatGroup", ''),
       COALESCE(L."VatGroup", ''), L."VatPrcnt", COUNT(*), MAX(H."DocDate")
FROM "SBOBOLIK"."OQUT" H
INNER JOIN "SBOBOLIK"."QUT1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBOBOLIK"."OCRD" C ON C."CardCode"=H."CardCode"
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
GROUP BY C."VatGroup", C."ECVatGroup", L."VatGroup", L."VatPrcnt"
ORDER BY "EMPRESA", "LINEAS" DESC;

-- H26: cuánto intervienen los grupos de descuento en la jerarquía de precio.
SELECT 'GRACO' AS "EMPRESA",
       (SELECT COUNT(*) FROM "SBO_GRACO"."OEDG") AS "OEDG",
       (SELECT COUNT(*) FROM "SBO_GRACO"."EDG1") AS "EDG1",
       (SELECT COUNT(*) FROM "SBO_GRACO"."OSPG") AS "OSPG"
FROM DUMMY
UNION ALL
SELECT 'FAES',
       (SELECT COUNT(*) FROM "SBOESCOCESA"."OEDG"),
       (SELECT COUNT(*) FROM "SBOESCOCESA"."EDG1"),
       (SELECT COUNT(*) FROM "SBOESCOCESA"."OSPG")
FROM DUMMY
UNION ALL
SELECT 'BOLIK',
       (SELECT COUNT(*) FROM "SBOBOLIK"."OEDG"),
       (SELECT COUNT(*) FROM "SBOBOLIK"."EDG1"),
       (SELECT COUNT(*) FROM "SBOBOLIK"."OSPG")
FROM DUMMY;

-- H27: comparación de líneas reales recientes contra ITM1 y las dos clases de
-- OSPP: especial del cliente y especial para todos los clientes de la lista.
-- Las muestras permiten comprobar si PriceBefDi coincide con la fuente que SAP
-- eligió antes de reproducir esa decisión en el módulo web.
SELECT 'GRACO' AS "EMPRESA", H."DocDate", H."DocNum", H."CardCode",
       C."ListNum", H."DocCur", L."ItemCode", L."Quantity",
       P."Price" AS "PRECIO_ITM1", P."Currency" AS "MONEDA_ITM1",
       E."Price" AS "PRECIO_ESPECIAL_CLIENTE",
       E."Discount" AS "DESC_ESPECIAL_CLIENTE",
       W."Price" AS "PRECIO_ESPECIAL_LISTA",
       W."Discount" AS "DESC_ESPECIAL_LISTA",
       L."PriceBefDi" AS "PRECIO_DOCUMENTO",
       L."DiscPrcnt" AS "DESC_DOCUMENTO", L."Price" AS "NETO_DOCUMENTO"
FROM "SBO_GRACO"."OINV" H
INNER JOIN "SBO_GRACO"."INV1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBO_GRACO"."OCRD" C ON C."CardCode"=H."CardCode"
LEFT JOIN "SBO_GRACO"."ITM1" P
       ON P."ItemCode"=L."ItemCode" AND P."PriceList"=C."ListNum"
LEFT JOIN "SBO_GRACO"."OSPP" E
       ON E."ItemCode"=L."ItemCode" AND E."CardCode"=H."CardCode"
LEFT JOIN "SBO_GRACO"."OSPP" W
       ON W."ItemCode"=L."ItemCode"
      AND W."CardCode"='*' || TO_NVARCHAR(C."ListNum")
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
  AND (E."ItemCode" IS NOT NULL OR W."ItemCode" IS NOT NULL)
ORDER BY H."DocDate" DESC, H."DocNum" DESC LIMIT 50;

SELECT 'FAES' AS "EMPRESA", H."DocDate", H."DocNum", H."CardCode",
       C."ListNum", H."DocCur", L."ItemCode", L."Quantity",
       P."Price" AS "PRECIO_ITM1", P."Currency" AS "MONEDA_ITM1",
       E."Price" AS "PRECIO_ESPECIAL_CLIENTE",
       E."Discount" AS "DESC_ESPECIAL_CLIENTE",
       W."Price" AS "PRECIO_ESPECIAL_LISTA",
       W."Discount" AS "DESC_ESPECIAL_LISTA",
       L."PriceBefDi" AS "PRECIO_DOCUMENTO",
       L."DiscPrcnt" AS "DESC_DOCUMENTO", L."Price" AS "NETO_DOCUMENTO"
FROM "SBOESCOCESA"."OINV" H
INNER JOIN "SBOESCOCESA"."INV1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBOESCOCESA"."OCRD" C ON C."CardCode"=H."CardCode"
LEFT JOIN "SBOESCOCESA"."ITM1" P
       ON P."ItemCode"=L."ItemCode" AND P."PriceList"=C."ListNum"
LEFT JOIN "SBOESCOCESA"."OSPP" E
       ON E."ItemCode"=L."ItemCode" AND E."CardCode"=H."CardCode"
LEFT JOIN "SBOESCOCESA"."OSPP" W
       ON W."ItemCode"=L."ItemCode"
      AND W."CardCode"='*' || TO_NVARCHAR(C."ListNum")
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
  AND (E."ItemCode" IS NOT NULL OR W."ItemCode" IS NOT NULL)
ORDER BY H."DocDate" DESC, H."DocNum" DESC LIMIT 50;

SELECT 'BOLIK' AS "EMPRESA", H."DocDate", H."DocNum", H."CardCode",
       C."ListNum", H."DocCur", L."ItemCode", L."Quantity",
       P."Price" AS "PRECIO_ITM1", P."Currency" AS "MONEDA_ITM1",
       E."Price" AS "PRECIO_ESPECIAL_CLIENTE",
       E."Discount" AS "DESC_ESPECIAL_CLIENTE",
       W."Price" AS "PRECIO_ESPECIAL_LISTA",
       W."Discount" AS "DESC_ESPECIAL_LISTA",
       L."PriceBefDi" AS "PRECIO_DOCUMENTO",
       L."DiscPrcnt" AS "DESC_DOCUMENTO", L."Price" AS "NETO_DOCUMENTO"
FROM "SBOBOLIK"."OINV" H
INNER JOIN "SBOBOLIK"."INV1" L ON L."DocEntry"=H."DocEntry"
INNER JOIN "SBOBOLIK"."OCRD" C ON C."CardCode"=H."CardCode"
LEFT JOIN "SBOBOLIK"."ITM1" P
       ON P."ItemCode"=L."ItemCode" AND P."PriceList"=C."ListNum"
LEFT JOIN "SBOBOLIK"."OSPP" E
       ON E."ItemCode"=L."ItemCode" AND E."CardCode"=H."CardCode"
LEFT JOIN "SBOBOLIK"."OSPP" W
       ON W."ItemCode"=L."ItemCode"
      AND W."CardCode"='*' || TO_NVARCHAR(C."ListNum")
WHERE H."CANCELED"='N' AND H."DocDate">=ADD_MONTHS(CURRENT_DATE,-24)
  AND L."ItemCode" IS NOT NULL
  AND (E."ItemCode" IS NOT NULL OR W."ItemCode" IS NOT NULL)
ORDER BY H."DocDate" DESC, H."DocNum" DESC LIMIT 50;
