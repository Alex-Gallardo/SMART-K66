# Despliegue de Borradores NC en producción

## Alcance

Este paquete apunta exclusivamente a `POS-SmartK66_DEV`, identificada por la
configuración versionada como la base de producción. Las guardas `USE` y
`DB_NAME()` detienen la ejecución si se intenta usar otra base.

Se despliegan:

- `BORR_NC_SERIES`, `BORR_NC_ENC`, `BORR_NC_DET` y
  `VW_BORR_NC_ACUMULADO`.
- Series `BWB-`, `BWF-` y `BWG-` para BOLIK, FAES y GRACO.
- Cinco permisos, dos entradas de menú y catorce asignaciones por rol.
- `BORR_NC_ADJUNTO` para documentación opcional.

No se copian borradores, facturas ni adjuntos generados en pruebas. Tampoco se
modifica SAP HANA: facturas, PDF, productos, notas de crédito y devoluciones se
consultan en tiempo real desde las tablas/vistas SAP existentes.

## Antes de ejecutar

1. Confirmar con Infraestructura que la base productiva es exactamente
   `POS-SmartK66_DEV` y que el servidor es el esperado.
2. Generar y verificar un respaldo completo de la base.
3. Programar una ventana sin capturas simultáneas del módulo.
4. Usar una ventana nueva de SSMS y ejecutar cada archivo completo, nunca una
   selección parcial.
5. Conservar todas las pestañas de resultados y `Messages`.

## Orden obligatorio

1. `00_preflight_y_validacion_final_solo_lectura.sql`
   - Es solo lectura.
   - En una instalación nueva es normal que el resumen muestre cero objetos,
     permisos y menús BorradorNC; se usa para confirmar prerrequisitos.
   - Detener el despliegue si faltan las tablas base `Empresa`, `Permiso`,
     `Menu`, `Rol`, `Rol_Permiso`, `Usuario`, `Usuario_Rol` o
     `Usuario_Empresa`, si las empresas no son BOLIK/FAES/GRACO, o si el menú
     de `ReciboCaja` no tiene un único padre con id `543`.
2. `01_crear_estructura_segura.sql`
   - Transaccional e idempotente.
   - Debe terminar con las salidas `01A` a `01E` y el mensaje `OK`.
3. `02_configurar_permisos_menu_roles.sql`
   - Transaccional e idempotente.
   - Requiere exactamente un rol con cada nombre: `Administrador`,
     `AdministradorK66`, `Telemarketing` y `VendedorK66`.
   - Debe informar 5 permisos, 2 menús y 14 asignaciones.
4. `03_crear_adjuntos_seguro.sql`
   - Transaccional e idempotente.
   - Debe devolver `RESULTADO = OK`.
5. `04_validar_adjuntos_solo_lectura.sql`
   - Es solo lectura. Cero registros es válido en una instalación nueva.
   - `Adjuntos huerfanos` debe ser cero.
6. Ejecutar nuevamente `00_preflight_y_validacion_final_solo_lectura.sql`.
   - El resumen final debe mostrar 4 objetos base, 5 permisos, 2 menús y 3
     empresas, todos con `RESULTADO = OK`.
7. `05_validacion_final_estricta_solo_lectura.sql`
   - Es solo lectura y detiene el lote si cualquier contrato final falla.
   - Todos los renglones deben indicar `RESULTADO = OK`.

Ante cualquier error, no continuar con el siguiente archivo. Los scripts de
escritura revierten su propia transacción, pero se debe guardar el error y todas
las salidas para diagnosticar antes de reintentar.

## Configuración de la aplicación

Antes de publicar, la configuración efectiva del sitio debe contener:

```xml
<add key="BorradorNC.BloquearPorNcPrevia" value="false" />
<add key="BorradorNC.MostrarFacturasPagadas" value="false" />
<add key="BorradorNC.HabilitarEnlaces" value="false" />
<add key="BorradorNC.OmitirPermisos" value="false" />

<add name="BorradorNcContext"
     connectionString="Alias=RecibosContext;Initial Catalog=POS-SmartK66_DEV"
     providerName="System.Data.SqlClient" />
```

El alias reutiliza servidor y credenciales de `RecibosContext`, pero mantiene
aislada la base seleccionada para Borradores NC. No publicar secretos en el
repositorio ni reemplazar otras cadenas de conexión.

## Publicación y prueba de humo

Publicar la aplicación únicamente después de que el paso 7 termine en `OK`.
Validar con usuarios de prueba controlados:

1. Un vendedor puede abrir Borradores NC, crear un borrador sin adjuntos y otro
   con un archivo.
2. El selector muestra únicamente facturas con saldo pendiente real.
3. Seguimiento respeta los agentes vinculados mediante `Usuario_Empresa`.
4. Un autorizador ve la bandeja, factura y productos, documentación y
   antecedentes SAP; puede autorizar o rechazar.
5. Un usuario sin `Control.BorradorNC.Autorizar` no ve ni accede a la bandeja.
6. Imprimir factura abre el URL almacenado en SAP cuando está disponible.

## Recuperación

No se incluye un script genérico de `DROP`: después de habilitar el módulo puede
eliminar datos válidos. Si el despliegue debe revertirse, retirar primero la
publicación y recuperar la base desde el respaldo verificado o preparar una
reversión específica según las filas ya creadas.
