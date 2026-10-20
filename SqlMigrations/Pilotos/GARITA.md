# Control de salida de garita

La vista `/Garita` usa el login POS. El rol `GARITA` entra directamente en la búsqueda de rutas. La cuenta debe estar activa, habilitada para web y tener uno o más centros asignados en `/Garita/Administrar`. En cada consulta y envío el servidor vuelve a comprobar permisos y centro. El rol `GARITA_ADMIN` permite asignar centros y resolver diferencias; no concede por sí mismo el permiso operativo de confirmar salidas.

## Instalación

1. En una copia aislada de POS y del catálogo de rutas, revisar y ejecutar `27_garita_salida_pos.sql`. Por defecto muestra una vista previa y no hace cambios. Ejecutar `SELECT DB_NAME() AS BaseActual`, copiar el nombre exacto a `@BaseEsperada` y poner `@Aplicar=1` para instalar. Repetir el procedimiento en el entorno de destino después de validar la copia.
2. Desde la administración POS de usuarios, asignar el rol `GARITA` a los agentes y `GARITA_ADMIN` al personal responsable. La migración crea los roles y sus permisos, pero no vincula usuarios.
3. Un administrador abre `/Garita/Administrar`, busca cada login POS y asigna sus centros. La lista de centros se toma de las rutas existentes. La cuenta de garita no necesita vínculo de piloto ni placa.
4. Publicar el sitio con `GaritaController`, las vistas y `garita.css`. Se usan `GiveContext` y el catálogo de rutas configurado por `Pilotos.CatalogoRutas` (o `APK66Context` en la misma instancia), igual que el panel actual. No se requiere una conexión nueva.

## Flujo

El agente busca el ID exacto indicado por el piloto. La pantalla muestra placa, piloto, centro, fecha, transporte, vehículo, clientes, documentos, direcciones y bultos. Una ruta `A` solo puede salir si no está liquidada, tiene documentos sin resultado y el vehículo y piloto siguen activos. El agente marca las dos verificaciones, puede anotar precinto y observación y confirma una vez.

La confirmación actualiza `RT_RUTAS.STATUS` de `A` a `E` e inserta un evento `S` en `dbo.GaritaRutaEvento` en la **misma transacción**. `FechaUtc` se asigna con `SYSUTCDATETIME()` del servidor SQL; la interfaz la presenta en hora de Guatemala. `MO_HR_SALIDA` pertenece a cada documento y no se modifica. El evento conserva actor POS, centro, piloto, placa, total de documentos y bultos, huella de los datos revisados, precinto y observación. Un índice único impide registrar dos salidas para la misma ruta y catálogo. Repetir la misma solicitud responde sin repetir la transición.

Si hay diferencia, el agente la reporta como evento `D` y la ruta permanece `A`. La salida queda bloqueada. Distribución verifica el cargamento y corrige lo necesario; el administrador registra la solución como evento `R` en `/Garita/Administrar`. Garita vuelve a buscar la ruta y revisa los datos actualizados. El panel y el detalle administrativo muestran la salida y los eventos de garita; después de una salida, la edición administrativa impide cambiar fecha, piloto, centro, placa o estado y permite solo la observación general.

Las rutas históricas `E` sin evento de garita muestran “Sin registro de garita”. No se infiere ni inventa una hora de salida.

## Verificación

`Tests/Pilotos/run.ps1` comprueba reglas de estado, liquidación, diferencias, cambios de carga y placa, permisos separados, protección CSRF, compilación C# y Razor, render de los cuatro estados de la vista, navegador Chrome en móvil y escritorio, y sintaxis SQL 150 de la migración y la transición `A → E`. El script no se conecta a SQL.

Antes de habilitar la operación real, ejecutar en la copia aislada una prueba integrada con dos sesiones simultáneas: ruta abierta con carga, salida exitosa, doble envío, intento desde otro centro, usuario sin permiso, vehículo/piloto inactivo, ruta sin documentos, ruta liquidada, diferencia pendiente y resuelta, cambio de documento entre lectura y POST, cierre del piloto posterior a salida y rollback ante fallo de inserción del evento. Verificar que cada salida genere exactamente un evento y que `STATUS` vuelva a `A` cuando se fuerza el rollback. No usar una ruta de producción para esta prueba.
