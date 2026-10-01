# Clientes CRM · despliegue SQL

Los scripts están destinados a `POS-SmartK66_DEV` en `K66-APPS`, según el diagnóstico compartido el 1 de octubre de 2026. La aplicación solo guarda solicitudes y fichas en SQL Server; el código SAP se captura manualmente en la ficha por Créditos. No hay escritura a SAP.

1. Ejecutar `00_diagnostico_previo.sql` en la base de destino y revisar sus resultados.
2. Ejecutar `01_estructura_clientes.sql`.
3. Ejecutar `02_permisos_menu.sql`.
4. Ejecutar `03_verificacion.sql` y compartir sus cuatro tablas de resultados.
5. Ejecutar `04_validacion_contrato_solo_lectura.sql` para comprobar que las columnas e índices instalados coinciden con el código.

Los scripts `01` y `02` son transaccionales e idempotentes para una instalación inicial. No se deben ejecutar en otra base sin adaptar primero el nombre en los scripts y en `ClientesCrmContext`.

Permisos: `VendedorK66` recibe acceso a solicitudes propias y creación; `CREDITOS` y `CREDITOS GERENCIA` reciben dashboard y administración SQL. El controlador exige además pertenencia al rol respectivo. La desactivación de clientes es lógica para conservar la auditoría.

## Recorrido de aceptación al desplegar la rama

1. Con un usuario `VendedorK66` que tenga una empresa y agente asignados, abrir **Clientes → Solicitudes de clientes**. Crear un borrador, volver a editarlo y enviarlo con RTU y DPI. Verificar que el tema visual y los agentes cambian al seleccionar empresa, y que otro vendedor no puede abrir el borrador.
2. Con un usuario de `CREDITOS`, abrir el dashboard, filtrar la solicitud, rechazarla con motivo y comprobar que el vendedor ve el motivo. Corregir y reenviar; aprobarla desde Créditos.
3. Abrir la ficha creada, añadir manualmente el código SAP y guardar. Verificar que figura en cartera y que la aplicación no creó ni cambió ningún registro en SAP.
4. Comprobar contactos, direcciones, horarios, documentos, indicadores por empresa y moneda; seleccionar varias solicitudes y abrir el Excel exportado.
5. Revisar la actividad de la ficha y solicitud, desactivar y reactivar el cliente, y comprobar que un usuario sin permisos no accede al dashboard ni a los archivos.

La comprobación de interfaz y flujo completo requiere que esta rama esté desplegada. Los scripts `03` y `04` verifican la instalación SQL sin datos de prueba.
