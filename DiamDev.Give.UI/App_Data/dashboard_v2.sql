WITH PARAMS AS
(
    SELECT
        TO_DATE(?) AS "FechaIni",
        TO_DATE(?) AS "FechaFin"
    FROM DUMMY
),

-- NOTA (26/08/2026) — filtro nuevo, POS."ItemCode" = D."ItemCode":
-- Luigi reportó "ruido" en el recurso PP FORMADORA PAPEL BMP BMI, donde
-- MP00355 (etiqueta) y MP00326 (corrugado) — materias primas COMPRADAS, que
-- según Luigi nunca se "producen" en ningún recurso — aparecían con horas y
-- cantidades de producción. Se investigó con datos_prueba.xls: para la OT
-- 13338 posición 250, el 02/06/2026 hay TRES líneas de IGN1 con la MISMA
-- OT+Posición+Fecha+Cantidad(779.56)+Horas(31.00): una para PT00536 (el vaso
-- que sí se produce en ese recurso) y dos más para las materias primas
-- consumidas en ese mismo paso (la etiqueta y el corrugado). Se repite en 116
-- filas / 59 códigos MP / 28 OTs distintas — siempre como una fila "extra" que
-- acompaña a la fila real del producto en la misma OT+Posición+Fecha.
--
-- Causa raíz: BEAS_FTAPL (T0, de dónde sale esta CTE) no tiene su propio
-- ItemCode — el JOIN a IGN1 solo usa OT+Posición, así que CUALQUIER línea de
-- IGN1 con esa etiqueta (la del producto Y las de materiales consumidos que
-- BEAS también registra en IGN1 con la misma OT+Posición) se mete como si
-- fuera "lo producido". BEAS_FTPOS.ItemCode sí es confiable a nivel de
-- posición (confirmado en data_dictionary.md), así que se agrega ese JOIN acá
-- para quedarnos solo con la línea de IGN1 cuyo ItemCode coincide con el item
-- que esa posición realmente tiene planificado producir.
--
-- ⚠️ No pude correr esto contra HANA real (sin acceso directo) — validé la
-- lógica contra datos_prueba.xls a mano, pero pido que Luigi corra esta
-- versión y confirme que (a) el ruido de MP desaparece y (b) no se cayó
-- ninguna fila legítima de producto antes de darlo por bueno.
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
    INNER JOIN "BEAS_FTPOS" POS
        ON POS."BELNR_ID" = D."U_beas_belnrid"
       AND POS."BELPOS_ID" = D."U_beas_belposid"
       AND POS."ItemCode" = D."ItemCode"
    CROSS JOIN PARAMS P
    WHERE H."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
      AND D."U_beas_belnrid" IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL
      -- KG y coproductos se conservan: son la producción válida de
      -- extrusoras y molinos. El JOIN con BEAS_FTPOS limita la fila al
      -- artículo planificado y evita incluir consumos de materia prima.
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
    INNER JOIN "BEAS_FTPOS" POS
        ON POS."BELNR_ID" = D."U_beas_belnrid"
       AND POS."BELPOS_ID" = D."U_beas_belposid"
       AND POS."ItemCode" = D."ItemCode"
    CROSS JOIN PARAMS P
    WHERE H."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
      AND D."U_beas_belnrid" IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL
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
    INNER JOIN "BEAS_FTPOS" POS
        ON POS."BELNR_ID" = D."U_beas_belnrid"
       AND POS."BELPOS_ID" = D."U_beas_belposid"
       AND POS."ItemCode" = D."ItemCode"
    CROSS JOIN PARAMS P
    WHERE H."DocDate" <= P."FechaFin"
      AND D."U_beas_belnrid" IS NOT NULL
      AND D."U_beas_belposid" IS NOT NULL
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
        A."UDF2"      AS "TurnoCodigo",
        MAX(A."UDF1") AS "Supervisor",
        MAX(A."UDF3") AS "MotivoParoCodigo",
        MAX(A."UDF4") AS "TiempoParo",
        SUM(A."ZEIT") / 60.0 AS "HoraRealDia"
    FROM "BEAS_ARBZEIT" A
    CROSS JOIN PARAMS P
    WHERE A."CANCEL" = 0
      AND A."DocDate" BETWEEN P."FechaIni" AND P."FechaFin"
    GROUP BY
        A."BELNR_ID",
        A."BELPOS_ID",
        A."POS_ID",
        A."DocDate",
        A."UDF2"
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

        -- Horas productivas efectivas usadas por las fórmulas de rendimiento.
        -- La UI usa 12 horas reloj por turno para capacidad/paro/disponible.
        11 AS "HorasTurno",

        HD."TurnoCodigo"             AS "TurnoCodigo",
        MAX(HD."Supervisor")         AS "Supervisor",
        MAX(HD."MotivoParoCodigo")   AS "MotivoParoCodigo",
        MAX(HD."TiempoParo")         AS "TiempoParo",

        MAX(P."CantidadPlanificada") AS "CantidadPlaneada",

        -- IGN1 no identifica turno: estas cantidades se repiten en Día/Noche.
        -- La UI usa Cantidad Real Día y desduplica por fecha/OT/posición/item.
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
        R."BEZ",
        HD."TurnoCodigo"
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
        WHEN D."TurnoCodigo" = '1' THEN 'Dia'
        WHEN D."TurnoCodigo" = '2' THEN 'Noche'
        ELSE D."TurnoCodigo"
    END AS "Turno",
    D."Supervisor"       AS "Supervisor",
    D."MotivoParoCodigo" AS "Motivo de Paro",
    D."TiempoParo"       AS "Tiempo de Paro",

    -- Familia — OITM.ItmsGrpCod -> OITB.ItmsGrpNam. CONFIRMADO poblado y con
    -- sentido en esta instalación (Luigi corrió la consulta de verificación:
    -- 58 grupos con nombres reales — "PT VASOS FOAM", "PP DOMOS", "MP BOBINA",
    -- "REPUESTOS", etc. — sin un bucket vacío/dominante). Ya no es un campo
    -- especulativo.
    OITM_V2."ItmsGrpCod"  AS "FamilyCode",
    OITB_V2."ItmsGrpNam"  AS "FamilyName",

    -- Categoría — la verificación de Familia reveló que el propio nombre de
    -- grupo ya trae una categoría de negocio en su prefijo: "PT ..." (Producto
    -- Terminado), "PP ..." (Producto en Proceso — ej. "PP DOMOS"/"PP VASOS" son
    -- la versión semi-elaborada de "PT DOMOS"/"PT VASOS"), "MP ..." (Materia
    -- Prima), "ME ..." (Material de Empaque). Grupos sin ese prefijo
    -- (REPUESTOS, EQUIPO DE COMPUTO, SUMINISTROS, MATERIALES VARIOS, FLETE,
    -- misceláneos) se dejan con su propio nombre de grupo como categoría.
    -- Esto reemplaza la heurística anterior (prefijo del CÓDIGO de item,
    -- PP/MP), que era menos confiable y ya no hace falta.
    -- ⚠️ Confirmar con Luigi/IT que "PP" = "Producto en Proceso" es la lectura
    -- correcta del prefijo (dato inferido de que cada familia "PP X" tiene una
    -- contraparte "PT X" en la misma lista — no viene de un catálogo de
    -- prefijos documentado).
    CASE
        WHEN UPPER(OITB_V2."ItmsGrpNam") LIKE 'PT %' THEN 'Producto Terminado (PT)'
        WHEN UPPER(OITB_V2."ItmsGrpNam") LIKE 'PP %' THEN 'Producto en Proceso (PP)'
        WHEN UPPER(OITB_V2."ItmsGrpNam") LIKE 'MP %' THEN 'Materia Prima (MP)'
        WHEN UPPER(OITB_V2."ItmsGrpNam") LIKE 'ME %' THEN 'Material de Empaque (ME)'
        WHEN OITB_V2."ItmsGrpNam" IS NOT NULL THEN OITB_V2."ItmsGrpNam"
        ELSE 'Sin familia'
    END AS "TipoItem",

    CASE
        WHEN D."CantidadRealTotal" >= D."CantidadPlaneada"
        THEN 'Cerrada'
        ELSE 'Abierta'
    END AS "EstadoOT",

    D."CantidadPlaneada"    AS "Cantidad Planeada",
    D."CantidadRealDia"     AS "Cantidad Real Día",
    D."CantidadRealRango"   AS "Cantidad Real Rango",
    D."CantidadRealTotal"   AS "Cantidad Hecha",

    D."HoraPlan"       AS "Hora Plan",
    D."HoraRealDia"    AS "Hora Real Día",
    D."HoraRealRango"  AS "Hora Real Rango",
    D."HoraRealOT"     AS "Hora Real OT",

    -- FIX 1: guard HoraPlan = 0  (antes: sin CASE → 50 / (0/11) explotaba)
    CASE
        WHEN COALESCE(D."HoraPlan", 0) > 0
        THEN ROUND(
                 D."CantidadPlaneada"
                 / (D."HoraPlan" / D."HorasTurno"),
             2)
    END AS "Pieza*turnoPlan",

    -- Sin cambio: ya tenía guard HoraRealDia > 0
    CASE
        WHEN D."HoraRealDia" > 0
        THEN ROUND(
                 D."CantidadRealDia"
                 / (D."HoraRealDia" / D."HorasTurno"),
             2)
    END AS "Pieza*turnoReal",

    -- FIX 2: añadido AND HoraPlan > 0 AND CantidadPlaneada > 0
    --        (el denominador interno era CantidadPlaneada/(HoraPlan/11) = 0 cuando HoraPlan=0)
    CASE
        WHEN D."HoraRealDia"  > 0
         AND COALESCE(D."HoraPlan", 0)          > 0
         AND COALESCE(D."CantidadPlaneada", 0)  > 0
        THEN ROUND(
                 (
                     (D."CantidadRealDia"  / (D."HoraRealDia"  / D."HorasTurno"))
                     /
                     (D."CantidadPlaneada" / (D."HoraPlan"      / D."HorasTurno"))
                 ) * 100,
             2)
    END AS "Eficiencia Dia",

    -- FIX 3: mismo patrón para el rango
    CASE
        WHEN D."HoraRealRango" > 0
         AND COALESCE(D."HoraPlan", 0)          > 0
         AND COALESCE(D."CantidadPlaneada", 0)  > 0
        THEN ROUND(
                 (
                     (D."CantidadRealRango" / (D."HoraRealRango" / D."HorasTurno"))
                     /
                     (D."CantidadPlaneada"  / (D."HoraPlan"       / D."HorasTurno"))
                 ) * 100,
             2)
    END AS "Eficiencia Rango"

