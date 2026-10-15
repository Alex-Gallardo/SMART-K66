# Cotizaciones: ejecución de base de datos

Los scripts de instalación `00`–`04` apuntan a la base de pruebas
`POS-SmartK66`. Los diagnósticos son de solo lectura. Los scripts `05` y `06`
documentan una auditoría y corrección puntual posterior en la base donde ya
se creó la cotización indicada; no forman parte de la instalación inicial.

## Fase 1 — diagnóstico

Ejecuta y comparte la salida completa de:

1. `00_diagnostico_sql_previo.sql` en SQL Server.
2. `HANA_00_diagnostico_catalogo_productos.sql` en SAP HANA.
3. Si `HANA_00` devuelve filas en `OSPP`, ejecuta también
   `HANA_01_diagnostico_precios_impuestos.sql`.
4. Ejecuta `HANA_02_diagnostico_determinacion_iva_precio.sql` después de
   `HANA_01` para confirmar los defaults fiscales y la fuente de precio que SAP
   aplicó en documentos reales.
5. Después de incorporar los resultados, ejecuta
   `HANA_03_smoke_consulta_final.sql`. Es una prueba de solo lectura sobre un
   caso real de BOLIK y valida la consulta definitiva usada por la aplicación.
6. Ejecuta `HANA_04_verificar_productos_venta.sql` para comprobar la regla de
   catálogo confirmada: se muestran únicamente artículos con `SellItem='Y'`,
   sin restringir códigos por prefijo, grupo, inventario ni `validFor`. Esto
   incluye artículos con existencia o disponible igual a cero.
7. Para diagnosticar precios en cero del caso `GRACO / CL0087`, ejecuta
   `HANA_05_diagnostico_precio_cero_CL0087.sql`. Compara `ITM1`, monedas
   adicionales, precios especiales y los procedimientos de `Pedidos_K66`.

La salida HANA confirma las columnas estándar de SAP Business One, las listas y
monedas utilizadas y si existen precios especiales en `OSPP`/`SPP1`. Esta
confirmación es necesaria antes de decidir si el precio base de `ITM1` debe ser
complementado por reglas especiales de cliente o cantidad.

Si `HANA_00` reporta precios especiales, ejecuta después
`HANA_01_diagnostico_precios_impuestos.sql`. Este segundo diagnóstico incluye
`SPP2` (escalas por cantidad), tasas reales de `OVTG`, descuentos generales y
la búsqueda de un posible schema SAP para `EMPAQUES`.

`HANA_02_diagnostico_determinacion_iva_precio.sql` es el diagnóstico final. Se
añadió porque `HANA_01` confirmó que los artículos activos no tienen un grupo
de IVA en `OITM`, mientras que sí existen precios especiales por cliente,
periodo y cantidad. El script compara la configuración con facturas y
cotizaciones SAP recientes sin modificar HANA.

`HANA_03_smoke_consulta_final.sql` debe devolver exactamente una fila, con
`GRUPO_IVA=IVA`, `TASA=12`, una `FUENTE` distinta de `SIN_PRECIO` y precios
bruto/neto mayores que cero.

`HANA_04_verificar_productos_venta.sql` devuelve un resumen y muestras de los
artículos incluidos y excluidos. Es de solo lectura; no crea procedimientos ni
modifica datos de SAP.

`HANA_05_diagnostico_precio_cero_CL0087.sql` también es de solo lectura. Sus
resultados distinguen un precio omitido por precedencia o moneda de un artículo
que realmente no tiene ninguna fuente de precio configurada en SAP.

## Decisiones confirmadas con los diagnósticos

- `OVTG` no contiene tasas en las tres compañías; los códigos comerciales
  vigentes están en `OSTC`: `IVA=12%` y `EXE=0%`.
- `OCRD.VatStatus` separa clientes afectos (`Y`) y exentos (`N`). Los documentos
  recientes confirman ambos comportamientos.
- Todos los clientes usan precio efectivo predeterminado (`EffecPrice=D`) y no
  comparan todas las fuentes (`EffcAllSrc=N`).
- Existen precios por cliente, vigencia y cantidad en `OSPP`/`SPP1`/`SPP2`, y
  grupos de descuento en `OEDG`/`EDG1`.
- Las fuentes SAP están expresadas como precio final con IVA en las compañías
  analizadas. Cotizaciones conserva ese importe y no retira ni agrega IVA.
  Los precios manuales siguen la misma regla. Si se captura un descuento
  adicional, se aplica sobre el precio final y el resultado continúa incluyendo
  IVA; `IMPUESTO_PORCENTAJE` queda como referencia SAP e `IMPUESTO_MONTO` es
  cero en las cotizaciones nuevas.

