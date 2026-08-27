/* =============================================================================
   SAP HANA — SMOKE TEST DE LA CONSULTA FINAL DE COTIZACIONES

   Caso real de precio observado en HANA_02 (el prefijo PT se usa solamente
   como dato de esta muestra, no como regla de visibilidad del catálogo):
     Empresa BOLIK, cliente CL0023, artículo PT00054, cantidad 300.

   Objetivo: validar en HANA la misma sintaxis y prioridad utilizada por la
   aplicación. Debe devolver exactamente una fila, IVA/12 y una fuente de
   precio distinta de SIN_PRECIO. No crea ni modifica objetos.
   ============================================================================= */

SELECT X.*,
       ROUND(CASE WHEN X."TASA" > 0
                  THEN X."PRECIO_BRUTO" / (1 + X."TASA" / 100)
                  ELSE X."PRECIO_BRUTO" END, 6) AS "PRECIO_NETO"
FROM (
    SELECT 'H30_SMOKE_BOLIK' AS "SECCION",
           I."ItemCode" AS "ITEM",
           C."CardCode" AS "CLIENTE",
           Q."Cantidad" AS "CANTIDAD",
           C."ListNum" AS "LISTA",
           CASE WHEN COALESCE(C."VatStatus", 'Y')='N'
                THEN 'EXE' ELSE 'IVA' END AS "GRUPO_IVA",
           COALESCE(T."Rate", 0) AS "TASA",
           COALESCE(
               ECQ."Price", ECP."Price", EC."Price",
               CASE WHEN (SELECT MAX(DR."Discount")
                               FROM "SBOBOLIK"."OEDG" DH
                               INNER JOIN "SBOBOLIK"."EDG1" DR
                                       ON DR."AbsEntry"=DH."AbsEntry"
                              WHERE COALESCE(DH."ValidFor", 'Y')='Y'
                                AND (DH."ValidForm" IS NULL OR
                                     DH."ValidForm"<=CURRENT_DATE)
                                AND (DH."ValidTo" IS NULL OR
                                     DH."ValidTo">=CURRENT_DATE)
                                AND (DH."Type"='A' OR
                                     (DH."Type"='S' AND
                                      DH."ObjCode"=C."CardCode") OR
                                     (DH."Type"='C' AND
                                      DH."ObjCode"=TO_NVARCHAR(C."GroupCode")))
                                AND DR."DiscType"='D'
                                AND ((DR."ObjType"='4' AND
                                      DR."ObjKey"=I."ItemCode") OR
                                     (DR."ObjType"='52' AND
                                      DR."ObjKey"=TO_NVARCHAR(I."ItmsGrpCod")) OR
                                     (DR."ObjType"='43' AND
                                      DR."ObjKey"=TO_NVARCHAR(I."FirmCode"))))
                         IS NOT NULL
                    THEN P."Price" * (1 -
                         (SELECT MAX(DR."Discount")
                            FROM "SBOBOLIK"."OEDG" DH
                            INNER JOIN "SBOBOLIK"."EDG1" DR
                                    ON DR."AbsEntry"=DH."AbsEntry"
                           WHERE COALESCE(DH."ValidFor", 'Y')='Y'
                             AND (DH."ValidForm" IS NULL OR
                                  DH."ValidForm"<=CURRENT_DATE)
                             AND (DH."ValidTo" IS NULL OR
                                  DH."ValidTo">=CURRENT_DATE)
                             AND (DH."Type"='A' OR
                                  (DH."Type"='S' AND
                                   DH."ObjCode"=C."CardCode") OR
                                  (DH."Type"='C' AND
                                   DH."ObjCode"=TO_NVARCHAR(C."GroupCode")))
                             AND DR."DiscType"='D'
                             AND ((DR."ObjType"='4' AND
                                   DR."ObjKey"=I."ItemCode") OR
                                  (DR."ObjType"='52' AND
                                   DR."ObjKey"=TO_NVARCHAR(I."ItmsGrpCod")) OR
                                  (DR."ObjType"='43' AND
                                   DR."ObjKey"=TO_NVARCHAR(I."FirmCode")))) / 100)
               END,
               WLQ."Price", WLP."Price", WL."Price", P."Price", 0
           ) AS "PRECIO_BRUTO",
           CASE
               WHEN ECQ."Price" IS NOT NULL THEN 'CLIENTE_CANTIDAD'
               WHEN ECP."Price" IS NOT NULL THEN 'CLIENTE_PERIODO'
               WHEN EC."Price" IS NOT NULL THEN 'CLIENTE'
               WHEN (SELECT MAX(DR."Discount")
                         FROM "SBOBOLIK"."OEDG" DH
                         INNER JOIN "SBOBOLIK"."EDG1" DR
                                 ON DR."AbsEntry"=DH."AbsEntry"
                        WHERE COALESCE(DH."ValidFor", 'Y')='Y'
                          AND (DH."ValidForm" IS NULL OR
                               DH."ValidForm"<=CURRENT_DATE)
                          AND (DH."ValidTo" IS NULL OR
                               DH."ValidTo">=CURRENT_DATE)
                          AND (DH."Type"='A' OR
                               (DH."Type"='S' AND
                                DH."ObjCode"=C."CardCode") OR
                               (DH."Type"='C' AND
                                DH."ObjCode"=TO_NVARCHAR(C."GroupCode")))
                          AND DR."DiscType"='D'
                          AND ((DR."ObjType"='4' AND
                                DR."ObjKey"=I."ItemCode") OR
                               (DR."ObjType"='52' AND
                                DR."ObjKey"=TO_NVARCHAR(I."ItmsGrpCod")) OR
                               (DR."ObjType"='43' AND
                                DR."ObjKey"=TO_NVARCHAR(I."FirmCode"))))
                    IS NOT NULL THEN 'GRUPO_DESCUENTO'
               WHEN WLQ."Price" IS NOT NULL THEN 'LISTA_CANTIDAD'
               WHEN WLP."Price" IS NOT NULL THEN 'LISTA_PERIODO'
               WHEN WL."Price" IS NOT NULL THEN 'LISTA_ESPECIAL'
               WHEN P."Price" IS NOT NULL THEN 'LISTA'
               ELSE 'SIN_PRECIO'
           END AS "FUENTE"
      FROM "SBOBOLIK"."OITM" I
      INNER JOIN (
          SELECT 'PT00054' AS "ItemCode",
                 CAST(300 AS DECIMAL(19,6)) AS "Cantidad"
            FROM DUMMY
      ) Q ON Q."ItemCode"=I."ItemCode"
      INNER JOIN "SBOBOLIK"."OCRD" C
              ON C."CardCode"='CL0023' AND C."CardType"='C'
      LEFT JOIN "SBOBOLIK"."ITM1" P
             ON P."ItemCode"=I."ItemCode" AND P."PriceList"=C."ListNum"
      LEFT JOIN "SBOBOLIK"."OSTC" T
             ON T."Code"=CASE WHEN COALESCE(C."VatStatus", 'Y')='N'
                              THEN 'EXE' ELSE 'IVA' END
      LEFT JOIN "SBOBOLIK"."OSPP" EC
             ON EC."ItemCode"=I."ItemCode" AND EC."CardCode"=C."CardCode"
            AND (COALESCE(EC."Valid", 'N')='N' OR
                 ((EC."ValidFrom" IS NULL OR EC."ValidFrom"<=CURRENT_DATE) AND
                  (EC."ValidTo" IS NULL OR EC."ValidTo">=CURRENT_DATE)))
      LEFT JOIN "SBOBOLIK"."SPP1" ECP
             ON ECP."ItemCode"=EC."ItemCode" AND ECP."CardCode"=EC."CardCode"
            AND (ECP."FromDate" IS NULL OR ECP."FromDate"<=CURRENT_DATE)
            AND (ECP."ToDate" IS NULL OR ECP."ToDate">=CURRENT_DATE)
      LEFT JOIN "SBOBOLIK"."SPP2" ECQ
             ON ECQ."ItemCode"=EC."ItemCode" AND ECQ."CardCode"=EC."CardCode"
            AND ECQ."SPP1LNum"=ECP."LINENUM" AND ECQ."Amount"<=Q."Cantidad"
            AND (ECQ."UomEntry" IS NULL OR ECQ."UomEntry"=-1 OR
                 ECQ."UomEntry"=P."UomEntry")
      LEFT JOIN "SBOBOLIK"."SPP2" ECQN
             ON ECQN."ItemCode"=ECQ."ItemCode"
            AND ECQN."CardCode"=ECQ."CardCode"
            AND ECQN."SPP1LNum"=ECQ."SPP1LNum"
            AND ECQN."Amount">ECQ."Amount" AND ECQN."Amount"<=Q."Cantidad"
            AND (ECQN."UomEntry" IS NULL OR ECQN."UomEntry"=-1 OR
                 ECQN."UomEntry"=P."UomEntry")
      LEFT JOIN "SBOBOLIK"."OSPP" WL
             ON WL."ItemCode"=I."ItemCode"
            AND WL."CardCode"='*' || TO_NVARCHAR(C."ListNum")
            AND (COALESCE(WL."Valid", 'N')='N' OR
                 ((WL."ValidFrom" IS NULL OR WL."ValidFrom"<=CURRENT_DATE) AND
                  (WL."ValidTo" IS NULL OR WL."ValidTo">=CURRENT_DATE)))
      LEFT JOIN "SBOBOLIK"."SPP1" WLP
             ON WLP."ItemCode"=WL."ItemCode" AND WLP."CardCode"=WL."CardCode"
            AND (WLP."FromDate" IS NULL OR WLP."FromDate"<=CURRENT_DATE)
            AND (WLP."ToDate" IS NULL OR WLP."ToDate">=CURRENT_DATE)
      LEFT JOIN "SBOBOLIK"."SPP2" WLQ
             ON WLQ."ItemCode"=WL."ItemCode" AND WLQ."CardCode"=WL."CardCode"
            AND WLQ."SPP1LNum"=WLP."LINENUM" AND WLQ."Amount"<=Q."Cantidad"
            AND (WLQ."UomEntry" IS NULL OR WLQ."UomEntry"=-1 OR
                 WLQ."UomEntry"=P."UomEntry")
      LEFT JOIN "SBOBOLIK"."SPP2" WLQN
             ON WLQN."ItemCode"=WLQ."ItemCode"
            AND WLQN."CardCode"=WLQ."CardCode"
            AND WLQN."SPP1LNum"=WLQ."SPP1LNum"
            AND WLQN."Amount">WLQ."Amount" AND WLQN."Amount"<=Q."Cantidad"
            AND (WLQN."UomEntry" IS NULL OR WLQN."UomEntry"=-1 OR
                 WLQN."UomEntry"=P."UomEntry")
     WHERE I."SellItem"='Y'
       AND ECQN."ItemCode" IS NULL AND WLQN."ItemCode" IS NULL
) X;
