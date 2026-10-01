# Clientes CRM · despliegue SQL

Los scripts están destinados a `POS-SmartK66_DEV` en `K66-APPS`, según el diagnóstico compartido el 1 de octubre de 2026. La aplicación solo guarda solicitudes y fichas en SQL Server; el código SAP se captura manualmente en la ficha por Créditos. No hay escritura a SAP.

1. Ejecutar `00_diagnostico_previo.sql` en la base de destino y revisar sus resultados.
2. Ejecutar `01_estructura_clientes.sql`.
3. Ejecutar `02_permisos_menu.sql`.
4. Ejecutar `03_verificacion.sql` y compartir sus cuatro tablas de resultados.
5. Ejecutar `04_validacion_contrato_solo_lectura.sql` para comprobar que las columnas e índices instalados coinciden con el código.

Los scripts `01` y `02` son transaccionales e idempotentes para una instalación inicial. No se deben ejecutar en otra base sin adaptar primero el nombre en los scripts y en `ClientesCrmContext`.

Los scripts anteriores asignaron permisos a los roles originales. El controlador solo valida permisos, independientemente del rol que los conceda. La desactivación de clientes es lógica para conservar la auditoría.

## Actualización de cartera por usuario

Después de los scripts ya instalados, ejecutar solo `05_cartera_por_usuario.sql` y luego `07_verificar_cartera_por_usuario.sql`. El script `05` crea `CRM_CLIENTE_USUARIO`, recupera propietarios de solicitudes aprobadas y altas directas auditadas, crea `Control.Clientes.CarteraGlobal` y hace visible Cartera mediante el permiso de módulo. No asigna el permiso global a ningún rol. La columna `FICHAS_SIN_PROPIETARIO` del resumen debe revisarse si es mayor que cero.

Para conceder la vista global a un rol, editar `@RolNombre` en `06_ejemplo_asignar_cartera_global.sql` y ejecutarlo una vez por rol. También concede `Control.Clientes.Modulo` para que el menú sea visible. Un usuario sin el nuevo permiso solo ve fichas vinculadas a sus solicitudes aprobadas o creadas directamente por él; tener acceso al dashboard no amplía su Cartera. Un usuario con `Control.Clientes.Dashboard` ve indicadores y solicitudes enviadas de todos los usuarios; los borradores siguen siendo privados.

## Recorrido de aceptación al desplegar la rama

1. Con un usuario que tenga `Control.Clientes.Crear` y una empresa y agente asignados, abrir **Clientes → Solicitudes de clientes**. Crear un borrador, volver a editarlo y enviarlo con RTU y DPI. Verificar que el tema visual y los agentes cambian al seleccionar empresa, y que otro usuario no puede abrir el borrador aunque tenga el mismo permiso.
2. Con un usuario que tenga `Control.Clientes.Dashboard`, abrir el dashboard, filtrar la solicitud, rechazarla con motivo y comprobar que el creador ve el motivo. Corregir y reenviar; aprobarla desde el dashboard.
3. Abrir la ficha creada, añadir manualmente el código SAP y guardar. Verificar que figura en cartera y que la aplicación no creó ni cambió ningún registro en SAP.
4. Comprobar contactos, direcciones, horarios, documentos, indicadores por empresa y moneda; seleccionar varias solicitudes y abrir el Excel exportado.
5. Revisar la actividad de la ficha y solicitud, desactivar y reactivar el cliente, y comprobar que un usuario sin permisos no accede al dashboard ni a los archivos.
6. Con dos usuarios y fichas distintas, comprobar que ambos ven solo su propia Cartera. Asignar `Control.Clientes.CarteraGlobal` a uno y verificar que puede listar y abrir ambas fichas. Confirmar que un usuario autenticado sin permiso llega a **Sin acceso** y no al login.

La comprobación de interfaz y flujo completo requiere que esta rama esté desplegada. Los scripts `03` y `04` verifican la instalación SQL sin datos de prueba.
