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
- **Importe de productos**: proviene de `INV1` menos `RIN1`, utilizando `LineTotal` o `TotalFrgn` según la moneda del documento, sin impuesto. Su participación usa como denominador la venta neta completa de la empresa; los ajustes de encabezado pueden hacer que la suma de participaciones de productos difiera de 100 %.
- **Informe ejecutivo**: requiere una sola empresa y moneda. Genera la versión monetaria o porcentual con resultados por agente y principales clientes y productos en crecimiento/caída. La versión porcentual no contiene importes monetarios. El navegador permite imprimir o guardar como PDF. «Cierre del último mes» selecciona el último mes calendario completo y prepara el informe.
- **Excel**: el comparativo filtrado se exporta hasta 10,000 filas; los valores de texto se neutralizan contra fórmulas.

## Avances de ventas por agente y períodos libres

- La sección a ancho completo de `/GerenciaComercial/Index` compara entre 2 y 12 períodos de hasta un año cada uno, dentro de los últimos diez años calendario. Los períodos se pueden añadir, editar, ordenar y quitar. Por defecto se presentan los avances del año actual y el anterior al mismo día de corte. El año en curso termina en la fecha actual y se identifica como parcial.
- La comparación usa una empresa (Bolik, Faes o Graco) y una moneda (GTQ o USD) por consulta. El eje vertical muestra agentes SAP y el horizontal ventas netas en la moneda seleccionada. Barras agrupa las series por agente; Líneas conecta los valores de cada agente entre períodos. Se muestran 12 agentes por defecto y se puede desplegar el listado completo.
- El endpoint `Avances` agrupa `OINV` menos `ORIN` por `OSLP.SlpCode` y período, sin impuesto y por fecha de documento. Incluye agentes activos sin movimientos y agentes inactivos con movimientos. `Usuario_Empresa` clasifica Sala de Ventas, Gerencia y Exportaciones con el mismo mapeo del comparativo existente. Ninguna venta POS se suma a SAP.
- Las unidades netas provienen de `INV1` menos `RIN1` y se agrupan por `unitMsr`. Si existen medidas distintas, no se presenta un total combinado. El Excel contiene una hoja de ventas por agente y otra de unidades por medida.
- Los KPI, lectura ejecutiva, alertas y panorama se calculan sobre la misma respuesta de SAP. El cambio absoluto y porcentual compara los últimos dos períodos en el orden elegido; también se muestra el último frente al período base. Con base cero se indica «Nuevo / sin base». Si los períodos tienen duraciones distintas se advierte expresamente.
- Las alertas señalan la mayor caída absoluta, caídas de al menos 20 % con impacto mínimo de 1 % sobre el resultado actual, concentración de los tres mayores agentes de al menos 60 %, y resultados netos no positivos. Son reglas visibles y deterministas para orientar la revisión comercial.
- El acceso sigue protegido por `Control.GerenciaComercial.Ver`; todas las fuentes se consultan en modo lectura.

Los campos de contraseña y binarios no se envían al navegador. Si una fuente no responde, el resumen muestra una advertencia y conserva los indicadores disponibles. Los errores técnicos se registran en el servidor.

## Verificación

Ejecutar `Tests/GerenciaComercial/run.ps1` para compilar las clases nuevas y validar filtros, privacidad, exportación, sintaxis Razor y la interfaz de avances. El script también comprueba la sintaxis de los tres archivos JavaScript del módulo.

El panel reutiliza `HanaHelper`, la configuración `HanaSchema.*` y `GiveContext` que ya emplean los módulos existentes. La verificación de datos en un entorno conectado forma parte de la revisión de integración y no requiere instalar componentes adicionales del panel.
