WITH PARAMS AS
(
    SELECT
        TO_DATE(?) AS "FechaIni",
        TO_DATE(?) AS "FechaFin"
    FROM DUMMY
),

INGRESOS AS
(
    SELECT
        D."U_beas_belnrid"  AS "OT",
        D."U_beas_belposid" AS "PosicionOT",

        D."ItemCode"        AS "ItemID",
        D."Dscription"      AS "DescripcionItem",
        D."unitMsr"         AS "UnidadMedida",

        H."DocDate"         AS "FechaIngreso",

        SUM(D."Quantity")   AS "IngresoEM"

    FROM "IGN1" D
    INNER JOIN "OIGN" H ON H."DocEntry" = D."DocEntry"
    CROSS JOIN PARAMS P

    WHERE H."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
      AND D."U_beas_belnrid"  IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL

    GROUP BY
        D."U_beas_belnrid",
        D."U_beas_belposid",
        D."ItemCode",
        D."Dscription",
        D."unitMsr",
        H."DocDate"
),

HORAS AS
(
    SELECT
        A."BELNR_ID"  AS "OT",
        A."BELPOS_ID" AS "PosicionOT",
        A."POS_ID",
        A."DocDate"   AS "Fecha",
        SUM(A."ZEIT") / 60.0 AS "HorasReales"
    FROM "BEAS_ARBZEIT" A
    CROSS JOIN PARAMS P
    WHERE A."CANCEL" = 0
      AND A."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
    GROUP BY A."BELNR_ID", A."BELPOS_ID", A."POS_ID", A."DocDate"
),

PLAN_OT AS
(
    SELECT
        "BELNR_ID"  AS "OT",
        "BELPOS_ID" AS "PosicionOT",
        "APLATZ_ID" AS "CodigoRecurso",

        SUM(
            ( COALESCE("TRAPLATZ", 0)
              + COALESCE("THAPLATZ", 0) * COALESCE("ANZAHL", 0)
              + COALESCE("TNAPLATZ", 0)
              + COALESCE("TEAPLATZ", 0)
            ) / 60.0
        ) AS "HorasPlanificadas"

    FROM "BEAS_FTAPL"
    WHERE COALESCE("AKTIV", 'J') = 'J'
    GROUP BY "BELNR_ID", "BELPOS_ID", "APLATZ_ID"
),

DETALLE AS
(
    SELECT
        I."FechaIngreso" AS "Fecha",

        T0."BELNR_ID"  AS "OT",
        T0."BELPOS_ID" AS "PosicionOT",

        I."ItemID",
        I."DescripcionItem",
        I."UnidadMedida",

        T0."APLATZ_ID" AS "CodigoRecurso",
        R."BEZ"        AS "DescripcionRecurso",

        SUM(I."IngresoEM")                       AS "IngresoEM",
        SUM(COALESCE(H."HorasReales",0))         AS "HorasRealesDia",
        MAX(P."HorasPlanificadas")               AS "HorasPlanificadas",
        GREATEST( 24.0 - SUM(COALESCE(H."HorasReales",0)), 0 )
            AS "HorasParo24h"

    FROM "BEAS_FTAPL" T0

    INNER JOIN INGRESOS I
        ON I."OT" = T0."BELNR_ID"
       AND I."PosicionOT" = T0."BELPOS_ID"

    LEFT JOIN HORAS H
        ON H."OT"         = T0."BELNR_ID"
       AND H."PosicionOT" = T0."BELPOS_ID"
       AND H."POS_ID"     = T0."POS_ID"
       AND H."Fecha"      = I."FechaIngreso"

    LEFT JOIN "BEAS_APLATZ" R
        ON R."APLATZ_ID" = T0."APLATZ_ID"

    LEFT JOIN PLAN_OT P
        ON P."OT"            = T0."BELNR_ID"
       AND P."PosicionOT"    = T0."BELPOS_ID"
       AND P."CodigoRecurso" = T0."APLATZ_ID"

    WHERE COALESCE(T0."AKTIV",'J') = 'J'

    GROUP BY
        I."FechaIngreso",
        T0."BELNR_ID",
        T0."BELPOS_ID",
        I."ItemID",
        I."DescripcionItem",
        I."UnidadMedida",
        T0."APLATZ_ID",
        R."BEZ"
)

SELECT
    D."Fecha",
    D."OT",
    D."PosicionOT",
    D."ItemID",
    D."DescripcionItem",
    D."UnidadMedida",
    D."CodigoRecurso",
    D."DescripcionRecurso",
    D."IngresoEM",
    D."HorasRealesDia",
    D."HorasPlanificadas",
    D."HorasParo24h",

    SUM(D."HorasRealesDia") OVER (
        PARTITION BY D."OT", D."PosicionOT", D."CodigoRecurso"
    ) AS "HorasRealesOT",

    CASE
        WHEN SUM(D."HorasRealesDia") OVER (
            PARTITION BY D."OT", D."PosicionOT", D."CodigoRecurso"
        ) > 0
        THEN ROUND(
            D."HorasPlanificadas"
            / SUM(D."HorasRealesDia") OVER (
                PARTITION BY D."OT", D."PosicionOT", D."CodigoRecurso"
            ) * 100
        , 2)
    END AS "PctEficiencia"

FROM DETALLE D

ORDER BY D."Fecha", D."OT", D."PosicionOT", D."CodigoRecurso";
