# Portal de pilotos

Vista MVC `/Piloto` para consultar las rutas propias y registrar el resultado de
cada documento al completar una ruta. Solo se admite **E → C**. La anulación
**X pertenece a Distribución**; el módulo no incluye acción ni permiso de anular.

La implementación está desactivada por defecto. El cierre requiere validación
integrada en una copia aislada de ambas bases y revisión de la lógica SQL central.
Los archivos SQL de esta carpeta **no se ejecutan al iniciar la aplicación**.
Las claves nuevas están en `appSettings.example.config`: incorporarlas
localmente sin reemplazar el resto de appSettings. Su ausencia también desactiva
el módulo. Este cambio no modifica ni publica archivos de conexión del despliegue.

## Consulta temporal sin permisos PILOTO

Para probar las pantallas con rutas reales usando una cuenta POS existente,
agregar estas claves al `appSettings` local del sitio (sin duplicar claves):

```xml
<add key="Pilotos.PruebasSoloLectura" value="true" />
<add key="Pilotos.UsuarioPrueba" value="TU_LOGIN_POS" />
<add key="Pilotos.PlacaPrueba" value="PLACA_REAL" />
<add key="Pilotos.PermitirCierre" value="false" />
```

Iniciar sesión normalmente y abrir `/Piloto/Index`. El login no necesita tener
una placa vinculada: `PlacaPrueba` define por separado el vehículo consultable.
Este modo omite el rol, los permisos, el vínculo de empleado y la sesión especial
PILOTO. No requiere instalar las tablas del portal ni habilitar el módulo normal.
Conserva autenticación POS, usuario activo/habilitado para web y la lista limitada
al login configurado. Listado y detalle solo muestran rutas de la placa indicada.
Las conexiones y permisos SQL de lectura siguen siendo necesarios.

Si se conoce una ruta pero no su placa, dejar `Pilotos.PlacaPrueba` vacío y usar
`Pilotos.RutaPrueba` con ese ID. Se debe configurar solo una de las dos opciones.
La lista muestra únicamente esa ruta, sin filtro por fecha, y el detalle rechaza
cualquier otro ID. Así no hace falta asignarle un vehículo al usuario de prueba.

Estas claves deben estar en el **Web.config principal del sitio que se ejecuta**;
el archivo de ejemplo no se carga automáticamente. Sin ellas, una sesión POS
normal no es una sesión PILOTO. El portal ahora devuelve una explicación 403 con
los pasos de configuración en vez de reenviar al login a un usuario autenticado.

Si las conexiones identifican el servidor con nombres diferentes, se puede
declarar `Pilotos.CatalogoRutas` explícitamente. Este catálogo se consulta siempre
en la instancia de `GiveContext`, usando la misma conexión POS; no es un enlace
a otro servidor ni requiere modificar las conexiones existentes. Verificar que
el catálogo de rutas exista en esa instancia. Si la clave está vacía, se conserva
la validación de servidor idéntico con `APK66Context`.

Durante este modo **el cierre se rechaza en el servidor**, incluso si otra clave
configura `PermitirCierre=true`. La vista indica que son pruebas de consulta.
Al terminar, cambiar `Pilotos.PruebasSoloLectura=false` y vaciar usuario/placa;
entonces vuelven los controles normales de piloto. No publicar identificadores
reales ni conexiones al completar el ejemplo local.

## Identidad y alcance

Las cuentas y contraseñas siguen en el sistema de usuarios existente. Se crean
con rol `PILOTO`, agencia y empresa operativa reales, usando la administración de
usuarios. El login dirige ese rol al portal y no exige el menú ni la selección
de agencia de otras pantallas.

`PilotoVinculo` relaciona `Usuario.Usuario_Id` con `RT_EMPLEADOS.ROWID`, no con un
nombre escrito por el navegador. `PilotoCentro` limita los centros consultables.
El vínculo tiene activación independiente y un código de auditoría de hasta 15
caracteres, acordado con el responsable de rutas: nunca truncar un login.

El origen conserva una relación heredada por nombre (`RT_RUTAS.PILOTO` con
`RT_EMPLEADOS.NOMBRE`). Se rechazan nombres ambiguos, incluso si el otro empleado
está inactivo. Los cambios de nombre deben reconciliarse en el origen. La placa
no sustituye al vínculo de empleado: PilotoVehiculo agrega una restricción de
acceso por vehículo, sin reasignar rutas en APK66.

