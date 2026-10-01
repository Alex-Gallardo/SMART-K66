# Clientes CRM en POS-SmartK66 (pruebas)

Estos archivos apuntan exclusivamente a `POS-SmartK66`. Los scripts de la carpeta superior apuntan a `POS-SmartK66_DEV` (producción) y no deben usarse para esta instalación. Ningún archivo de esta carpeta crea usuarios, crea roles ni asigna permisos a roles. Tampoco conecta con SAP.

1. Ejecutar `00_estado_previo_solo_lectura.sql` y confirmar `BASE_ACTUAL = POS-SmartK66` y el servidor esperado.
2. Si el resumen indica que faltan tablas CRM, ejecutar `01_estructura_clientes.sql`. Si indica que faltan permisos o menú, ejecutar `02_permisos_menu.sql`. Para una instalación nueva, ejecutar ambos en ese orden. Son transaccionales e idempotentes; `02` solo crea los cinco permisos base y las entradas del menú.
3. Ejecutar `04_validacion_contrato_solo_lectura.sql` y comprobar `ERRORES = 0`. `03_verificacion.sql` permite revisar el detalle de objetos, permisos y menú sin consultar roles.
4. Ejecutar `05_cartera_por_usuario.sql`. Crea la tabla de propietarios de fichas y `Control.Clientes.CarteraGlobal`, sin conceder este permiso a ningún rol. En el resultado, `PERMISO_GLOBAL_CREADO` debe ser `1`.
5. Ejecutar `07_verificar_cartera_por_usuario.sql`. Debe indicar `OK` para tabla, permiso e índice; `PERMISO_MENU` debe ser `Control.Clientes.Modulo`. Si hay fichas existentes, revisar las filas `SIN_PROPIETARIO`.

Para probar la aplicación contra esta base, configurar **solo en el entorno local de pruebas** la conexión `ClientesCrmContext` con `Initial Catalog=POS-SmartK66`. El `Web.config` del repositorio apunta actualmente a `POS-SmartK66_DEV`.
