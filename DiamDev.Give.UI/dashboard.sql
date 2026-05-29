WITH PARAMS AS
(
    SELECT
        TO_DATE(?) AS "FechaIni",
        TO_DATE(?) AS "FechaFin"
    FROM DUMMY
),

INGRESOS_DIA AS
(
    SELECT
        D."U_beas_belnrid"  AS "OT",
        D."U_beas_belposid" AS "PosicionOT",
        D."ItemCode"        AS "ItemID",
        D."Dscription"      AS "DescripcionItem",
        D."unitMsr"         AS "UnidadMedida",
        H."DocDate"         AS "FechaIngreso",
        SUM(D."Quantity")   AS "CantidadRealDia"
    FROM "IGN1" D
    INNER JOIN "OIGN" H
        ON H."DocEntry" = D."DocEntry"
    CROSS JOIN PARAMS P
    WHERE H."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
      AND D."U_beas_belnrid" IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL
      AND UPPER(D."unitMsr") <> 'KG'
      AND UPPER(D."Dscription") NOT LIKE '%COPRODUCTO%'
    GROUP BY
        D."U_beas_belnrid",
        D."U_beas_belposid",
        D."ItemCode",
        D."Dscription",
        D."unitMsr",
        H."DocDate"
),

INGRESOS_RANGO AS
(
    SELECT
        D."U_beas_belnrid"  AS "OT",
        D."U_beas_belposid" AS "PosicionOT",
        SUM(D."Quantity")   AS "CantidadRealRango"
    FROM "IGN1" D
    INNER JOIN "OIGN" H
        ON H."DocEntry" = D."DocEntry"
    CROSS JOIN PARAMS P
    WHERE H."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
      AND D."U_beas_belnrid" IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL
      AND UPPER(D."unitMsr") <> 'KG'
      AND UPPER(D."Dscription") NOT LIKE '%COPRODUCTO%'
    GROUP BY
        D."U_beas_belnrid",
        D."U_beas_belposid"
),

INGRESOS_TOTAL AS
(
    SELECT
        D."U_beas_belnrid"  AS "OT",
        D."U_beas_belposid" AS "PosicionOT",
        SUM(D."Quantity")   AS "CantidadRealTotal"
    FROM "IGN1" D
    INNER JOIN "OIGN" H
        ON H."DocEntry" = D."DocEntry"
    CROSS JOIN PARAMS P
    WHERE H."DocDate" <= P."FechaFin"
      AND D."U_beas_belnrid" IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL
      AND UPPER(D."unitMsr") <> 'KG'
      AND UPPER(D."Dscription") NOT LIKE '%COPRODUCTO%'
    GROUP BY
        D."U_beas_belnrid",
        D."U_beas_belposid"
),

HORAS_DIA AS
(
    SELECT
        A."BELNR_ID"  AS "OT",
        A."BELPOS_ID" AS "PosicionOT",
        A."POS_ID",
        A."DocDate"   AS "Fecha",
        SUM(A."ZEIT") / 60.0 AS "HoraRealDia"
    FROM "BEAS_ARBZEIT" A
    CROSS JOIN PARAMS P
    WHERE A."CANCEL" = 0
      AND A."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
    GROUP BY
        A."BELNR_ID",
        A."BELPOS_ID",
        A."POS_ID",
        A."DocDate"
),

HORAS_RANGO AS
(
    SELECT
        A."BELNR_ID"  AS "OT",
        A."BELPOS_ID" AS "PosicionOT",
        A."POS_ID",
        SUM(A."ZEIT") / 60.0 AS "HoraRealRango"
    FROM "BEAS_ARBZEIT" A
    CROSS JOIN PARAMS P
    WHERE A."CANCEL" = 0
      AND A."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
    GROUP BY
        A."BELNR_ID",
        A."BELPOS_ID",
        A."POS_ID"
),

HORAS_OT AS
(
    SELECT
        A."BELNR_ID"  AS "OT",
        A."BELPOS_ID" AS "PosicionOT",
        A."POS_ID",
        SUM(A."ZEIT") / 60.0 AS "HoraRealOT"
    FROM "BEAS_ARBZEIT" A
    CROSS JOIN PARAMS P
    WHERE A."CANCEL" = 0
      AND A."DocDate" <= P."FechaFin"
    GROUP BY
        A."BELNR_ID",
        A."BELPOS_ID",
        A."POS_ID"
),