Cada operación vuelve a comprobar usuario activo/habilitado para web, rol,
permiso, vínculo activo, empleado activo y centros. La identidad procede de la
sesión autenticada. Cambiar un ID en la URL o el formulario no amplía el alcance.

Si la cuenta requiere Token, el piloto usa un desafío de seis dígitos, con cinco
intentos y cinco minutos de vigencia. El código esperado vive en la sesión del
servidor. La cookie y el acceso al portal se emiten después de validarlo; no se
acepta el flujo Token heredado como autenticación del piloto. La rama de login
de otros roles se conserva. El envío usa el proveedor ya configurado, mediante
[POST con autenticación en cabecera](https://sms.to/gateway/), y comprueba la
[aceptación del envío](https://support.sms.to/support/solutions/articles/43000695113-bulk-sms-and-webhooks).
No se enviaron SMS durante estas pruebas.

## Resultados y cierre

| Dato | Comportamiento |
| --- | --- |
| Estado de ruta | A abierta, E en ruta, C cerrada, X anulada. Solo E puede completarse. |
| Resultado de documento | `ENTREGADO`, `NO ENTREGADO` o `INCIDENCIA`; se conserva el resultado existente al mostrar el formulario. |
| Visita | Selección explícita; entregado requiere visita afirmativa. |
| Motivo | Obligatorio para no entregado/incidencia y limitado al catálogo observado en APK66. |
| Observación del piloto | Obligatoria para no entregado/incidencia, máximo 500 caracteres; se agrega sin borrar la observación existente. |
| Datos que no edita el piloto | Vehículo, asignación, cliente, dirección y horas. |

El catálogo inicial usa los motivos recurrentes encontrados en una muestra de
producción: `TIEMPO CLIENTE`, `TIEMPO RUTA`, `CLIENTE CERRADO`, `FALTA ESPACIO`,
`PEDIDO INCORRECTO`, `CLIENTE RECHAZO PEDIDO`, `PRODUCTO NO SOLICITADO` y `OTRO`.
El servidor valida los mismos valores que presenta la interfaz. La pantalla usa
opciones táctiles grandes y una confirmación final que resume entregas, fallos e
incidencias; el POST solo se habilita después de marcar que se revisó el resumen.

Una ruta cerrada puede conservar documentos fallidos o con incidencia. Completar
no marca todos los documentos como entregados, no anula la ruta y no liquida ni
cambia estados financieros de documentos fuente.

El servidor exige el conjunto exacto de documentos y una versión del detalle.
La revalidación y el guardado usan una sola conexión y transacción serializable
entre catálogos de la misma instancia. Se bloquean encabezado/detalle y se
rechaza una reasignación, cambios de resultados o un formulario obsoleto.
`portal_piloto_guardar_resultado` modifica `MO_VISITO`, `MO_ENTREGA`, `MO_MOTIVO`
y agrega la nueva observación a `MO_OBSER`, conservando texto previo. El cierre
invoca la rutina central `rutas_cerrar`; no se usa
`rutas_confirmar`, que corresponde a otra transición.

Se verifica el resultado antes de confirmar la transacción y se inserta auditoría
en POS en la misma transacción. Un reintento con el mismo usuario, identificador
y contenido no vuelve a modificar datos. Un identificador reutilizado con otro
contenido se rechaza. Ante timeout o respuesta perdida, recargar para comprobar
el estado: no interpretar un error HTTP como prueba de que no hubo commit.

La lista permite períodos de 31 días y páginas de 25 rutas. El detalle admite
25 documentos por página de consulta. El formulario de cierre admite hasta 200
documentos completos; rutas mayores se consultan por páginas y su cierre se
deriva a Distribución, sin guardar parcialmente.

## Instalación manual y habilitación

1. Revisar la configuración efectiva de `GiveContext` (usuarios/auditoría) y
   `Pilotos.CatalogoRutas` (o `APK66Context` si la clave está vacía). Deben
   identificar catálogos diferentes de la **misma instancia**; el módulo conecta
   con las credenciales de GiveContext.
   La cuenta SQL debe tener acceso explícito a ambos y collations compatibles.
   No se usa la clase heredada `APK66Context`, cuyo constructor tiene otro destino.
2. Ejecutar primero `07_pos_instalacion_solo_lectura.sql` para comprobar si la
   instalación está ausente, completa o parcial. Revisar después `10_portal_pos.sql`:
   crea las tablas del portal, rol y dos permisos.
   No crea cuentas, no vincula personas y no otorga permisos a usuarios existentes.
   Completar el catálogo destino localmente; la aplicación está desactivada.
3. Crear la cuenta con rol PILOTO y usar `11_vincular_piloto.sql` para revisar y
   registrar su vínculo y centro. Un vínculo existente nunca se sobrescribe.
   Se requiere validar el centro con Distribución. Para revocar acceso, desactivar
   el vínculo; no borrar su auditoría. Revisar roles adicionales de las cuentas.
4. Revisar índices y planes con `12_indices_rutas.sql` antes de habilitar consultas.
   Su creación necesita una ventana acordada por posibles bloqueos y uso del log.
   No ejecutar índices en producción como si fueran diagnósticos de solo lectura.
5. Para consulta, configurar `Pilotos.Habilitado=true` y mantener
   **`Pilotos.PermitirCierre=false`**. La tabla de vínculo vacía no da acceso a nadie.
6. Solo en una copia aislada, instalar `13_resultado_documento.sql`. Revisar
   `16_trigger_rutas_seguro.sql` con el responsable de la lógica central y
   ejecutarlo primero en vista previa. La migración exige la huella original,
   respalda la definición y reemplaza la selección global por procesamiento
   set-based limitado a `inserted`. Rechaza documentos fuente ambiguos.
7. En esa copia ejecutar `17_trigger_rutas_prueba_rollback.sql`. Requiere una
   confirmación explícita de copia aislada, prueba un `CAMBIO` o `ENVIO` real y
   revierte tanto la cabecera como el documento fuente. Un cierre no debe
   modificar documentos de otras rutas ni reabrir documentos terminales.
8. Ejecutar los casos integrados de la siguiente sección. Registrar la huella
   SHA-256 de la **definición revisada y probada** del trigger en
   `Pilotos.TriggerSha256`. No copiar la huella de una definición sin revisar para
   sortear la protección. El módulo rechaza triggers habilitados adicionales en
   encabezado/detalle y definiciones ausentes, deshabilitadas o distintas.
9. Habilitar cierre únicamente después de esa validación y un despliegue acordado.
   Mantenerlo apagado si no hay copia aislada. La base de usuarios de pruebas no
   aísla las rutas si APK66Context sigue apuntando al catálogo operativo.

Los scripts empiezan en vista previa y requieren completar parámetros y activar
explícitamente la aplicación del cambio. Detienen una instalación existente para
revisión. No incluyen credenciales, resultados de producción ni copias de módulos
recibidos para análisis. Los diagnósticos originales `00`/`01` son históricos;
la corrección de su LOCK_TIMEOUT se integró en el PR #54. No hace falta volver
a ejecutarlos para usar esta implementación.

Permisos SQL mínimos a evaluar para la cuenta de servicio: SELECT en las tablas
de autorización POS y las cuatro tablas RT consultadas; SELECT/INSERT en auditoría;
EXECUTE en las dos rutinas de resultado/cierre y visibilidad de definiciones del
trigger. La cuenta web no administra vínculos ni cambia permisos/triggers. No
habilitar TRUSTWORTHY, cross-db ownership chaining global ni permisos `sa` para
resolver el acceso; usar usuarios/permisos explícitos según la política local.

Para detener el módulo: ambos flags en false. Para detener solo escrituras:
PermitirCierre=false. Conservar tablas y auditoría; no revertir datos comerciales
mediante una migración inversa genérica.

## Validación

`Tests/Pilotos/run.ps1` compila el módulo aislado, ejecuta 71 comprobaciones
centrales más los escenarios de configuración temporal, sesión y acceso a la
administración (incluido bloqueo del cierre antes de conectar SQL), compila las
siete vistas Razor y analiza los once scripts del módulo con ScriptDom SQL150,
sin conexiones SQL. Las rutas de DLL pueden
pasarse por parámetros; usa paquetes locales/restaurados, sin descargarlos.

Estas comprobaciones **no sustituyen un build completo ni pruebas SQL/IIS**.
Pendientes en copia aislada con dos pilotos y datos ficticios:

- Listado/detalle solo propios; ID ajeno devuelve 404; vínculos, empleados,
  centros o permisos revocados bloquean también una sesión ya iniciada.
- Nombres homónimos y login duplicado rechazan acceso; un piloto no puede anular
  ni cerrar A/C/X o una ruta liquidada.
- Reasignación y edición simultánea en origen invalidan el formulario abierto.
- Cierre mixto entregado/no entregado/incidencia guarda todos los resultados,
  mantiene horas/observaciones y deja la cabecera C con actor/fecha centrales.
- Fallo a mitad del guardado revierte detalle, cabecera y auditoría. Dos cierres
  simultáneos o reenvío tras pérdida de respuesta no duplican la operación.
- Comparar documentos fuente antes/después: cierre no debe alterar ninguno.
  Probar también operaciones de Distribución afectadas por el trigger revisado.
- Login con/sin Token, código incorrecto/expirado/reutilizado, sesión perdida,
  POST sin antiforgery, navegación móvil y mensajes de error sin datos internos.

La compilación completa requiere el toolchain web de Visual Studio, targeting
pack .NET Framework y dependencias de la solución (incluido Crystal Reports).
La aplicación no debe desplegarse basándose únicamente en el compilador aislado.

## Actualizacion: usuario, transporte y placas

No se crea otra tabla de credenciales ni se modifica `dbo.Usuario`. La cuenta POS
sigue siendo la identidad autenticada y el login habitual sigue llevando al
Dashboard; el usuario entra al portal desde el menu existente.

El modelo de acceso normal queda asi:

- `Usuario.Usuario_Id` → `PilotoVinculo` → `RT_EMPLEADOS.ROWID`: identidad del piloto.
- `Usuario.Usuario_Id` → `PilotoCentro`: centros autorizados.
- `Usuario.Usuario_Id` → `PilotoVehiculo.Placa`: una o varias placas autorizadas.
- `RT_VEHICULOS.PLACA`: marca, linea, tipo y empresa de transporte del catalogo de rutas.

Las cuatro condiciones son acumulativas: piloto de la ruta, centro permitido,
placa vinculada y vehiculo activo en APK66. Compartir una placa no da acceso a
rutas asignadas a otra persona. El piloto no elige una placa libremente desde
la URL. Distribucion conserva las asignaciones reales en APK66; la tabla POS
limita el acceso web y NO replica ni cambia la asignacion de las rutas.
La consulta temporal conserva su alcance limitado por login y ruta/placa, sin
requerir estas tablas; todos los cierres siguen bloqueados en ese modo.

### Instalacion o actualizacion manual

1. Confirmar el catalogo POS de destino con el responsable del sitio. No inferir
   si es pruebas o produccion por el sufijo del nombre.
2. Si no existe el esquema anterior, revisar e instalar `10_portal_pos.sql` y
   vincular el usuario con `11_vincular_piloto.sql`. Una instalacion anterior NO
   debe volver a ejecutar el script 10: continuar con el paso 3.
3. Revisar `14_vehiculos_pos.sql`. Crea `PilotoVehiculo` y
   `PilotoVehiculoHistorial`, inicialmente sin asignaciones. Requiere el esquema
   previo y se detiene si ya existen objetos, sin sobrescribirlos.
4. Revisar `15_vincular_vehiculo.sql`. Completar usuario por ID, placa, catalogos,
   motivo y `@Activar`. Revisar primero con `@Aplicar=0`; solo despues aplicar
   explicitamente. `@Activar=0` revoca el vinculo y `@Activar=1` lo activa.
   Guarda antes/despues y actor SQL en historial dentro de la misma transaccion.
   Repetir el mismo estado no duplica el historial. No borrar asignaciones con
   historial.
5. Revisar `18_administracion_pilotos_pos.sql`. Al aplicar crea los historiales
   de identidad y centros. No crea permisos ni cambia roles. Es idempotente para
   una instalacion completa y se detiene ante tablas incompatibles.
6. Otorgar a la cuenta web los permisos SQL minimos para esta interfaz: lectura
   en seguridad POS y catalogos RT; SELECT/INSERT/UPDATE en `PilotoVinculo` y
   `PilotoVehiculo`; SELECT/INSERT/DELETE en `PilotoCentro`; e INSERT/SELECT en
   los tres historiales. Usar el usuario de base configurado en `GiveContext` y
   la politica local; el script no infiere ni concede permisos a principals SQL.
7. Volver a ejecutar `07_pos_instalacion_solo_lectura.sql`. Debe mostrar los
   siete objetos y los indices de auditoria. Validar despues
   `/Piloto/Administracion` con una sesion POS normal.
8. Validar alta, cambio de vehiculo y revocacion con datos de prueba. Confirmar
   que la vista previa muestra solo las rutas A/E coincidentes y mantener
   `Pilotos.PermitirCierre=false` hasta completar las pruebas de cierre.

No hay FK entre bases: el script comprueba la placa en APK66 al activarla y la
web vuelve a comprobar que el vehiculo este activo en cada consulta. La empresa
se muestra desde `RT_VEHICULOS.EMPRESA`; no se crea un catalogo paralelo de
transportistas ni se concede acceso a todos los vehiculos de una empresa.

### Interfaz de administracion

`/Piloto/Administracion` reemplaza la repeticion de los scripts 11 y 15 para la
operacion diaria. Cualquier usuario POS autenticado, activo y habilitado para el
sitio puede abrirla y guardar cambios; no requiere rol ni permiso funcional
adicional. El usuario que se vincula sí debe tener rol PILOTO para activar su
acceso a rutas. En una transaccion serializable se valida nuevamente la cuenta
del actor, la cuenta objetivo, el empleado unico/activo, centros y vehiculos de
APK66; despues se actualiza el vinculo POS y se registra actor, motivo y estado
anterior/nuevo. Desactivar conserva la cuenta y el historial, y revoca todas las
placas activas.

La pantalla no crea usuarios, roles, empleados, vehiculos ni rutas. Tampoco
escribe en APK66. La asignacion operativa de piloto/vehiculo/ruta permanece en
Distribucion; la vista previa solo explica que rutas A/E resultan visibles con
el alcance guardado. Los scripts 11 y 15 se conservan para instalacion inicial,
recuperacion controlada y diagnostico fuera del sitio.

### Diagnostico local de configuracion

Ejecutar en PowerShell, con la ruta del Web.config PRINCIPAL del sitio que
realmente ejecuta Visual Studio/IIS (el archivo no se envia a ningun servicio):

```powershell
.\SqlMigrations\Pilotos\Diagnosticar-Configuracion.ps1 `
  -WebConfig 'C:\ruta-del-sitio\Web.config' -Usuario 'LOGIN_DE_PRUEBA'
```

El resultado muestra la ruta revisada y verificaciones booleanas. No imprime
conexiones, passwords, login configurado ni identificadores de rutas/placas.
Detecta claves duplicadas y deriva archivos externos a revision manual.
Este diagnostico NO prueba que IIS use ese archivo ni realiza conexiones SQL.
Tras modificar configuracion, reiniciar la sesion y probar desde el menu.
Los detalles tecnicos de instalacion ya no aparecen en las pantallas del piloto.

### Consulta y experiencia de uso

Navbar sticky con Dashboard, Mis rutas, usuario conectado y salida. Los enlaces
del detalle conservan fechas y pagina del listado. La consulta muestra hasta
25 documentos por pagina, con total, numeracion continua y horas existentes.
Las rutas mayores a 200 documentos son consultables; solo su cierre se deriva a
Distribucion. El formulario editable conserva el conjunto completo de documentos,
por lo que nunca guarda una pagina parcial. Ante un error de validacion, recupera
los campos solo si puede volver a autorizar la ruta y su version sigue vigente.

Los errores distinguen sesion de piloto no disponible, configuracion incompleta,
instalacion pendiente, permisos/conexion y espera/bloqueo. Los errores tecnicos
incluyen una referencia correlacionada con Trace, sin registrar SQL, parametros,
contraseñas ni datos comerciales. La aplicacion fija LOCK_TIMEOUT=3000 en su
conexion; conserva lecturas confirmadas y transacciones de autorizacion. Revisar
planes e indices antes de cargas concurrentes; no se crean indices automaticamente.

### Evidencia y pendientes de esta revision

`Tests/Pilotos/run.ps1`: 107 comprobaciones (62 generales, 24 de consulta temporal,
12 de configuración inválida, 5 de ruta fija y 4 de sesión normal), cinco vistas
Razor y diez scripts SQL.
También analiza los cuerpos SQL dinámicos de los scripts
de vinculacion usando un catalogo ficticio. No conecta a SQL, envia SMS ni lee
credenciales productivas para las pruebas.

Se reviso una vista previa visual con datos ficticios, estilos del modulo y
ancho movil: sin desbordamiento horizontal y navbar fijo al desplazarse. Esa
vista previa no ejecuta MVC ni sustituye la verificacion del sitio real.

La compilacion completa no fue posible en el equipo de revision: no hay SDK .NET
ni herramientas web de Visual Studio; MSBuild Framework antiguo rechaza sintaxis
preexistente de otros modulos. Usar Visual Studio con el toolchain del proyecto,
paquetes, targeting pack y Crystal Reports para completar esa verificacion.

Pendientes de integracion, en entorno preparado:

- Login normal → Dashboard → menu Pilotos; salida, sesion vencida y usuario sin acceso.
- Dos usuarios/pilotos, distintas placas y centros: listado y detalle no cruzan datos.
- Revocar placa o inactivar vehiculo bloquea nuevas lecturas de una sesion iniciada.
- Activar/desactivar un vinculo registra historial; repetirlo no duplica registros.
- Rutas con 0, 25, 26, 200 y 201 documentos: todas consultables sin omisiones.
- Reasignacion concurrente, permisos SQL insuficientes, timeouts y collations distintas.
- En una copia aislada se validaron cierres entregados y mixtos, auditoria,
  rollback del cierre y la asignacion de un `ENVIO` por el trigger corregido,
  tambien con rollback. La promocion del trigger y del cierre a produccion sigue
  requiriendo revision de Distribucion, respaldo y ventana de mantenimiento.

## Rutas activas y diseño móvil (sin instalar tablas para la prueba)

La consulta temporal funciona sin ejecutar los scripts de instalación 10–15.
Para cambiar la ruta fija cerrada por una prueba con rutas activas:

1. Abrir una ventana nueva de SSMS en la base de rutas y revisar
   `06_rutas_activas_solo_lectura.sql`. Completar únicamente `@BaseEsperada` con
   su nombre exacto. El script devuelve hasta 20 cabeceras A/E de los últimos
   14 días. No modifica datos ni incluye documentos de clientes. El timeout de
   bloqueos no limita la duración total; detenerse ante un error o timeout.
2. Elegir una placa del resultado y, opcionalmente, el nombre exacto del piloto
   de esa ruta. No publicar esos resultados ni identificadores en el repositorio.
3. En el Web.config PRINCIPAL del sitio remoto, actualizar las claves existentes
   sin duplicarlas ni tocar las conexiones:

```xml
<add key="Pilotos.PruebasSoloLectura" value="true" />
<add key="Pilotos.UsuarioPrueba" value="LOGIN_POS" />
<add key="Pilotos.PlacaPrueba" value="PLACA_REAL" />
<add key="Pilotos.PilotoPrueba" value="" />
<add key="Pilotos.RutaPrueba" value="" />
<add key="Pilotos.PermitirCierre" value="false" />
```

Reemplazar los marcadores localmente. `PilotoPrueba` es opcional y solo se admite
junto con una placa: si se completa con el nombre exacto del resultado, listado
Y detalle exigen ambos. El nombre nunca se concatena en SQL; se parametriza.
Sin ese filtro se ven las rutas del vehículo autorizado, aunque cambie de piloto.
Esta excepción temporal no sustituye la vinculación formal usuario/empleado/placas.

Para ver solo una ruta A/E, usar su ID en `RutaPrueba` y dejar vacíos tanto
`PlacaPrueba` como `PilotoPrueba`. Una ruta fija se presenta como «Ruta de prueba»
y se muestra cualquiera que sea su estado/fecha; no se disfraza como lista de
rutas activas. Los cambios de configuración reciclan la aplicación: iniciar
sesión de nuevo y entrar desde el menú habitual. No hay cambios al login.

La vista por vehículo o del piloto normal comienza en **Activas** (A/E), con las
rutas E primero; **Historial** incluye C/X. Por defecto se consultan los últimos
14 días incluyendo hoy; se puede ajustar el período hasta 31 días. La fecha
siempre está visible. Si no aparecen rutas, comprobar período y asignación;
el portal no amplía automáticamente el alcance ni modifica estados.

El rediseño usa estilos mobile first, tipografía de sistema, iconos SVG locales,
botones principales de 56 px, encabezado sticky compacto y un solo aviso de
consulta. Los colores de estado llevan texto e icono. El menú Cuenta contiene
la salida POST con antiforgery. El detalle conserva filtros/página y el listado
restaura su desplazamiento usando solo una posición numérica en sessionStorage,
separada por fechas/vista/página. No almacena clientes ni documentos.

### Comprobación visual reproducible sin SQL

Después de `Tests/Pilotos/run.ps1`, ejecutar desde la raíz del repositorio:

```powershell
.\Tests\Pilotos\bin\RazorCompile.exe (Get-Location).Path --render
node .\Tests\Pilotos\preview.cjs
```

Abrir `http://127.0.0.1:8770`. También existen `/preview/fija`, `/preview/vacia`,
`/preview/error` y `/preview/cierre` para probar el formulario y su modal. La herramienta renderiza las vistas Razor reales usando
modelos ficticios, genera HTML solo en `Tests/Pilotos/bin` y sirve únicamente
archivos permitidos en loopback. No carga Web.config del sitio, no conecta SQL
ni ejecuta login/cierres; rechaza solicitudes distintas de GET. El formulario
de salida se puede inspeccionar pero no cerrar una sesión real desde esta vista.
El compilador también valida el layout original antes de renderizar la vista previa.

Se revisaron anchos de 320, 375, 425, 768, 1280 y 1600 px, sin desbordamiento
horizontal del listado. Se comprobó detalle móvil, menú Cuenta, salida POST,
fechas, navegación a Historial, regreso con filtros y desplazamiento. Contraste
medido en el navegador: botón principal 7.83:1; estados E/A 8.26:1 y 8.11:1;
aviso de consulta 9.22:1. Estas medidas no simulan condiciones físicas de luz solar.
El funcionamiento integrado contra el servidor remoto y SQL sigue pendiente de
la prueba del responsable del entorno; no se declaró validado mediante esta vista.

## Imágenes por documento (migración 19)

`19_documento_imagen_pos.sql` crea **solo en POS** una imagen vigente por
`ID_RUTA` + `RT_RUTAS_DET.ROWID` y un registro de altas/reemplazos con usuario,
fecha y huellas SHA-256. No modifica APK66 ni APP_TEST. El botón de carga y
reemplazo aparece únicamente en rutas **E** para pilotos autenticados con
vínculo vigente; `Pilotos.PruebasSoloLectura=true` impide escribir. Las imágenes
ya cargadas siguen consultables al cerrar la ruta. La selección masiva por cliente
solo prepara resultados del formulario: se guardan con la confirmación de cierre.

Para instalar, abrir una ventana nueva de SSMS y seleccionar la base POS exacta
(`POS-SmartK66_DEV` en el entorno descrito; confirmar antes de aplicar). Primero
ejecutar el script sin cambios: `@Aplicar=0` devuelve la vista previa. Revisar
el nombre de la base y que las tablas no existan. Después indicar el nombre
exacto en `@BaseEsperada`, cambiar `@Aplicar=1` y ejecutar en esa misma base.
El script requiere `dbo.Usuario` y `dbo.PilotoVinculo`, rechaza transacciones
abiertas y revierte la instalación si falla. No volver a ejecutarlo en otra base.
Tras instalarlo, consultar una ruta E con una cuenta PILOTO real; subir una
imagen JPG/PNG/WebP de hasta 10 MB, verla y reemplazarla. Confirmar que solo
queda una imagen vigente y que la tabla de eventos contiene `AGREGADA` y
`REEMPLAZADA`. Intentar cargar en una ruta C y en modo temporal debe ser
rechazado. La prueba local de sintaxis no ejecuta SQL ni sustituye esta
validación integrada.
