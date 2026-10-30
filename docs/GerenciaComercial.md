# Panel de Gerencia Comercial

Ruta: `/GerenciaComercial`. El menú y todos los endpoints del controlador exigen `[Authorize]` y el permiso existente `Control.GerenciaComercial.Ver`. El permiso se administra con los roles actuales; este módulo no crea permisos adicionales. Todas las consultas son `SELECT` sobre SAP HANA y `POS-SmartK66_DEV`.

## Fuentes y alcance

| Sección | Fuente | Tablas | Alcance |
| --- | --- | --- | --- |
| Ventas netas | SAP HANA | `OINV`, `ORIN` | Facturas vigentes menos notas de crédito vigentes por fecha de documento y moneda. |
| Cobros | SAP HANA | `ORCT` | Pagos no cancelados por fecha de documento; se muestran separados de las ventas. |
| Pedidos | SAP HANA | `ORDR` | Creados en el período y abiertos actualmente, excluyendo cancelados. |
| Productos más vendidos | SAP HANA | `INV1`, `RIN1`, `OINV`, `ORIN` | Cantidad facturada menos cantidad acreditada por empresa y código. |
| Agentes y clientes destacados | SAP HANA | `OSLP`, `OINV`, `ORIN` | Ventas netas por empresa y moneda. |
| Catálogos y perfiles | SAP HANA | `OCRD`, `OITM`, `OSLP`, `OITB`, `CRD1`, `OCPR`, `OITW`, `ITM1` | Todos los agentes, clientes y productos, incluidos inactivos. El detalle muestra campos de SAP y hasta 100 registros relacionados. |
| Catálogos y pedidos K66 | SQL Server | `Cliente`, `Producto`, `Vendedor`, `Pedido_K66`, `Pedido_Detalle_K66` | Datos locales separados de SAP para evitar duplicar ventas o pedidos sincronizados. |
| Clasificación de agentes | SQL Server + SAP HANA | `Usuario_Empresa`, `OSLP` | El prefijo numérico de `Usuario_Empresa.Codigo` se contrasta con `OSLP.SlpCode` dentro de la empresa. Los nombres exactos Sala de Ventas, Gerencia y Exportaciones forman los frentes; el resto se muestra como agente comercial. Si SQL no responde, se usa el nombre SAP y se avisa. |

Las tres empresas se resuelven mediante las claves existentes `HanaSchema.BOLIK`, `HanaSchema.FAES` y `HanaSchema.GRACO`. FAES usa el schema configurado en el entorno. Los identificadores de empresa de SQL se limitan a Bolik, Faes y Graco.

## Definiciones y uso

- **Ventas netas**: `DocTotal - VatSum` para moneda local y `DocTotalFC - VatSumFC` para moneda extranjera, restando las notas de crédito. Se excluyen documentos cancelados. La fecha es `DocDate`.
- **Moneda local**: SAP puede devolver `QTZ` o `Q`; el panel las presenta como `GTQ`. GTQ y USD nunca se suman entre sí. Cualquier otra moneda permanece separada en los datos.
- **Cobros**: total de `ORCT` no cancelados. Representan pagos recibidos, no una segunda venta.
- **Pedidos K66 pendientes**: pedidos con `Sincronizado = 0` en SQL durante el período. Los pedidos SAP abiertos son un indicador diferente.
- **Productos más vendidos**: cantidad neta por producto; las unidades pueden variar entre productos. El ranking no convierte unidades de medida.
- **Fecha de consulta**: se muestra en el encabezado. El resumen se conserva durante dos minutos; **Actualizar** fuerza una nueva lectura.
- **Explorador**: búsqueda y filtros por empresa, moneda, estado y, donde corresponda, código de agente y cliente. Paginación en el servidor, 25/50/100 filas.
- **Excel**: exporta el conjunto filtrado del explorador hasta 10,000 filas. Valores que podrían interpretarse como fórmulas se neutralizan.
- **Rango**: hasta cinco años para documentos; los catálogos muestran todos los registros independientemente del rango.

## Análisis de desempeño e informe ejecutivo

- **Comparativo**: agentes, clientes y productos con movimientos en el período seleccionado o en las mismas fechas del año anterior. Admite períodos de hasta un año. Incluye importes de ambos períodos, diferencia, variación porcentual, participación, búsqueda, orden y paginación. Los agentes pueden filtrarse por frente.
- **Histórico individual**: desde cada fila se consultan hasta cinco años de movimientos mensuales para el código y empresa seleccionados. Los productos también muestran cantidad neta mensual.
- **Participación**: el denominador es el total actual de la empresa y moneda seleccionadas, calculado antes de aplicar la búsqueda. Nunca se suman monedas distintas. Si el período anterior es cero, la variación se marca como «Nuevo / sin base».
- **Importe de productos**: proviene de `INV1` menos `RIN1`, utilizando `LineTotal` o `TotalFrgn` según la moneda del documento, sin impuesto. Los ajustes de encabezado pueden hacer que su suma difiera del total neto de facturas.
- **Informe ejecutivo**: requiere una sola empresa y moneda. Genera la versión monetaria o porcentual con resultados por agente y principales clientes y productos en crecimiento/caída. La versión porcentual no contiene importes monetarios. El navegador permite imprimir o guardar como PDF. «Cierre del último mes» selecciona el último mes calendario completo y prepara el informe.
- **Excel**: el comparativo filtrado se exporta hasta 10,000 filas; los valores de texto se neutralizan contra fórmulas.

Los campos de contraseña y binarios no se envían al navegador. Si una fuente no responde, el resumen muestra una advertencia y conserva los indicadores disponibles. Los errores técnicos se registran en el servidor.

## Verificación

Ejecutar `Tests/GerenciaComercial/run.ps1` para compilar las clases nuevas y validar filtros, privacidad, exportación y sintaxis Razor. Ejecutar `node --check DiamDev.Give.UI/Scripts/App/gerencia-comercial.js` para validar JavaScript.

El panel reutiliza `HanaHelper`, la configuración `HanaSchema.*` y `GiveContext` que ya emplean los módulos existentes. La verificación de datos en un entorno conectado forma parte de la revisión de integración y no requiere instalar componentes adicionales del panel.
