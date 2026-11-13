# Analytics integrado en Gerencia Comercial

La nueva entrada /GerenciaComercial/Index adapta el frontend de [analytics-agents-k66](https://github.com/Alex-Gallardo/analytics-agents-k66), revisión 3c3e429829e51d97c08d17e99cf6557a7216f295. El HTML, diseño y gráficos SVG se adaptaron a Razor y a servicios C# de SMART-K66. No requiere Node.js, Express, otro puerto, otra sesión ni credenciales nuevas.

## Navegación

La presentación usa las superficies claras, bordes, controles y azul institucional de SMART-K66. Las clases internas llevan el prefijo gca- para impedir que el shell aplique márgenes de navegación al contenido del módulo. Las empresas se distinguen con acentos discretos en el selector. Los filtros avanzados y criterios de cobertura son desplegables; los filtros no aplicables a una vista se ocultan.
- Dashboard: ventas actuales/anteriores, crecimiento, conteos completos, tendencia mensual, empresas, tipo de venta y rankings.
- Venta general, Agentes, Clientes, Productos y Producto–Agente: búsqueda, filtros, orden y paginación en SAP.
- Dashboard de agentes: selector por empresa y SlpCode, indicadores, tendencia, diez clientes principales y diez resultados producto/unidad. “Explorar todos los productos” abre el detalle paginado del agente.
- Consultas operativas y reportes: /GerenciaComercial/Operaciones conserva el panel previo, sus consultas SAP/SQL, análisis y reportes existentes.

## Datos y criterios
Se consultan OINV/INV1 menos ORIN/RIN1, sin documentos cancelados, solo documentos de artículos y líneas con ItemCode. Las fechas inclusivas usan DocDate; la comparación corresponde a las mismas fechas del año anterior y admite hasta un año. La tendencia conserva mes y año para rangos que cruzan diciembre.

GTQ usa LineTotal para documentos Q/QTZ/GTQ; USD usa TotalFrgn para documentos USD. No se mezclan monedas ni se convierten en el navegador. Esta base de líneas puede diferir de los totales de encabezado del panel operativo por descuentos y ajustes; no representa una conciliación contable con ese panel.

La cobertura predeterminada es Completa. “Exclusiones Analytics” reproduce las exclusiones explícitas del origen: cliente CL0149 y vendedores OTROS/EMPLEADOS. Local/Exportación conserva el criterio del origen: prefijo CL del código del cliente. No equivale al frente comercial del usuario.

Los agentes se identifican con Empresa|SlpCode; nombres iguales no se fusionan. Los conteos de clientes y productos distinguen empresa y solo cuentan líneas con importe actual no cero. Las cantidades se agrupan por unidad de venta, sin sumar medidas distintas. La participación usa el total de la empresa y moneda antes de filtrar el detalle. Base anterior cero muestra N/D; base negativa usa el valor absoluto para calcular variación. Las participaciones con importes negativos se muestran como importes en lugar de un anillo engañoso.

## Servicios y exportaciones
Todos los endpoints heredan Control.GerenciaComercial.Ver y Authorize del controlador. No se crean permisos nuevos.
- AnalyticsPanel: general, tendencia, agentes, cinco resultados producto/unidad y conteos. Máximo 10,000 agentes, con error visible si se supera.
- AnalyticsDetalle: hasta 100 filas por página, filtros y orden parametrizados o restringidos a columnas conocidas.
- AnalyticsPerfil: ventas, tendencia, clientes principales agregados por cliente y productos separados por medida.
- AnalyticsExcel: vuelve a consultar SAP con los filtros; hasta 10,000 filas por hoja. Exporta números como números y neutraliza textos interpretables como fórmulas. Dashboard exporta cinco hojas; perfil exporta agentes, clientes y producto-agente.
- PDF / Imprimir vista: usa el diálogo del navegador para imprimir o guardar la vista actual como PDF. Indica empresa, fechas, moneda, cobertura y alcance de página. Las tablas imprimen la página visible; Excel contiene el detalle filtrado. Los reportes PDF completos anteriores siguen en Consultas operativas.

Las consultas de datos son SELECT, con schemas HanaSchema.* existentes y parámetros ODBC. SQL POS se mantiene disponible en Consultas operativas y no se suma a SAP. Se eliminaron los datasets ilustrativos del frontend; un fallo de fuente se informa y deshabilita exportación.

## Verificación
Ejecutar Tests/GerenciaComercial/run.ps1 para compilación, filtros, permisos, SQL generado, PDF previo, Razor y pruebas existentes. Ejecutar Tests/GerenciaComercial/analytics-browser-smoke.ps1 para las siete vistas, búsqueda, paginación, identidad de agentes, moneda, texto seguro, estilos y apertura de impresión en Edge con datos simulados. El script conserva el HTML de prueba en TEMP para revisión visual; esos datos no se despliegan.

La prueba de navegador carga las hojas de estilo del layout real, incluyendo Bootstrap, main.css, main-responsive.css, Site.css y ui-shell.css, dentro de una estructura con encabezado, menú y contenedor de contenido. Ejecuta las siete vistas a 1440, 1366, 1024, 768 y 390 px, menú compacto y zoom del 125 %. Comprueba alineación sin doble margen, superficies claras, importes largos, foco y activación por teclado, filtros desplegables, estados vacíos/error/recuperación, impresión sin margen lateral y ausencia de desbordamiento fuera de las tablas. El helper analytics-layout-browser.test.js usa DevTools de Edge sin paquetes adicionales y guarda capturas de dashboard, clientes y perfil.

La comprobación contra datos reales y tiempos de respuesta de HANA requiere el entorno conectado de SMART-K66. Las pruebas locales no certifican importes reales ni planes de ejecución de SAP.
