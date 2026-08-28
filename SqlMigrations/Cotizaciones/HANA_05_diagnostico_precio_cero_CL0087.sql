/* =============================================================================
   COTIZACIONES — DIAGNÓSTICO DE PRECIOS EN CERO

   Empresa : GRACO (schema SBO_GRACO)
   Cliente : CL0087 - CORPORACION RELROL, S.A.
   Moneda  : GTQ/QTZ

   Solo lectura. No crea ni modifica objetos o datos en SAP HANA.

   Objetivos:
     1) Confirmar lista, moneda y modo de precio del cliente.
     2) Identificar qué fuentes contienen precio positivo o cero.
     3) Comparar con los procedimientos usados por Pedidos_K66.
     4) Separar un error de precedencia de un artículo sin precio SAP.
   ============================================================================= */

/* H49 — Firma real del procedimiento utilizado por Pedidos_K66. */
SELECT 'H49_FIRMA_GET_PROCEDURE' AS "SECCION",
       "POSITION", "PARAMETER_NAME", "PARAMETER_TYPE", "DATA_TYPE_NAME",
       "LENGTH", "SCALE"
  FROM "SYS"."PROCEDURE_PARAMETERS"
 WHERE "SCHEMA_NAME"='SBO_GRACO'
   AND "PROCEDURE_NAME"='GET_PROCEDURE'
 ORDER BY "POSITION";

/* H50 — Configuración comercial del cliente. */
SELECT 'H50_CLIENTE' AS "SECCION",
       C."CardCode", C."CardName", C."ListNum", C."Currency",
       C."VatStatus", C."PriceMode", C."EffecPrice", C."EffcAllSrc",
       A."MainCurncy"
  FROM "SBO_GRACO"."OCRD" C
 CROSS JOIN "SBO_GRACO"."OADM" A
 WHERE C."CardCode"='CL0087'
   AND C."CardType"='C';

/* H51 — Primeros 100 artículos que muestra actualmente Cotizaciones.
   Incluye las tres monedas de ITM1 para detectar precios alternativos. */
SELECT 'H51_CATALOGO' AS "SECCION",
       I."ItemCode", I."ItemName", G."ItmsGrpNam", I."SellItem",
       I."SalUnitMsr", I."InvntryUom", C."ListNum",
       P."UomEntry", P."Price", P."Currency",
       P."AddPrice1", P."Currency1", P."AddPrice2", P."Currency2",
       CASE
         WHEN COALESCE(P."Price", 0)>0
          AND NULLIF(P."Currency", '')=
              COALESCE(NULLIF(NULLIF(C."Currency", ''), '##'),
                       NULLIF(A."MainCurncy", ''), 'QTZ')
           THEN P."Price"
         WHEN COALESCE(P."AddPrice1", 0)>0
          AND NULLIF(P."Currency1", '')=
              COALESCE(NULLIF(NULLIF(C."Currency", ''), '##'),
                       NULLIF(A."MainCurncy", ''), 'QTZ')
           THEN P."AddPrice1"
         WHEN COALESCE(P."AddPrice2", 0)>0
          AND NULLIF(P."Currency2", '')=
              COALESCE(NULLIF(NULLIF(C."Currency", ''), '##'),
                       NULLIF(A."MainCurncy", ''), 'QTZ')
           THEN P."AddPrice2"
         WHEN COALESCE(P."Price", 0)>0 THEN P."Price"
         WHEN COALESCE(P."AddPrice1", 0)>0 THEN P."AddPrice1"
         WHEN COALESCE(P."AddPrice2", 0)>0 THEN P."AddPrice2"
         ELSE 0
       END AS "PRECIO_LISTA_SELECCIONADO",
       I."OnHand", I."IsCommited"
  FROM "SBO_GRACO"."OITM" I
 INNER JOIN "SBO_GRACO"."OCRD" C
         ON C."CardCode"='CL0087' AND C."CardType"='C'
 CROSS JOIN "SBO_GRACO"."OADM" A
  LEFT JOIN "SBO_GRACO"."OITB" G
         ON G."ItmsGrpCod"=I."ItmsGrpCod"
  LEFT JOIN "SBO_GRACO"."ITM1" P
         ON P."ItemCode"=I."ItemCode" AND P."PriceList"=C."ListNum"
 WHERE I."SellItem"='Y'
 ORDER BY I."ItemCode"
 LIMIT 100;