## Fase 2 — instalación en pruebas

Después de revisar el diagnóstico:

1. Ejecuta `01_crear_estructura_cotizaciones.sql`.
2. Ejecuta `02_configurar_permisos_menu.sql`.
3. Ejecuta `03_verificacion_post.sql` y comparte la salida.

Los scripts `01` y `02` son transaccionales, reejecutables y no contienen
`DROP`, `TRUNCATE` ni `DELETE`. Si detectan una estructura o configuración
incompatible, revierten su transacción y reportan el problema.

`Cotizaciones.OmitirPermisos` se entrega en `false` en
`DiamDev.Give.UI/Web.config`; los accesos dependen de los permisos creados por
el script `02`.

## Fase 3 — prospectos sin código SAP

En una base donde la estructura de Cotizaciones ya fue instalada, ejecuta:

1. `04_habilitar_prospectos_cotizaciones.sql`.
2. Verifica que el resultado indique `ID_CLIENTE`, `is_nullable = 1` y
   `ESTADO = LISTO`.

El script sólo cambia la nulabilidad de `dbo.COT_ENC.ID_CLIENTE`; no modifica
filas, índices, cálculos, impuestos ni objetos de SAP HANA. Es transaccional y
se puede ejecutar nuevamente sin producir cambios adicionales. En instalaciones
nuevas, `01_crear_estructura_cotizaciones.sql` ya crea esta columna como opcional.

## Auditoría de importes históricos

El cálculo vigente usa el precio unitario visible a dos decimales. No suma IVA
porque el precio recibido de SAP o capturado por el usuario ya lo incluye. El
descuento porcentual, cuando se ingresa explícitamente como descuento adicional,
sí se aplica a ese precio final.

Para el documento histórico `BOLIK / COT-BO-00000001`, cuyo PDF muestra
`144 × 13.12` pero conserva importes anteriores, ejecutar en este orden:

1. `05_auditar_totales_precio_visible.sql` en la base de producción. Es solo
   lectura; muestra el caso y otras cotizaciones con impuesto monetario legado.
2. Revisar que el caso coincida con el PDF: subtotal `1888.71`, IVA `226.65`,
   total `2115.36`, una línea de cantidad `144`, precio visible `13.12` y sin
   descuento adicional.
3. `06_corregir_COT_BO_00000001.sql` primero con `@Aplicar = 0`: muestra el
   resultado `1889.28` y revierte la transacción. El script exige que la base
   actual se llame `POS-SmartK66_DEV`; si la base real tiene otro nombre, hay
   que revisar y cambiar esta guarda de seguridad antes de ejecutarlo.
4. Si la simulación es correcta, cambiar únicamente `@Aplicar` a `1` y volver
   a ejecutar. Repetir `05` y regenerar el PDF para comprobar el resultado.

`06` no modifica otros documentos ni las tasas fiscales históricas: pone en
cero los importes monetarios de IVA de esta cotización y conserva el porcentaje
SAP de referencia. El script es reejecutable, pero no se debe utilizar para
otras cotizaciones sin verificar individualmente sus descuentos y precios.

## Lote histórico de 20 cotizaciones adicionales

`07_corregir_20_cotizaciones_historicas.sql` contiene exactamente los 20
documentos con impuesto monetario que aparecieron en la auditoría del
15/10/2026, sin incluir `COT-BO-00000001`. Su primera ejecución debe conservar
`@Aplicar = 0`; devuelve una comparación por documento y por línea, comprueba
la fórmula histórica y revierte la simulación. Comparar cada importe nuevo con
el anterior y solicitar aprobación comercial/de créditos antes de reemplazar
cotizaciones ya emitidas.

Es importante distinguir la causa: el código antiguo dividía el precio bruto
de SAP entre `1 + tasa/100` y luego sumaba IVA al total. Por ello, el cambio
propuesto *reduce* los importes existentes si el precio neto impreso se trata
ahora como precio final. No es una simple eliminación visual del IVA. Si esa
interpretación no está aprobada, no cambiar `@Aplicar` a `1`.

Si los 20 documentos figuran como `LISTO` y sus valores se aprueban, el script
requiere además `@Confirmacion = N'ACEPTO_REEMPLAZAR_20_TOTALES'`. Se ejecuta
en una sola transacción y crea respaldos completos en
`dbo.COT_RESPALDO_20261015_ENC` y `dbo.COT_RESPALDO_20261015_DET`; no toca HANA.
Si falta un documento, un precio visible es cero, un estado no es vigente o no
coinciden los cálculos anteriores, aborta todo el lote. La guarda de base exige
`POS-SmartK66_DEV`, el nombre mostrado en la auditoría compartida.
