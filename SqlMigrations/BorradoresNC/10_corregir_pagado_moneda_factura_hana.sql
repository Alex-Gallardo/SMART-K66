/* SAP HANA / NDB - corrección del pago en moneda de la factura.
   Esta definición parte de las tres vistas vigentes compartidas para
   despliegue: conserva sus filtros, columnas y WITH READ ONLY.

   Antes de aplicar, guardar la definición actual de RC_FACTURAS_BORRNC
   en las tres sociedades y comparar sus filtros con esta migración. Ejecutar
   cada CREATE OR REPLACE VIEW individualmente. No modifica OINV ni otras
   tablas o vistas de SAP.

   La vista conserva su contrato: DocTotal y PaidToDate se expresan ambos
   en moneda del documento. Para QTZ: OINV.DocTotal / OINV.PaidToDate.
   Para moneda extranjera: OINV.DocTotalFC / OINV.PaidFC. Esta regla se
   aplica a todos los agentes. A diferencia de la migración histórica 09,
   no agrega un límite superior de fecha para EXPORTACION a la vista. */

/* 1/3 BOLIK. Otros agentes: 60 días; ABEL RIOS: 364 días. */
CREATE OR REPLACE VIEW "SBOBOLIK"."RC_FACTURAS_BORRNC"
    ("Empresa", "SlpName", "DocNum", "U_SERIE_FACE", "U_NUMERO_DOCUMENTO",
     "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "PaidToDate")
AS ((SELECT 'BOLIK' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBOBOLIK"."OINV" T0
     LEFT OUTER JOIN "SBOBOLIK"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 60
       AND T0."CANCELED" = 'N'
       AND T1."SlpName" NOT IN ('ABEL RIOS', 'EXPORTACION'))
    UNION ALL
    (SELECT 'BOLIK' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBOBOLIK"."OINV" T0
     LEFT OUTER JOIN "SBOBOLIK"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE T0."CANCELED" = 'N'
       AND ((T1."SlpName" = 'ABEL RIOS'
             AND DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 364)
            OR (T1."SlpName" = 'EXPORTACION'
                AND T0."DocDate" >= ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE)))))) WITH READ ONLY;

/* 2/3 FAES. Otros agentes y ABEL RIOS: 150 días. */
CREATE OR REPLACE VIEW "SBOESCOCESA"."RC_FACTURAS_BORRNC"
    ("Empresa", "SlpName", "DocNum", "U_SERIE_FACE", "U_NUMERO_DOCUMENTO",
     "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "PaidToDate")
AS ((SELECT 'FAES' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBOESCOCESA"."OINV" T0
     LEFT OUTER JOIN "SBOESCOCESA"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 150
       AND T0."CANCELED" = 'N'
       AND T1."SlpName" NOT IN ('ABEL RIOS', 'EXPORTACION'))
    UNION ALL
    (SELECT 'FAES' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBOESCOCESA"."OINV" T0
     LEFT OUTER JOIN "SBOESCOCESA"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE T0."CANCELED" = 'N'
       AND ((T1."SlpName" = 'ABEL RIOS'
             AND DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 150)
            OR (T1."SlpName" = 'EXPORTACION'
                AND T0."DocDate" >= ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE)))))) WITH READ ONLY;

/* 3/3 GRACO. Otros agentes: 60 días; ABEL RIOS: 365 días. */
CREATE OR REPLACE VIEW "SBO_GRACO"."RC_FACTURAS_BORRNC"
    ("Empresa", "SlpName", "DocNum", "U_SERIE_FACE", "U_NUMERO_DOCUMENTO",
     "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "PaidToDate")
AS ((SELECT 'GRACO' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBO_GRACO"."OINV" T0
     LEFT OUTER JOIN "SBO_GRACO"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 60
       AND T0."CANCELED" = 'N'
       AND T1."SlpName" NOT IN ('ABEL RIOS', 'EXPORTACION'))
    UNION ALL
    (SELECT 'GRACO' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBO_GRACO"."OINV" T0
     LEFT OUTER JOIN "SBO_GRACO"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE T0."CANCELED" = 'N'
       AND ((T1."SlpName" = 'ABEL RIOS'
             AND DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 365)
            OR (T1."SlpName" = 'EXPORTACION'
                AND T0."DocDate" >= ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE)))))) WITH READ ONLY;

/* Debe devolver tres vistas válidas y de solo lectura. */
SELECT "SCHEMA_NAME", "IS_VALID", "IS_READ_ONLY",
       LENGTH("DEFINITION") AS "LARGO"
FROM "SYS"."VIEWS"
WHERE "VIEW_NAME" = 'RC_FACTURAS_BORRNC'
  AND "SCHEMA_NAME" IN ('SBOBOLIK', 'SBOESCOCESA', 'SBO_GRACO')
ORDER BY "SCHEMA_NAME";

/* Caso histórico FAES / CE0014 / 2000706: comparar con OINV actual.
   Los importes pueden haber cambiado desde el diagnóstico original. */
SELECT V."DocNum", V."DocCur", V."DocTotal" AS "TOTAL_VISTA",
       V."PaidToDate" AS "PAGADO_VISTA", I."DocTotal" AS "TOTAL_LOCAL",
       I."PaidToDate" AS "PAGADO_LOCAL", I."DocTotalFC" AS "TOTAL_FC",
       I."PaidFC" AS "PAGADO_FC"
FROM "SBOESCOCESA"."RC_FACTURAS_BORRNC" V
JOIN "SBOESCOCESA"."OINV" I
  ON I."DocNum" = V."DocNum" AND I."CardCode" = V."CardCode"
 AND I."DocDate" = V."DocDate" AND I."DocCur" = V."DocCur"
WHERE V."DocNum" = 2000706 AND V."CardCode" = 'CE0014';
