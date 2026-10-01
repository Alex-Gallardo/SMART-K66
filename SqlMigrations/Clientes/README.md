# Clientes CRM · despliegue SQL

Los scripts están destinados a `POS-SmartK66_DEV` en `K66-APPS`, según el diagnóstico compartido el 1 de octubre de 2026. La aplicación solo guarda solicitudes y fichas en SQL Server; el código SAP se captura manualmente en la ficha por Créditos. No hay escritura a SAP.

1. Ejecutar `00_diagnostico_previo.sql` en la base de destino y revisar sus resultados.
2. Ejecutar `01_estructura_clientes.sql`.
3. Ejecutar `02_permisos_menu.sql`.
4. Ejecutar `03_verificacion.sql` y compartir sus cuatro tablas de resultados.

Los scripts `01` y `02` son transaccionales e idempotentes para una instalación inicial. No se deben ejecutar en otra base sin adaptar primero el nombre en los scripts y en `ClientesCrmContext`.

Permisos: `VendedorK66` recibe acceso a solicitudes propias y creación; `CREDITOS` y `CREDITOS GERENCIA` reciben dashboard y administración SQL. El controlador exige además pertenencia al rol respectivo. La desactivación de clientes es lógica para conservar la auditoría.
