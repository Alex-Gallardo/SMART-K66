/* =============================================================================
   COTIZACIONES — VERIFICACIÓN DE ARTÍCULOS DISPONIBLES PARA VENTA

   Solo lectura. No crea ni modifica objetos en SAP HANA.

   Regla funcional confirmada:
     - Se muestra cualquier código, nombre o grupo cuando OITM."SellItem"='Y'.
     - No se filtra por prefijo, grupo, existencia ni OITM."validFor".

   Resultados esperados:
     1) Resumen por empresa, incluyendo artículos visibles con stock cero.
     2) Muestras de artículos que la aplicación sí mostrará.
     3) Muestras de artículos que la aplicación excluirá.
   ============================================================================= */

SELECT 'GRACO' AS "EMPRESA",
       COUNT(*) AS "TOTAL_OITM",
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y' THEN 1 ELSE 0 END)
           AS "MOSTRADOS_SELLITEM_Y",
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y'
                     AND COALESCE("OnHand", 0)=0 THEN 1 ELSE 0 END)
           AS "MOSTRADOS_STOCK_CERO",
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y'
                     AND COALESCE("OnHand", 0)-COALESCE("IsCommited", 0)<=0
                THEN 1 ELSE 0 END) AS "MOSTRADOS_DISPONIBLE_NO_POSITIVO",
       SUM(CASE WHEN COALESCE("SellItem", 'N')<>'Y' THEN 1 ELSE 0 END)
           AS "EXCLUIDOS_SELLITEM_NO_Y"
  FROM "SBO_GRACO"."OITM"
UNION ALL
SELECT 'FAES', COUNT(*),
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y' THEN 1 ELSE 0 END),
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y'
                     AND COALESCE("OnHand", 0)=0 THEN 1 ELSE 0 END),
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y'
                     AND COALESCE("OnHand", 0)-COALESCE("IsCommited", 0)<=0
                THEN 1 ELSE 0 END),
       SUM(CASE WHEN COALESCE("SellItem", 'N')<>'Y' THEN 1 ELSE 0 END)
  FROM "SBOESCOCESA"."OITM"
UNION ALL
SELECT 'BOLIK', COUNT(*),
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y' THEN 1 ELSE 0 END),
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y'
                     AND COALESCE("OnHand", 0)=0 THEN 1 ELSE 0 END),
       SUM(CASE WHEN COALESCE("SellItem", 'N')='Y'
                     AND COALESCE("OnHand", 0)-COALESCE("IsCommited", 0)<=0
                THEN 1 ELSE 0 END),
       SUM(CASE WHEN COALESCE("SellItem", 'N')<>'Y' THEN 1 ELSE 0 END)
  FROM "SBOBOLIK"."OITM"
ORDER BY "EMPRESA";

SELECT X."EMPRESA", X."ItemCode", X."ItemName", X."GRUPO",
       X."SellItem", X."validFor", X."OnHand", X."IsCommited"
FROM (
    SELECT U.*, ROW_NUMBER() OVER (
               PARTITION BY U."EMPRESA" ORDER BY U."ItemCode") AS "FILA"
    FROM (
    SELECT 'GRACO' AS "EMPRESA", I."ItemCode", I."ItemName",
           COALESCE(G."ItmsGrpNam", '') AS "GRUPO", I."SellItem",
           I."validFor", I."OnHand", I."IsCommited"
      FROM "SBO_GRACO"."OITM" I
      LEFT JOIN "SBO_GRACO"."OITB" G
             ON G."ItmsGrpCod"=I."ItmsGrpCod"
     WHERE COALESCE(I."SellItem", 'N')='Y'
    UNION ALL
    SELECT 'FAES', I."ItemCode", I."ItemName",
           COALESCE(G."ItmsGrpNam", ''), I."SellItem",
           I."validFor", I."OnHand", I."IsCommited"
      FROM "SBOESCOCESA"."OITM" I
      LEFT JOIN "SBOESCOCESA"."OITB" G
             ON G."ItmsGrpCod"=I."ItmsGrpCod"
     WHERE COALESCE(I."SellItem", 'N')='Y'
    UNION ALL
    SELECT 'BOLIK', I."ItemCode", I."ItemName",
           COALESCE(G."ItmsGrpNam", ''), I."SellItem",
           I."validFor", I."OnHand", I."IsCommited"
      FROM "SBOBOLIK"."OITM" I
     LEFT JOIN "SBOBOLIK"."OITB" G
             ON G."ItmsGrpCod"=I."ItmsGrpCod"
     WHERE COALESCE(I."SellItem", 'N')='Y'
    ) U
) X
WHERE X."FILA"<=20
ORDER BY X."EMPRESA", X."ItemCode"
;

SELECT X."EMPRESA", X."ItemCode", X."ItemName", X."GRUPO",
       X."SellItem", X."validFor", X."OnHand", X."IsCommited"
FROM (
    SELECT U.*, ROW_NUMBER() OVER (
               PARTITION BY U."EMPRESA" ORDER BY U."ItemCode") AS "FILA"
    FROM (
    SELECT 'GRACO' AS "EMPRESA", I."ItemCode", I."ItemName",
           COALESCE(G."ItmsGrpNam", '') AS "GRUPO", I."SellItem",
           I."validFor", I."OnHand", I."IsCommited"
      FROM "SBO_GRACO"."OITM" I
      LEFT JOIN "SBO_GRACO"."OITB" G
             ON G."ItmsGrpCod"=I."ItmsGrpCod"
     WHERE COALESCE(I."SellItem", 'N')<>'Y'
    UNION ALL
    SELECT 'FAES', I."ItemCode", I."ItemName",
           COALESCE(G."ItmsGrpNam", ''), I."SellItem",
           I."validFor", I."OnHand", I."IsCommited"
      FROM "SBOESCOCESA"."OITM" I
      LEFT JOIN "SBOESCOCESA"."OITB" G
             ON G."ItmsGrpCod"=I."ItmsGrpCod"
     WHERE COALESCE(I."SellItem", 'N')<>'Y'
    UNION ALL
    SELECT 'BOLIK', I."ItemCode", I."ItemName",
           COALESCE(G."ItmsGrpNam", ''), I."SellItem",
           I."validFor", I."OnHand", I."IsCommited"
      FROM "SBOBOLIK"."OITM" I
     LEFT JOIN "SBOBOLIK"."OITB" G
             ON G."ItmsGrpCod"=I."ItmsGrpCod"
     WHERE COALESCE(I."SellItem", 'N')<>'Y'
    ) U
) X
WHERE X."FILA"<=20
ORDER BY X."EMPRESA", X."ItemCode"
;
