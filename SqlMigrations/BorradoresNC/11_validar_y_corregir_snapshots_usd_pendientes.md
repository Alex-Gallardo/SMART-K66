# Corrección puntual de pagos históricos en borradores USD

La migración HANA 10 corrigió la fuente para borradores nuevos. Al guardar,
`BorradorNcBLL` vuelve a consultar HANA y copia el importe a `BORR_NC_DET.PAGADO`.
Autorizaciones muestra «Pagada al crear» si el importe guardado es mayor o igual
que `TOTAL_FACT`; no vuelve a consultar SAP. Por eso los borradores anteriores
a la migración conservan el aviso hasta corregir su fotografía histórica.

## Alcance comprobado el 04/11/2026

Hasta las 12:49:56 del servidor SQL del 04/11/2026 no se habían registrado
borradores ese día: el último `BORR_NC_ENC.REGISTRO` era del 03/11/2026 a las
19:16:33. La consulta
de todos los borradores pendientes USD/EUR devolvió siete líneas, todas
creadas el 28/10/2026. En seis, `BORR_NC_DET.PAGADO` coincidía exactamente
con `OINV.PaidToDate` actual en moneda local. `TOTAL_FACT` coincidía con
`OINV.DocTotalFC`; `OINV.PaidFC` era inferior al total USD y la factura seguía
abierta. Son seis registros con importes de monedas mezcladas:

| Borrador | ROWID | Factura | Pagado guardado (local) | PaidFC (USD) |
| --- | ---: | ---: | ---: | ---: |
| BWF-00037 | 227 | 2000607 | 165844.780 | 21629.520 |
| BWG-00017 | 220 | 2001463 | 17148.780 | 2250.960 |
| BWG-00016 | 218 | 2001424 | 89971.230 | 11803.530 |
| BWG-00015 | 216 | 2001384 | 194340.200 | 25489.880 |
| BWG-00014 | 214 | 2001368 | 23202.970 | 3040.540 |
| BWG-00013 | 213 | 2001324 | 134237.060 | 17571.170 |

El séptimo, **BWF-00035 / ROWID 207**, queda excluido. Su `PAGADO` guardado
era 119520.970, mientras que `OINV.PaidToDate` actual era 132695.230 y
`OINV.PaidFC` actual 17406.260. No se conoce `PaidFC` a la hora de creación.
Revisar en SAP las aplicaciones y conciliaciones de la factura FAES 2000777
(`DocEntry` 22031, cliente CE0011) hasta el 28/10/2026 a las 12:08:31.
Solo después de obtener el pagado USD histórico se podrá preparar otra
corrección. No sustituirlo por el pagado actual.

## Antes de ejecutar

1. Confirmar en SAP que, para los seis documentos de la tabla, `DocTotalFC`,
   `PaidToDate` y `PaidFC` aún coinciden con la evidencia y que no hubo pagos,
   NC o conciliaciones posteriores a la creación. Igualdad del valor local
   actual con el guardado es un indicio fuerte, pero no sustituye esa revisión.
2. Confirmar en SQL Server que los seis borradores siguen `PENDIENTE` y que
   sus importes guardados no cambiaron. El script también comprueba esto y
   aborta sin actualizar si alguna fila difiere.
3. Ejecutar
   [`11_corregir_snapshots_usd_pendientes_20261104.sql`](11_corregir_snapshots_usd_pendientes_20261104.sql)
   con `@Aplicar = 0`. Debe mostrar seis filas propuestas y
   `SIMULADO_SIN_CAMBIOS`; después del `ROLLBACK` no habrá respaldo ni cambios.
4. Si las seis filas son correctas, cambiar `@Aplicar = 1` y ejecutar el script
   completo una sola vez. Debe devolver `APLICADO`. La tabla permanente
   `dbo.BORR_NC_DET_RESPALDO_PAGADO_20261104` conservará las filas originales,
   el valor destino, usuario y fecha de ejecución.
5. Volver a abrir cada borrador en Autorizaciones. El aviso falso debe
   desaparecer. Revisar por separado el importe solicitado frente al saldo
   pendiente; la regla actual permite solicitar más que ese saldo.

No hay cambio de código de aplicación en esta corrección. Para validar el flujo
nuevo de extremo a extremo, crear un borrador de prueba después de la migración
con una factura USD parcialmente pagada y comprobar que `PAGADO` guardado
coincide con `OINV.PaidFC`, no con `OINV.PaidToDate` local.

El control de solo lectura
[`11_auditar_moneda_vistas_hana_solo_lectura.sql`](11_auditar_moneda_vistas_hana_solo_lectura.sql)
compara todas las filas de las tres vistas con `OINV`. Debe devolver cero
filas; ejecutarlo de nuevo si se cambia alguna definición de vista o se
reporta una alerta nueva.
