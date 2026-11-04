/*
  Control de regresión para RC_FACTURAS_BORRNC en las tres sociedades.
  Debe devolver CERO filas. Cada fila devuelta requiere investigar si la
  vista dejó de expresar DocTotal y PaidToDate en la moneda de DocCur.
  No modifica SAP ni SQL Server.
*/
SELECT 'BOLIK' AS "Empresa", V."DocNum", V."CardCode", V."DocDate",
       V."DocCur", V."DocTotal", V."PaidToDate"
FROM "SBOBOLIK"."RC_FACTURAS_BORRNC" V
WHERE NOT EXISTS
(
    SELECT 1 FROM "SBOBOLIK"."OINV" I
    WHERE I."DocNum" = V."DocNum" AND I."CardCode" = V."CardCode"
      AND I."DocDate" = V."DocDate" AND I."DocCur" = V."DocCur"
      AND V."DocTotal" = CASE WHEN I."DocCur" <> 'QTZ'
                              THEN I."DocTotalFC" ELSE I."DocTotal" END
      AND V."PaidToDate" = CASE WHEN I."DocCur" <> 'QTZ'
                                THEN I."PaidFC" ELSE I."PaidToDate" END
)
UNION ALL
SELECT 'FAES', V."DocNum", V."CardCode", V."DocDate",
       V."DocCur", V."DocTotal", V."PaidToDate"
FROM "SBOESCOCESA"."RC_FACTURAS_BORRNC" V
WHERE NOT EXISTS
(
    SELECT 1 FROM "SBOESCOCESA"."OINV" I
    WHERE I."DocNum" = V."DocNum" AND I."CardCode" = V."CardCode"
      AND I."DocDate" = V."DocDate" AND I."DocCur" = V."DocCur"
      AND V."DocTotal" = CASE WHEN I."DocCur" <> 'QTZ'
                              THEN I."DocTotalFC" ELSE I."DocTotal" END
      AND V."PaidToDate" = CASE WHEN I."DocCur" <> 'QTZ'
                                THEN I."PaidFC" ELSE I."PaidToDate" END
)
UNION ALL
SELECT 'GRACO', V."DocNum", V."CardCode", V."DocDate",
       V."DocCur", V."DocTotal", V."PaidToDate"
FROM "SBO_GRACO"."RC_FACTURAS_BORRNC" V
WHERE NOT EXISTS
(
    SELECT 1 FROM "SBO_GRACO"."OINV" I
    WHERE I."DocNum" = V."DocNum" AND I."CardCode" = V."CardCode"
      AND I."DocDate" = V."DocDate" AND I."DocCur" = V."DocCur"
      AND V."DocTotal" = CASE WHEN I."DocCur" <> 'QTZ'
                              THEN I."DocTotalFC" ELSE I."DocTotal" END
      AND V."PaidToDate" = CASE WHEN I."DocCur" <> 'QTZ'
                                THEN I."PaidFC" ELSE I."PaidToDate" END
);