FROM DETALLE D

LEFT JOIN OITM OITM_V2
    ON OITM_V2."ItemCode" = D."ItemID"
LEFT JOIN OITB OITB_V2
    ON OITB_V2."ItmsGrpCod" = OITM_V2."ItmsGrpCod"

ORDER BY
    D."Fecha",
    D."OT",
    D."PosicionOT",
    D."CodigoRecurso";


-- =============================================================================
-- VERIFICACIÓN DE FAMILIA — YA CORRIDA por Luigi (26/08/2026). Resultado: 58
-- grupos con nombres reales y con volumen, sin bucket vacío/dominante, ej.:
--   157 PT VASOS FOAM (10,309) · 129 PP DOMOS (12,203) · 143 PT CONTENEDORES
--   (10,525) · 162 REPUESTOS (5,385) · 110 MP BOBINA (835) · 108 ME MATERIAL
--   DE EMPAQUE (227) · 165 FLETE (1)
-- Familia queda CONFIRMADA como campo confiable. Se deja la consulta abajo
-- solo como referencia/reproducibilidad, no hace falta volver a correrla.
-- =============================================================================
-- SELECT
--     OITM_V2."ItmsGrpCod", OITB_V2."ItmsGrpNam", COUNT(*) AS Filas
-- FROM "IGN1" D
-- LEFT JOIN OITM OITM_V2 ON OITM_V2."ItemCode" = D."ItemCode"
-- LEFT JOIN OITB OITB_V2 ON OITB_V2."ItmsGrpCod" = OITM_V2."ItmsGrpCod"
-- GROUP BY OITM_V2."ItmsGrpCod", OITB_V2."ItmsGrpNam"
-- ORDER BY Filas DESC;
--
-- ⚠️ RECORDATORIO — igual que en query_2_machine_production_performance.sql:
-- los códigos/nombres de recurso (BEAS_APLATZ / CodigoRecurso-DescripcionRecurso)
-- fueron renombrados y reestructurados el 2026-05-01 (misma máquina física,
-- código distinto antes/después). Si el nuevo filtro de "Recurso" se usa con
-- un rango de fechas que cruza esa fecha, una misma máquina puede aparecer
-- como dos recursos distintos, o dos máquinas distintas bajo el mismo nombre
-- histórico. El dashboard v2 no filtra esto automáticamente — la vista muestra
-- un aviso cuando el rango de fechas seleccionado cruza el 2026-05-01.
