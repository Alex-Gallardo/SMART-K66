# Facturas reutilizables e indicadores NC de SAP

## Comportamiento

- Una factura abierta puede incluirse en varios borradores, aunque los importes de otros borradores sumen su total o lo superen. Se informa qué borradores vigentes la incluyen; no se reserva saldo.
- Cada factura aparece una sola vez **dentro del mismo borrador**. Su importe individual debe ser positivo y no superar su total, validado contra SAP. Se conservan la transacción, el correlativo, los permisos, la bitácora y las demás reglas de creación.
- Sigue vigente `BorradorNC.MostrarFacturasPagadas=false`: cualquier diferencia pendiente conserva una factura abierta, sin tolerancia de pago.
- NC vigentes se consultan contra ORIN y `INF_VRC_FACRNC`, sin sumar dos veces NC/NC RECON ni incluir devoluciones o documentos cancelados en el importe de NC. Una NC relacionada con dos facturas se cuenta una vez en el indicador del borrador, conservando ambas relaciones para consultar su detalle.
- **La relación es NC/factura, no NC/borrador.** No se puede afirmar qué borrador originó una NC con los datos disponibles. Los avisos no modifican estados ni importes del borrador.
- En Seguimiento, Autorizaciones y Dashboard siempre aparece un indicador: por consultar, con NC vigente, sin NC vigente, canceladas o consulta no disponible. Incluye fecha/hora de la última consulta exitosa. Al pulsar el indicador se actualiza esa consulta; en el detalle también se accede a las NC en pestaña nueva.
- Actualización automática bajo demanda mientras la pantalla está visible, cada 60 segundos por defecto. No es un servicio de sincronización ni una escritura en SAP. Pestañas ocultas no inician consultas nuevas. Los lotes se serializan y se limita cada petición a 100 borradores; no se descargan adjuntos para consultar indicadores.
- Ante un fallo se conservan los últimos datos y se indica que no están actualizados. Nunca se interpreta una consulta fallida como ausencia de NC.
- `NC_PREVIA_SAP`, el filtro de Dashboard y la columna Excel **NC al crear (histórico)** mantienen el snapshot guardado al crear; no reflejan la consulta actual. No se reescriben borradores existentes.

## Despliegue

No requiere una migración SQL nueva. Se utilizan las tablas existentes y las vistas/tablas SAP ya integradas. Desplegar juntas las DLL de Entidades, DAL, BLL y UI, las vistas, los dos componentes JS y la nueva hoja de estilos. El proyecto incluye los recursos nuevos como Content.

En el Web.config de destino:

```xml
<add key="BorradorNC.IntervaloConsultaSapSegundos" value="60" />
```

Si falta esta clave, se usan 60 segundos; el rango admitido es 30–600 segundos. La antigua clave `BorradorNC.BloquearPorNcPrevia` deja de utilizarse: las NC son antecedentes informativos por el requisito aprobado. No alterar las conexiones ni las credenciales del entorno.

SAP debe permitir al usuario de consulta leer `INF_VRC_FACRNC`, ORIN y ORDN en los esquemas de las empresas configuradas. No ejecutar scripts de escritura en SAP para esta funcionalidad. El criterio de cancelación N/Y/C está documentado en [la referencia oficial de ORIN](https://help.sap.com/doc/089315d8d0f8475a9fc84fb919b501a3/10.0/en-US/SDKHelp/orin.html).

## Verificación local sin bases de datos

```powershell
Get-ChildItem Tests/BorradoresNC/*Tests.js | ForEach-Object { node $_.FullName }
./Tests/BorradoresNC/compilar-bnc.ps1 -Compiler 'ruta/al/csc.exe'
# Con Playwright instalado y Chrome disponible:
node Tests/BorradoresNC/sap-indicadores.browser.js
```

El compilador debe ser Roslyn C# 6 o superior. Las dependencias NuGet deben estar restauradas. El verificador compila las tres capas completas, el controlador/modelos/filtros reales de BNC y las cinco vistas Razor modificadas; ejecuta relaciones SAP con fixtures sin conexiones. La prueba de navegador carga los componentes reales y jQuery, con respuestas simuladas. Si Playwright está en otra carpeta, indicar su módulo con la variable `BNC_PLAYWRIGHT_PATH`.

## Prueba de aceptación con el entorno de pruebas

1. Seleccionar una factura abierta incluida en un borrador vigente; comprobar la advertencia y crear otro borrador de prueba con un importe válido. Probar también una factura cuyo acumulado de otros borradores ya iguale/supere su total. Se debe permitir guardar.
2. Intentar repetir la factura en el mismo borrador o superar su total individual: debe rechazarse. Comprobar que siguen ocultas las pagadas exactamente y aparecen las que tienen cualquier diferencia pendiente.
3. Comparar los indicadores de las tres pantallas con NC conocidas en SAP. Comprobar NC/NC RECON duplicadas, NC de dos facturas y NC canceladas. No sumar devoluciones como NC.
4. Crear/cancelar una NC por el flujo autorizado normal de SAP en **pruebas**; actualizar el indicador o esperar el intervalo con la pantalla visible. El dato histórico no debe cambiar.
5. Simular un fallo de SAP en pruebas: debe conservarse el último resultado con advertencia. Un primer fallo debe mostrar consulta no disponible, nunca “sin NC”.
6. Verificar usuarios sin permiso, con agentes asignados y con Dashboard global. Seleccionar una NC y comprobar detalle en otra pestaña, retorno a la pantalla correcta y rechazo de claves/documentos fuera de alcance.
7. Cambiar filtros, selección y pestaña durante consultas; no debe aparecer información de otra selección ni iniciarse sondeo en pestañas ocultas.

La compilación y los fixtures locales no sustituyen esta aceptación contra SQL/HANA. No se probaron escrituras reales de borradores ni NC en producción.
