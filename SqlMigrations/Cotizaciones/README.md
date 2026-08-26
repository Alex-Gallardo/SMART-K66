# Cotizaciones: ejecución de base de datos

Todos los scripts SQL Server apuntan explícitamente a la base de pruebas
`POS-SmartK66`. Ningún diagnóstico elimina o modifica datos.

## Fase 1 — diagnóstico

Ejecuta y comparte la salida completa de:

1. `00_diagnostico_sql_previo.sql` en SQL Server.
2. `HANA_00_diagnostico_catalogo_productos.sql` en SAP HANA.
3. Si `HANA_00` devuelve filas en `OSPP`, ejecuta también
   `HANA_01_diagnostico_precios_impuestos.sql`.
4. Ejecuta `HANA_02_diagnostico_determinacion_iva_precio.sql` después de
   `HANA_01` para confirmar los defaults fiscales y la fuente de precio que SAP
   aplicó en documentos reales.

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
