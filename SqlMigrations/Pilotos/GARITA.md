# Control de salida de garita

La vista `/Garita` usa el login POS. El rol `GARITA` permite consultar rutas de los centros asignados y registrar una salida o una diferencia. `GARITA_ADMIN` permite asignar centros y resolver diferencias; por sí solo no concede acceso operativo. El servidor comprueba permisos y centro en cada consulta y envío.

## Instalación

1. Aplicar primero las migraciones de garita ya existentes `27_garita_salida_pos.sql` y, si corresponde a la instalación, `28_garita_reparar_acceso_pos.sql`.
2. Antes de publicar esta versión, ejecutar `29_garita_foto_salida_pos.sql` en POS. El archivo muestra una vista previa por defecto. Comprobar `SELECT DB_NAME() AS BaseActual`, copiar el nombre exacto a `@BaseEsperada` y cambiar `@Aplicar` a `1`. Revisar y probar primero en una copia aislada. La aplicación mostrará un error de instalación en Garita si falta la tabla nueva.
3. Asignar el rol `GARITA` a los agentes y sus centros en `/Garita/Administrar`. La cuenta no necesita vínculo de piloto ni placa. Distribución conserva el cambio de estado de la ruta.

## Flujo

La pantalla inicial no muestra rutas. El agente busca la **placa exacta** y recibe las rutas `E` (“En ruta”) de esa placa en sus centros autorizados, hasta 50 por página. Cada resultado muestra ID, fecha, piloto, centro, placa, clientes, documentos, bultos y si la salida ya fue registrada o hay una diferencia. El agente elige la ruta concreta y revisa el detalle.

Para registrar la salida, la ruta debe seguir `E`, sin liquidación, sin salida ni diferencia pendientes, con piloto y vehículo válidos y documentos aún sin resultado. El agente marca una sola verificación de clientes, facturas y bultos. Puede añadir observación y una foto JPG, PNG o WebP de hasta 10 MB. El precinto ya no se solicita, pero los datos históricos siguen visibles.

El registro inserta un evento `S` en `dbo.GaritaRutaEvento`; **no modifica `RT_RUTAS.STATUS`**. `FechaUtc` usa `SYSUTCDATETIME()` del servidor SQL y la vista muestra la hora de Guatemala. Si se adjuntó una foto, se inserta en `dbo.GaritaSalidaImagen` en la misma transacción y se asocia al evento. Un fallo de foto revierte también el evento. El índice único de la migración 27 impide registrar dos salidas para la misma ruta. Garita y el detalle administrativo de Distribución pueden consultar la evidencia con sus permisos respectivos.

Si algo no coincide, Garita reporta una diferencia `D` y detiene la salida. Distribución corrige o verifica los datos y un administrador resuelve la diferencia mediante `R`; después Garita vuelve a buscar la placa. Las rutas que dejan de estar `E` desaparecen de los resultados de búsqueda, pero su foto permanece disponible en el panel de Distribución. No se inventa una hora de salida para rutas históricas sin evento de garita.

## Verificación

`Tests/Pilotos/run.ps1` comprueba reglas, permisos, validación de imagen, protección CSRF, compilación C# y Razor, render de búsqueda y detalle, navegador Chrome móvil y escritorio, y sintaxis SQL 150 de las consultas y la migración. El script **no ejecuta consultas contra SQL Server**.

Antes de habilitar la operación real, probar en una copia aislada de POS: una placa con varias rutas `E`, paginación, placa sin rutas, acceso desde otro centro, cambio de estado entre búsqueda y envío, salida con y sin foto, foto inválida o excesiva, doble envío, diferencia pendiente y resuelta, y acceso a la foto desde Garita y Distribución. Comprobar que un fallo al guardar la foto deja cero eventos de salida y que `RT_RUTAS.STATUS` permanece `E` después de registrar la hora.
