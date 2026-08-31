-- =============================================================================
-- BACKLOG DE PEDIDOS DE VENTA
-- =============================================================================
-- Devuelve:
--   1. Todas las líneas actualmente abiertas, aunque el pedido sea anterior al
--      rango, para que el backlog operativo y la reserva FIFO no omitan deuda.
--   2. Todas las líneas de pedidos creados dentro del rango, para construir la
--      tendencia histórica aun cuando esas líneas ya estén cerradas.
--
-- Parámetros ODBC posicionales:
--   p1 = fecha inicial yyyy-MM-dd
--   p2 = fecha final   yyyy-MM-dd
--
-- Los aliases se citan porque SAP HANA normaliza identificadores no citados a
-- mayúsculas y el cliente JavaScript consume estos nombres con casing exacto.
-- =============================================================================

SELECT
    r."DocEntry"      AS "OrderDocEntry",
    r."DocNum"        AS "OrderNumber",
    r."DocDate"       AS "OrderDate",
    r."DocDueDate"    AS "DueDate",
    r."DocStatus"     AS "OrderStatus",
    r."CardCode"      AS "CustomerCode",
    r."CardName"      AS "CustomerName",
    CASE
        WHEN UPPER(SUBSTRING(r."CardCode", 1, 2)) = 'CL' THEN 'Local'
        WHEN UPPER(SUBSTRING(r."CardCode", 1, 2)) = 'CE' THEN 'Extranjero'
        ELSE 'Otro'
    END               AS "Origen",
    r."SlpCode"       AS "SalesAgentCode",
    ag."SlpName"      AS "SalesAgent",
    st."State"        AS "CustomerStateCode",
    COALESCE(cst."Name", st."State")
                        AS "CustomerState",
    l."LineNum"       AS "LineNumber",
    l."ItemCode"      AS "ItemCode",
    i."ItemName"      AS "ItemDescription",
    i."ItmsGrpCod"    AS "FamilyCode",
    b."ItmsGrpNam"    AS "FamilyName",
    l."Quantity"      AS "OrderedQty",
    l."OpenQty"       AS "OpenQty",
    l."LineStatus"    AS "LineStatus",
    l."ShipDate"      AS "LineShipDate",
    stock."OnHand"    AS "StockOnHand",
    stock_detail."ByWarehouse"
                        AS "StockByWarehouse"
FROM ORDR r
JOIN RDR1 l
    ON l."DocEntry" = r."DocEntry"
LEFT JOIN OITM i
    ON i."ItemCode" = l."ItemCode"
LEFT JOIN OITB b
    ON b."ItmsGrpCod" = i."ItmsGrpCod"
LEFT JOIN OSLP ag
    ON ag."SlpCode" = r."SlpCode"
LEFT JOIN CRD1 st
    ON st."CardCode" = r."CardCode"
   AND st."Address" = r."ShipToCode"
   AND st."AdresType" = 'S'
LEFT JOIN OCST cst
    ON cst."Code" = st."State"
   AND cst."Country" = st."Country"
LEFT JOIN (
    SELECT
        o."ItemCode",
        SUM(o."OnHand") AS "OnHand"
    FROM OITW o
    GROUP BY o."ItemCode"
) stock
    ON stock."ItemCode" = l."ItemCode"
LEFT JOIN (
    SELECT
        o."ItemCode",
        STRING_AGG(
            COALESCE(whs."WhsName", o."WhsCode")
                || ': '
                || TO_VARCHAR(o."OnHand"),
            ' · '
        ) AS "ByWarehouse"
    FROM OITW o
    LEFT JOIN OWHS whs
        ON whs."WhsCode" = o."WhsCode"
    WHERE o."OnHand" <> 0
    GROUP BY o."ItemCode"
) stock_detail
    ON stock_detail."ItemCode" = l."ItemCode"
WHERE
    (l."LineStatus" = 'O' AND l."OpenQty" > 0)
    OR r."DocDate" BETWEEN ? AND ?
ORDER BY
    r."DocDate" DESC,
    r."DocEntry",
    l."LineNum";

-- =============================================================================
-- VALIDACIONES DE AMBIENTE REQUERIDAS
-- =============================================================================
-- 1. Confirmar que OCST resuelve CustomerState a nombres de departamentos.
-- 2. Comparar StockOnHand de una muestra de items contra SAP B1.
-- 3. Confirmar disponibilidad de STRING_AGG en la versión de SAP HANA.
-- 4. Verificar que OrderDocEntry + LineNumber sea único en el resultado.
-- 5. Comparar cantidades abiertas contra el reporte interno Backorder General.