PLAN_OT AS
(
    SELECT
        T0."BELNR_ID"  AS "OT",
        T0."BELPOS_ID" AS "PosicionOT",
        T0."APLATZ_ID" AS "CodigoRecurso",

        MAX(T3."MENGE") AS "CantidadPlanificada",

        SUM(
            CASE
                WHEN T0."MENGE_JE" = 0
                THEN T0."TEAPLATZ" + T0."TRAPLATZ"
                ELSE
                    ROUND(
                        (T3."MENGE_VERBRAUCH" * T0."TEAPLATZ")
                        / T0."MENGE_JE",
                    2)
                    + T0."TRAPLATZ" / 24
            END
        ) AS "HoraPlan"

    FROM "BEAS_FTAPL" T0

    INNER JOIN "BEAS_FTPOS" T3
        ON T3."BELNR_ID" = T0."BELNR_ID"
       AND T3."BELPOS_ID" = T0."BELPOS_ID"

    WHERE COALESCE(T0."AKTIV",'J') = 'J'

    GROUP BY
        T0."BELNR_ID",
        T0."BELPOS_ID",
        T0."APLATZ_ID"
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

        11 AS "HorasTurno",

        MAX(P."CantidadPlanificada") AS "CantidadPlaneada",

        MAX(IDR."CantidadRealDia")   AS "CantidadRealDia",
        MAX(IR."CantidadRealRango")  AS "CantidadRealRango",
        MAX(IT."CantidadRealTotal")  AS "CantidadRealTotal",

        MAX(P."HoraPlan") AS "HoraPlan",

        MAX(HD."HoraRealDia")    AS "HoraRealDia",
        MAX(HR."HoraRealRango")  AS "HoraRealRango",
        MAX(HO."HoraRealOT")     AS "HoraRealOT"

    FROM "BEAS_FTAPL" T0

    INNER JOIN INGRESOS_DIA I
        ON I."OT" = T0."BELNR_ID"
       AND I."PosicionOT" = T0."BELPOS_ID"

    LEFT JOIN INGRESOS_DIA IDR
        ON IDR."OT" = T0."BELNR_ID"
       AND IDR."PosicionOT" = T0."BELPOS_ID"
       AND IDR."FechaIngreso" = I."FechaIngreso"

    LEFT JOIN INGRESOS_RANGO IR
        ON IR."OT" = T0."BELNR_ID"
       AND IR."PosicionOT" = T0."BELPOS_ID"

    LEFT JOIN INGRESOS_TOTAL IT
        ON IT."OT" = T0."BELNR_ID"
       AND IT."PosicionOT" = T0."BELPOS_ID"

    LEFT JOIN HORAS_DIA HD
        ON HD."OT" = T0."BELNR_ID"
       AND HD."PosicionOT" = T0."BELPOS_ID"
       AND HD."POS_ID" = T0."POS_ID"
       AND HD."Fecha" = I."FechaIngreso"

    LEFT JOIN HORAS_RANGO HR
        ON HR."OT" = T0."BELNR_ID"
       AND HR."PosicionOT" = T0."BELPOS_ID"
       AND HR."POS_ID" = T0."POS_ID"

    LEFT JOIN HORAS_OT HO
        ON HO."OT" = T0."BELNR_ID"
       AND HO."PosicionOT" = T0."BELPOS_ID"
       AND HO."POS_ID" = T0."POS_ID"

    LEFT JOIN "BEAS_APLATZ" R
        ON R."APLATZ_ID" = T0."APLATZ_ID"

    LEFT JOIN PLAN_OT P
        ON P."OT" = T0."BELNR_ID"
       AND P."PosicionOT" = T0."BELPOS_ID"
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

    CASE
        WHEN D."CantidadRealTotal" >= D."CantidadPlaneada"
        THEN 'Cerrada'
        ELSE 'Abierta'
    END AS "EstadoOT",

    D."CantidadPlaneada"   AS "Cantidad Planeada",
    D."CantidadRealTotal"  AS "Cantidad Hecha",

    D."HoraPlan"       AS "Hora Plan",
    D."HoraRealDia"    AS "Hora Real Día",
    D."HoraRealRango"  AS "Hora Real Rango",
    D."HoraRealOT"     AS "Hora Real OT",

    ROUND(
        D."CantidadPlaneada"
        /
        (D."HoraPlan" / D."HorasTurno"),
    2) AS "Pieza*turnoPlan",

    CASE
        WHEN D."HoraRealDia" > 0
        THEN ROUND(
            D."CantidadRealDia"
            /
            (D."HoraRealDia" / D."HorasTurno"),
        2)
    END AS "Pieza*turnoReal",

    CASE
        WHEN D."HoraRealDia" > 0
        THEN ROUND(
            (
                (
                    D."CantidadRealDia"
                    /
                    (D."HoraRealDia" / D."HorasTurno")
                )
                /
                (
                    D."CantidadPlaneada"
                    /
                    (D."HoraPlan" / D."HorasTurno")
                )
            ) * 100,
        2)
    END AS "Eficiencia Dia",

    CASE
        WHEN D."HoraRealRango" > 0
        THEN ROUND(
            (
                (
                    D."CantidadRealRango"
                    /
                    (D."HoraRealRango" / D."HorasTurno")
                )
                /
                (
                    D."CantidadPlaneada"
                    /
                    (D."HoraPlan" / D."HorasTurno")
                )
            ) * 100,
        2)
    END AS "Eficiencia Rango"

FROM DETALLE D

ORDER BY
    D."Fecha",
    D."OT",
    D."PosicionOT",
    D."CodigoRecurso";