# Validación del importe pagado de Borradores NC

La migración `10_corregir_pagado_moneda_factura_hana.sql` parte de las tres
definiciones de producción compartidas el 03/11/2026. Conserva sus columnas,
filtros, rangos de fechas y `WITH READ ONLY`. Se aplica directamente a esas
vistas; no es necesario ejecutar antes la migración histórica 09, que agrega
un límite de fecha distinto para `EXPORTACION`. Corrige el par de
importes que consume la aplicación: ambos deben estar en la moneda de `DocCur`.

| `OINV.DocCur` | Total de la vista | Pagado de la vista |
| --- | --- | --- |
| `QTZ` | `OINV.DocTotal` | `OINV.PaidToDate` |
| Otra moneda | `OINV.DocTotalFC` | `OINV.PaidFC` |

La columna de salida sigue llamándose `PaidToDate` para conservar el contrato
con `HanaRepository.ObtenerFacturasBorradorNc`. No debe interpretarse como el
campo bruto `OINV.PaidToDate` cuando `DocCur` es extranjero.

## Comprobar un reporte antes de desplegar

Anotar empresa, número de factura, código de cliente, agente, número de
borrador y fecha del aviso. Usar el esquema correspondiente: `SBOBOLIK`
(BOLIK), `SBOESCOCESA` (FAES) o `SBO_GRACO` (GRACO). En las consultas siguientes,
reemplazar `<ESQUEMA>`, `<NUMERO>` y `<CLIENTE>` con esos datos. Son consultas
de solo lectura.

```sql
-- SAP HANA: importes originales y estado actual de la factura.
SELECT "DocEntry", "DocNum", "CardCode", "DocDate", "DocCur",
       "DocStatus", "CANCELED", "DocTotal", "PaidToDate",
       "DocTotalFC", "PaidFC"
FROM "<ESQUEMA>"."OINV"
WHERE "DocNum" = <NUMERO> AND "CardCode" = '<CLIENTE>';

-- SAP HANA: pareja de importes que recibe la aplicación.
SELECT "Empresa", "SlpName", "DocNum", "CardCode", "DocDate",
       "DocCur", "DocTotal", "PaidToDate"
FROM "<ESQUEMA>"."RC_FACTURAS_BORRNC"
WHERE "DocNum" = <NUMERO> AND "CardCode" = '<CLIENTE>';
```

Si `OINV` devuelve más de una factura con el mismo `DocNum` y `CardCode`,
identificar la correcta por `DocEntry` y fecha; la vista no expone `DocEntry`.
Si la vista no devuelve filas, revisar el filtro de agente, el período y
`CANCELED` en su definición actual. Consultar en SQL Server la fotografía que
se guardó al crear el borrador:

```sql
SELECT ID_EMPRESA, ID_BORRADOR, DOCUMENTO, MONEDA,
       TOTAL_FACT, PAGADO, IMPORTE
FROM dbo.BORR_NC_DET
WHERE ID_EMPRESA = '<EMPRESA>' AND ID_BORRADOR = '<BORRADOR>'
  AND DOCUMENTO = '<NUMERO>';
```

Una vista con `DocTotal = OINV.DocTotalFC` y
`PaidToDate = OINV.PaidToDate` compara monedas distintas. Si la pareja de la
vista es correcta pero `PAGADO >= TOTAL_FACT` en el registro histórico, revisar
el estado y las transacciones aplicadas o conciliaciones en SAP en la fecha del
borrador. Los importes actuales de `OINV` pueden haber cambiado después.

## Despliegue y comprobación

1. Guardar la definición vigente de cada vista en HANA y comparar sus filtros
   con la migración 10. Si hay cambios de producción posteriores a la migración
   09, integrarlos antes de reemplazar la vista.
2. Ejecutar individualmente los tres `CREATE OR REPLACE VIEW` de la migración
   10. No ejecutar escrituras sobre `OINV` ni sobre `BORR_NC_DET`.
3. Verificar que las tres vistas estén válidas con el `SELECT` final de la
   migración. Repetir las consultas de arriba con facturas locales y extranjeras
   de `EXPORTACION` y de otros agentes; confirmar que total y pagado coincidan
   con el par de campos esperado de `OINV`.
4. Crear un borrador de prueba con una factura extranjera parcialmente aplicada.
   Debe mostrarse como pendiente y no generar el aviso de pagada. Una factura
   totalmente aplicada debe conservar el aviso. Confirmar que los rangos de
   fechas y el filtro de agentes siguen iguales.

Si la validación falla, restaurar las definiciones respaldadas de las vistas.
La migración 09 no es un respaldo de posibles cambios posteriores en producción.
