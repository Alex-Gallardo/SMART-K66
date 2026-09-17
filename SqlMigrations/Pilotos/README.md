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
está inactivo. Los cambios de nombre deben reconciliarse en el origen. No se
inventa un vínculo por placa: se muestra el vehículo de cada ruta asignada al
empleado autorizado, sin mantener otra asignación en POS.

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
| Motivo | Obligatorio para no entregado/incidencia, máximo 150 caracteres. |
| Datos que no edita el piloto | Vehículo, asignación, cliente, dirección, horas y observaciones existentes. |

Una ruta cerrada puede conservar documentos fallidos o con incidencia. Completar
no marca todos los documentos como entregados, no anula la ruta y no liquida ni
cambia estados financieros de documentos fuente.

El servidor exige el conjunto exacto de documentos y una versión del detalle.
La revalidación y el guardado usan una sola conexión y transacción serializable
entre catálogos de la misma instancia. Se bloquean encabezado/detalle y se
rechaza una reasignación, cambios de resultados o un formulario obsoleto.
`portal_piloto_guardar_resultado` modifica solamente `MO_VISITO`, `MO_ENTREGA` y
`MO_MOTIVO`. El cierre invoca la rutina central `rutas_cerrar`; no se usa
`rutas_confirmar`, que corresponde a otra transición.

Se verifica el resultado antes de confirmar la transacción y se inserta auditoría
en POS en la misma transacción. Un reintento con el mismo usuario, identificador
y contenido no vuelve a modificar datos. Un identificador reutilizado con otro
contenido se rechaza. Ante timeout o respuesta perdida, recargar para comprobar
el estado: no interpretar un error HTTP como prueba de que no hubo commit.

La lista permite períodos de 31 días y páginas de 25 rutas. El detalle admite
hasta 200 documentos para mantener el formulario bajo el límite de claves de
ASP.NET. Rutas mayores se derivan a Distribución, sin guardar parcialmente.

## Instalación manual y habilitación

1. Revisar la configuración efectiva de `GiveContext` (usuarios/auditoría) y
   `APK66Context` (catálogo de rutas). Deben identificar catálogos diferentes de
   la **misma instancia**; el módulo conecta con las credenciales de GiveContext.
   La cuenta SQL debe tener acceso explícito a ambos y collations compatibles.
   No se usa la clase heredada `APK66Context`, cuyo constructor tiene otro destino.
2. Revisar `10_portal_pos.sql`: crea las tablas del portal, rol y dos permisos.
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
6. Solo en una copia aislada, instalar `13_resultado_documento.sql` y revisar el
   trigger existente de rutas con el responsable de la lógica central. Un cierre
   no debe modificar documentos de otras rutas ni reabrir documentos terminales.
   La corrección del trigger y su aprobación son requisitos de habilitación.
7. Ejecutar los casos integrados de la siguiente sección. Registrar la huella
   SHA-256 de la **definición revisada y probada** del trigger en
   `Pilotos.TriggerSha256`. No copiar la huella de una definición sin revisar para
   sortear la protección. El módulo rechaza triggers habilitados adicionales en
   encabezado/detalle y definiciones ausentes, deshabilitadas o distintas.
8. Habilitar cierre únicamente después de esa validación y un despliegue acordado.
   Mantenerlo apagado si no hay copia aislada. La base de usuarios de pruebas no
   aísla las rutas si APK66Context sigue apuntando al catálogo operativo.

Los scripts empiezan en vista previa y requieren completar parámetros y activar
explícitamente la aplicación del cambio. Detienen una instalación existente para
revisión. No incluyen credenciales, resultados de producción ni copias de módulos
recibidos para análisis. Los diagnósticos originales `00`/`01` son históricos;
la corrección de su LOCK_TIMEOUT se integró en el PR #54. No hace falta volver
a ejecutarlos para usar esta implementación.

Permisos SQL mínimos a evaluar para la cuenta de servicio: SELECT en las tablas
de autorización POS y las tres tablas RT consultadas; SELECT/INSERT en auditoría;
EXECUTE en las dos rutinas de resultado/cierre y visibilidad de definiciones del
trigger. La cuenta web no administra vínculos ni cambia permisos/triggers. No
habilitar TRUSTWORTHY, cross-db ownership chaining global ni permisos `sa` para
resolver el acceso; usar usuarios/permisos explícitos según la política local.

Para detener el módulo: ambos flags en false. Para detener solo escrituras:
PermitirCierre=false. Conservar tablas y auditoría; no revertir datos comerciales
mediante una migración inversa genérica.

## Validación

`Tests/Pilotos/run.ps1` compila el módulo aislado, ejecuta 41 comprobaciones de
reglas/formulario/desafío y ocho del modo temporal (incluido bloqueo del cierre
antes de conectar SQL), compila las cinco vistas Razor y analiza los cuatro
scripts nuevos con ScriptDom SQL150, sin conexiones SQL. Las rutas de DLL pueden
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