/* H52 — Cobertura de precios en la lista asignada a CL0087. */
SELECT 'H52_COBERTURA_LISTA' AS "SECCION",
       C."ListNum",
       COUNT(*) AS "ARTICULOS_SELLITEM_Y",
       SUM(CASE WHEN COALESCE(P."Price", 0)>0 THEN 1 ELSE 0 END)
           AS "PRECIO_PRIMARIO_POSITIVO",
       SUM(CASE WHEN COALESCE(P."AddPrice1", 0)>0 THEN 1 ELSE 0 END)
           AS "PRECIO_ADICIONAL_1_POSITIVO",
       SUM(CASE WHEN COALESCE(P."AddPrice2", 0)>0 THEN 1 ELSE 0 END)
           AS "PRECIO_ADICIONAL_2_POSITIVO",
       SUM(CASE WHEN COALESCE(P."Price", 0)<=0
                      AND COALESCE(P."AddPrice1", 0)<=0
                      AND COALESCE(P."AddPrice2", 0)<=0
                THEN 1 ELSE 0 END) AS "SIN_PRECIO_EN_ITM1"
  FROM "SBO_GRACO"."OITM" I
 INNER JOIN "SBO_GRACO"."OCRD" C
         ON C."CardCode"='CL0087' AND C."CardType"='C'
  LEFT JOIN "SBO_GRACO"."ITM1" P
         ON P."ItemCode"=I."ItemCode" AND P."PriceList"=C."ListNum"
 WHERE I."SellItem"='Y'
 GROUP BY C."ListNum";

/* H53 — Artículos visibles en la captura y sus precios especiales.
   PT00062 se incluye como control porque tiene historial comercial reciente. */
SELECT 'H53_FUENTES_MUESTRA' AS "SECCION",
       I."ItemCode", I."ItemName", C."ListNum",
       P."Price" AS "ITM1_PRICE", P."Currency" AS "ITM1_CURRENCY",
       P."AddPrice1" AS "ITM1_ADD1", P."Currency1" AS "ITM1_CURR1",
       P."AddPrice2" AS "ITM1_ADD2", P."Currency2" AS "ITM1_CURR2",
       EC."Price" AS "OSPP_CLIENTE_PRICE",
       EC."Currency" AS "OSPP_CLIENTE_CURRENCY",
       EC."Discount" AS "OSPP_CLIENTE_DISCOUNT",
       WL."Price" AS "OSPP_LISTA_PRICE",
       WL."Currency" AS "OSPP_LISTA_CURRENCY",
       WL."Discount" AS "OSPP_LISTA_DISCOUNT"
  FROM "SBO_GRACO"."OITM" I
 INNER JOIN "SBO_GRACO"."OCRD" C
         ON C."CardCode"='CL0087' AND C."CardType"='C'
  LEFT JOIN "SBO_GRACO"."ITM1" P
         ON P."ItemCode"=I."ItemCode" AND P."PriceList"=C."ListNum"
  LEFT JOIN "SBO_GRACO"."OSPP" EC
         ON EC."ItemCode"=I."ItemCode" AND EC."CardCode"=C."CardCode"
  LEFT JOIN "SBO_GRACO"."OSPP" WL
         ON WL."ItemCode"=I."ItemCode"
        AND WL."CardCode"='*' || TO_NVARCHAR(C."ListNum")
 WHERE I."ItemCode" IN
       ('HERR000089','MP00071','MP00081','MP00123','MP00164',
        'MP00181','MP00183','MP00184','PT00062')
 ORDER BY I."ItemCode";

/* H54 — Últimos precios realmente documentados para CL0087.
   Es evidencia diagnóstica; NO se propone utilizarlos como fallback. */
SELECT 'H54_DOCUMENTOS_RECIENTES' AS "SECCION",
       H."DocDate", H."DocNum", H."DocCur", L."ItemCode", L."Quantity",
       L."ListNum", L."Price" AS "PRECIO_DOCUMENTO",
       L."PriceBefDi", L."DiscPrcnt", L."VatPrcnt", L."PriceAfVAT",
       P."Price" AS "ITM1_ACTUAL", P."Currency" AS "MONEDA_ITM1"
  FROM "SBO_GRACO"."OINV" H
 INNER JOIN "SBO_GRACO"."INV1" L ON L."DocEntry"=H."DocEntry"
  LEFT JOIN "SBO_GRACO"."ITM1" P
         ON P."ItemCode"=L."ItemCode" AND P."PriceList"=L."ListNum"
 WHERE H."CardCode"='CL0087'
   AND COALESCE(H."CANCELED", 'N')='N'
   AND L."ItemCode" IS NOT NULL
 ORDER BY H."DocDate" DESC, H."DocNum" DESC, L."LineNum"
 LIMIT 50;

/* H55–H57 — Misma lógica productiva utilizada por Pedidos_K66.
   Si algún CALL falla, compartir también el mensaje de error completo. */
CALL "SBO_GRACO"."GET_PROCEDURE"
    ('itemLibre','MP00071','CL0087','','','','','','','','');

CALL "SBO_GRACO"."GET_PROCEDURE"
    ('itemCantidad','MP00071','1','CL0087','','','','','','','');

CALL "SBO_GRACO"."GET_PROCEDURE"
    ('itemCantidad','PT00062','1','CL0087','','','','','','','');
