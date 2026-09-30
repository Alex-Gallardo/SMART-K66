-- =============================================================================
-- OTIF — una fila por linea de pedido y nota de entrega valida.
-- =============================================================================
-- Los dos parametros ODBC delimitan la fecha prometida, no la fecha de
-- creacion. Se incluyen todas las lineas de los pedidos candidatos para
-- evaluar el modo "Pedido" completo; el navegador aplica el rango a las
-- lineas cuando se elige el modo "Linea".
-- Cada entrega se envia por separado para calcular la cantidad recibida
-- hasta la fecha prometida + tolerancia configurable, sin perder entregas
-- parciales. ODLN.CANCELED='N' excluye notas anuladas.
-- La conciliacion contra HANA debe verificarse en cada sociedad.
-- =============================================================================

WITH "CandidateOrders" AS
(
    SELECT DISTINCT o."DocEntry"
    FROM ORDR o
    JOIN RDR1 r ON r."DocEntry" = o."DocEntry"
    WHERE COALESCE(r."ShipDate", o."DocDueDate")
        BETWEEN TO_DATE(?) AND TO_DATE(?)
),
"ValidDeliveries" AS
(
    SELECT
        l."BaseEntry",
        l."BaseLine",
        l."DocEntry",
        l."LineNum",
        l."Quantity",
        d."DocDate"
    FROM DLN1 l
    JOIN ODLN d ON d."DocEntry" = l."DocEntry"
    JOIN "CandidateOrders" selected ON selected."DocEntry" = l."BaseEntry"
    WHERE l."BaseType" = 17
      AND d."CANCELED" = 'N'
)
SELECT
    o."DocEntry"      AS "OrderDocEntry",
    o."DocNum"        AS "OrderNumber",
    o."CardCode"      AS "CustomerCode",
    o."CardName"      AS "CustomerName",
    CASE
        WHEN UPPER(SUBSTRING(o."CardCode", 1, 2)) = 'CL' THEN 'Local'
        WHEN UPPER(SUBSTRING(o."CardCode", 1, 2)) = 'CE' THEN 'Extranjero'
        ELSE 'Otro'
    END               AS "Origen",
    o."SlpCode"       AS "SalesAgentCode",
    ag."SlpName"      AS "SalesAgent",
    r."LineNum"       AS "LineNumber",
    r."ItemCode"      AS "ItemCode",
    i."ItemName"      AS "ItemDescription",
    i."ItmsGrpCod"    AS "FamilyCode",
    b."ItmsGrpNam"    AS "FamilyName",
    o."DocDate"       AS "OrderDate",
    o."DocDueDate"    AS "DueDate",
    r."ShipDate"      AS "LineShipDate",
    r."Quantity"      AS "OrderedQty",
    r."OpenQty"       AS "OpenQty",
    r."LineStatus"    AS "LineStatus",
    dln."DocEntry"    AS "DeliveryDocEntry",
    dln."LineNum"     AS "DeliveryLineNumber",
    dln."DocDate"     AS "DeliveryDate",
    dln."Quantity"    AS "DeliveryQty",
    i."InvntryUom"    AS "UnitOfMeasure"
FROM ORDR o
JOIN "CandidateOrders" selected ON selected."DocEntry" = o."DocEntry"
JOIN RDR1 r ON r."DocEntry" = o."DocEntry"
LEFT JOIN OITM i ON i."ItemCode" = r."ItemCode"
LEFT JOIN OITB b ON b."ItmsGrpCod" = i."ItmsGrpCod"
LEFT JOIN OSLP ag ON ag."SlpCode" = o."SlpCode"
LEFT JOIN "ValidDeliveries" dln ON dln."BaseEntry" = o."DocEntry"
                               AND dln."BaseLine" = r."LineNum"
ORDER BY COALESCE(r."ShipDate", o."DocDueDate") DESC,
         o."DocEntry", r."LineNum", dln."DocDate", dln."DocEntry", dln."LineNum";


-- ---------------------------------------------------------------------------
-- VERIFICATION QUERIES — re-run before trusting OTIF on a NEW company schema
-- ---------------------------------------------------------------------------
-- SELECT COUNT(*) AS TotalNotasEntrega, MIN("DocDate") AS Desde, MAX("DocDate") AS Hasta
-- FROM ODLN WHERE "CANCELED" = 'N';
--
-- SELECT COUNT(*) AS TotalLineasEntrega,
--        COUNT(DISTINCT "ItemCode")  AS ItemsDistintos,
--        COUNT(DISTINCT "BaseEntry") AS OrdenesDistintasReferenciadas
-- FROM DLN1 l JOIN ODLN d ON d."DocEntry" = l."DocEntry"
-- WHERE l."BaseType" = 17 AND d."CANCELED" = 'N';
--
-- -- Closed-but-undelivered rate on this schema (compare to Graco's ~8.8%)
-- SELECT
--     SUM(CASE WHEN r."LineStatus"='C' AND dlnl."DocEntry" IS NULL THEN 1 ELSE 0 END) AS CerradasSinEntrega,
--     COUNT(*) AS TotalLineas
-- FROM ORDR o
-- JOIN RDR1 r ON r."DocEntry" = o."DocEntry"
-- LEFT JOIN DLN1 dlnl ON dlnl."BaseEntry" = o."DocEntry" AND dlnl."BaseLine" = r."LineNum" AND dlnl."BaseType" = 17
-- LEFT JOIN ODLN d ON d."DocEntry" = dlnl."DocEntry" AND d."CANCELED" = 'N';
