/* SAP HANA / NDB - EXPORTACION: todas las facturas no anuladas desde el
   1 de enero del año en curso hasta hoy, incluidas las ya pagadas.

   Requisitos de despliegue:
   1. Respaldar el CREATE VIEW actual de las tres vistas.
   2. Ejecutar cada CREATE OR REPLACE VIEW individualmente en producción.
   3. Publicar después la aplicación con el filtro especial de EXPORTACION.

   La vista no decide si se muestran pagadas: esa regla está en BorradorNcBLL.
   Solo para EXPORTACION, esta migración corrige la moneda de PaidToDate
   para que coincida con DocTotal: para USD/otra moneda extranjera usa
   PaidFC; para QTZ usa PaidToDate. Mantiene intactos los importes y
   límites previos de los otros agentes.
   No modifica las vistas RC_FACTURAS_BORRNC2 ni las tablas de SAP. */

SELECT CURRENT_DATE AS "HOY",
       ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE)) AS "DESDE_EXPORTACION"
FROM DUMMY;

/* 1/3 BOLIK. Otros agentes: 60 días; ABEL RIOS: 364 días. */
CREATE OR REPLACE VIEW "SBOBOLIK"."RC_FACTURAS_BORRNC"
    ("Empresa", "SlpName", "DocNum", "U_SERIE_FACE", "U_NUMERO_DOCUMENTO",
     "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "PaidToDate")
AS ((SELECT 'BOLIK' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            T0."PaidToDate"
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
            CASE WHEN T1."SlpName" = 'EXPORTACION' AND T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBOBOLIK"."OINV" T0
     LEFT OUTER JOIN "SBOBOLIK"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE T0."CANCELED" = 'N'
       AND ((T1."SlpName" = 'ABEL RIOS'
             AND DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 364)
            OR (T1."SlpName" = 'EXPORTACION'
                AND T0."DocDate" >= ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE))
                AND T0."DocDate" < ADD_DAYS(CURRENT_DATE, 1)))));

/* 2/3 FAES. Otros agentes y ABEL RIOS: 150 días. */
CREATE OR REPLACE VIEW "SBOESCOCESA"."RC_FACTURAS_BORRNC"
    ("Empresa", "SlpName", "DocNum", "U_SERIE_FACE", "U_NUMERO_DOCUMENTO",
     "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "PaidToDate")
AS ((SELECT 'FAES' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            T0."PaidToDate"
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
            CASE WHEN T1."SlpName" = 'EXPORTACION' AND T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBOESCOCESA"."OINV" T0
     LEFT OUTER JOIN "SBOESCOCESA"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE T0."CANCELED" = 'N'
       AND ((T1."SlpName" = 'ABEL RIOS'
             AND DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 150)
            OR (T1."SlpName" = 'EXPORTACION'
                AND T0."DocDate" >= ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE))
                AND T0."DocDate" < ADD_DAYS(CURRENT_DATE, 1)))));

/* 3/3 GRACO. Otros agentes: 60 días; ABEL RIOS: 365 días. */
CREATE OR REPLACE VIEW "SBO_GRACO"."RC_FACTURAS_BORRNC"
    ("Empresa", "SlpName", "DocNum", "U_SERIE_FACE", "U_NUMERO_DOCUMENTO",
     "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "PaidToDate")
AS ((SELECT 'GRACO' AS "Empresa", T1."SlpName", T0."DocNum",
            T0."U_SERIE_FACE", T0."U_NUMERO_DOCUMENTO", T0."DocDate",
            T0."CardCode", T0."CardName", T0."DocCur",
            CASE WHEN T0."DocCur" <> 'QTZ' THEN T0."DocTotalFC"
                 ELSE T0."DocTotal" END AS "DocTotal",
            T0."PaidToDate"
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
            CASE WHEN T1."SlpName" = 'EXPORTACION' AND T0."DocCur" <> 'QTZ' THEN T0."PaidFC"
                 ELSE T0."PaidToDate" END AS "PaidToDate"
     FROM "SBO_GRACO"."OINV" T0
     LEFT OUTER JOIN "SBO_GRACO"."OSLP" T1 ON T0."SlpCode" = T1."SlpCode"
     WHERE T0."CANCELED" = 'N'
       AND ((T1."SlpName" = 'ABEL RIOS'
             AND DAYS_BETWEEN(T0."DocDate", CURRENT_DATE) <= 365)
            OR (T1."SlpName" = 'EXPORTACION'
                AND T0."DocDate" >= ADD_DAYS(CURRENT_DATE, 1 - DAYOFYEAR(CURRENT_DATE))
                AND T0."DocDate" < ADD_DAYS(CURRENT_DATE, 1)))));

/* Debe devolver tres vistas válidas. */
SELECT "SCHEMA_NAME", "IS_VALID", LENGTH("DEFINITION") AS "LARGO"
FROM "SYS"."VIEWS"
WHERE "VIEW_NAME" = 'RC_FACTURAS_BORRNC'
  AND "SCHEMA_NAME" IN ('SBOBOLIK', 'SBOESCOCESA', 'SBO_GRACO')
ORDER BY "SCHEMA_NAME";

/* Caso de control obtenido del diagnóstico: debe devolver PAGADO_VISTA
   cercano a 22473.04 USD, no 171610.20 GTQ. */
SELECT "DocNum", "DocDate", "DocCur", "DocTotal",
       "PaidToDate" AS "PAGADO_VISTA"
FROM "SBOESCOCESA"."RC_FACTURAS_BORRNC"
WHERE "DocNum" = 2000706 AND "CardCode" = 'CE0014';

/* Con los datos del diagnóstico del 08/10/2026, CE0014 debe tener 9 facturas
   en FAES y 1 en GRACO. BOLIK no tenía registros para ese cliente. */
SELECT 'BOLIK' AS "EMPRESA", COUNT(*) AS "FACTURAS_CE0014_EXPORTACION"
FROM "SBOBOLIK"."RC_FACTURAS_BORRNC"
WHERE "CardCode" = 'CE0014' AND "SlpName" = 'EXPORTACION'
UNION ALL
SELECT 'FAES' AS "EMPRESA", COUNT(*) AS "FACTURAS_CE0014_EXPORTACION"
FROM "SBOESCOCESA"."RC_FACTURAS_BORRNC"
WHERE "CardCode" = 'CE0014' AND "SlpName" = 'EXPORTACION'
UNION ALL
SELECT 'GRACO' AS "EMPRESA", COUNT(*) AS "FACTURAS_CE0014_EXPORTACION"
FROM "SBO_GRACO"."RC_FACTURAS_BORRNC"
WHERE "CardCode" = 'CE0014' AND "SlpName" = 'EXPORTACION';
