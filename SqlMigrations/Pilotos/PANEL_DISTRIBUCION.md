# Panel de distribución

Ruta: `/Piloto/Panel`. Consulta todos los pilotos y centros del catálogo configurado en `Pilotos.CatalogoRutas`; no suplanta la sesión del piloto.

## Instalación

1. Revisar y aplicar `24_permiso_monitorear_pos.sql` en **POS-SmartK66_DEV**. Primero ejecutar completo con `@Aplicar=0`; luego, en una ventana nueva, con `@Aplicar=1`. Solo crea `Pilotos.Monitorear`: no crea roles ni asigna permisos.
2. Revisar y aplicar de la misma forma `25_eventos_cliente_pos.sql` en **POS-SmartK66_DEV**. Conserva nuevos eventos de finalización de cliente después de cerrar la ruta, con usuario, actor, fecha UTC, versión y catálogo de origen. No modifica APK66 ni reconstruye registros antiguos.
3. Asignar `Pilotos.Monitorear` a los roles de consulta desde el panel POS existente. Mantener `Pilotos.Administrar` solo en los roles que administran vínculos; para ambas funciones asignar ambos permisos.
4. Compilar y publicar la aplicación. No requiere cambiar conexiones, activar el cierre ni ejecutar migraciones en APK66.
5. Iniciar sesión POS y abrir `/Piloto/Panel`, o usar el acceso nuevo en el dashboard de Inicio. No necesita rol PILOTO, vínculo operativo ni sesión especial del portal para monitorear.

Si se publica antes del script 25, el flujo operativo sigue funcionando, pero el panel advierte que falta la instalación y no atribuye borradores sin catálogo al avance de producción. Instalar el 25 antes de comenzar a validar nuevos guardados.

## Datos y filtros

- APK66 determina estado, liquidación y resultados registrados; POS aporta vínculos, fotografías, auditoría y clientes guardados.
- El período inicial es hoy, hora Guatemala. Admite hasta 31 días, con filtro por estado, liquidación, empleado, usuario POS, centro, placa, avance, resultado, búsqueda y tipo de evento.
- Las rutas liquidadas están ocultas inicialmente. Distribución puede consultarlas mediante el filtro; el portal operativo continúa ocultándolas. Las abiertas tienen detalle administrativo de consulta, pero el piloto conserva su interacción deshabilitada.
- Las tarjetas de rutas y avance se refieren a las rutas seleccionadas completas. Los resultados y el listado/exportación de documentos aplican además el filtro de documento/cliente y resultado. El filtro de tipo de evento afecta únicamente la actividad.
- Los usuarios/configuraciones se filtran por empleado, usuario, centro y placa; su cantidad de rutas corresponde a los filtros de rutas. Usuarios sin rutas siguen disponibles para detectar configuraciones incompletas.
- Los clientes se agrupan por nombre y dirección, igual que en el portal. Para contar un cliente guardado se exige la misma versión, documentos, usuario habilitado y evento del catálogo actual. Datos incompatibles o de origen no verificable se muestran como observaciones, no como avance vigente.
- No se atribuyen automáticamente a APK66 cierres POS históricos por coincidencia de ID. La auditoría anterior no tiene catálogo de origen. Las fotografías se identifican como archivos POS y se consultan en contexto, sin asumir un origen histórico no registrado.
- La actividad refleja datos persistidos; no indica GPS, usuario conectado ni respuestas que todavía no se hayan guardado. Los eventos nuevos de cliente sobreviven al borrado del borrador al cerrar; para eventos previos solo puede observarse el borrador mientras exista.
- Fechas POS UTC se convierten a Guatemala (UTC−6). `FECHA_MON` de APK66 se identifica como hora del servidor de rutas, sin convertirla arbitrariamente a UTC.

## Interfaz y límites

Resumen con indicadores y gráficos, listados de rutas/pilotos/documentos/actividad/observaciones, detalles por cliente y visor de fotografías. Los filtros se conservan al regresar; en móvil se pliegan inicialmente.

La actualización automática es opcional cada 60 segundos. Se pausa con pestaña oculta, filtros modificados, detalles abiertos o foco dentro del contenido. Ante error mantiene los datos anteriores y permite reintentar; no reemplaza la pantalla por una consulta incompleta.

Las consultas usan parámetros, lecturas confirmadas, espera por bloqueo de 3 segundos y timeout de 20 segundos. Los listados muestran 25 filas por página; los totales no se calculan solo sobre esa página. Se rechaza expresamente un período con más de 2.000 rutas, 100.000 documentos, 5.000 usuarios o 5.000 eventos para pedir filtros más específicos, sin presentar cifras parciales. La exportación CSV de documentos tiene un máximo de 10.000 filas y protege celdas que puedan interpretarse como fórmulas. No carga binarios de fotografías al construir listados.

## Validación en SQL/IIS

La compilación y pruebas locales no sustituyen esta comprobación integrada:

1. Usuario activo con solo `Pilotos.Monitorear`: consultar todos los pilotos, centros, detalle, fotografías y CSV; la configuración de pilotos debe rechazar el acceso directo sin `Pilotos.Administrar`.
2. Usuario con solo `Pilotos.Administrar`: administración existente disponible; `/Piloto/Panel` y sus endpoints deben responder sin conceder consulta global.
3. Usuario con ambos: acceso al panel y a configuración. Quitar cada permiso durante la misma sesión y verificar el rechazo en servidor en la próxima solicitud, incluyendo fotos/exportaciones.
4. Comparar rutas y resultados con consultas de solo lectura APK66 para el mismo período, estado, centro y liquidación. Probar `LIQUIDADO=1` en el filtro administrativo y confirmar que siga oculto al piloto.
5. En APP_TEST, guardar un cliente de una ruta E con el flujo normal. Verificar un evento POS `CatalogoRutas=APP_TEST`, progreso POS y resultado APK66 sin modificar hasta el cierre. Al cerrar, comprobar que el borrador se elimina y el evento permanece.
6. Comprobar origen: un evento APP_TEST de una ruta con ID coincidente no debe acreditar un borrador en APK66. No copiar ni modificar datos reales para preparar esta prueba; utilizar la copia aislada.
7. Probar fechas inválidas, búsqueda sin resultados, páginas posteriores a la última y exportación con filtros. Verificar que no se descarguen CSV parciales al exceder el límite.
8. Revisar móvil, teclado, movimiento reducido, visor de fotos, actualización manual/automática y error/reintento conservando la información previa.

## Verificación local

`Tests/Pilotos/run.ps1`: compila el módulo aislado, ejecuta reglas, permisos y filtros; compila once vistas Razor y valida sintaxis SQL, incluidas consultas reales del panel. No abre conexiones SQL.

`Tests/Pilotos/panel-browser-smoke.ps1`: Chrome aislado con datos ficticios para fechas, alertas propias, error/reintento, conservación de datos, separación de botones administrativos y visor/foco.

`node Tests/Pilotos/panel-responsive.js`: Chrome propio con viewport real de 390/1440 px, sin desbordamiento y con movimiento reducido. Genera capturas de prueba bajo `Tests/Pilotos/bin`, ignorado por Git; no utiliza el perfil del navegador del usuario.

`Tests/Pilotos/image-browser-smoke.ps1`: regresión del flujo operativo de fotos, clientes bloqueados y cierre/resumen.
