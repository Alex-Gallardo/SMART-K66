# Control de salida y entrada de garita

La vista `/Garita` usa el login POS. El rol `GARITA` permite consultar rutas de los centros asignados y registrar una salida o una diferencia. `GARITA_ADMIN` permite asignar centros y resolver diferencias; por sí solo no concede acceso operativo. El servidor comprueba permisos y centro en cada consulta y envío.

## Instalación

1. Aplicar primero las migraciones de garita ya existentes `27_garita_salida_pos.sql` y, si corresponde a la instalación, `28_garita_reparar_acceso_pos.sql`.
2. Aplicar `29_garita_foto_salida_pos.sql` y, **antes de publicar esta versión**, `33_garita_entrada_pos.sql` en POS. Ambos muestran una vista previa por defecto. Comprobar `SELECT DB_NAME() AS BaseActual`, copiar el nombre exacto a `@BaseEsperada` y cambiar `@Aplicar` a `1`. Probar primero en una copia aislada. La migración 33 permite el evento `I` (entrada) y crea un índice único para evitar duplicados. Es repetible si ya se aplicó correctamente. La aplicación mostrará un error de instalación si falta el índice.
3. En APK66 no hay migración de esquema: ya existen `dbo.RT_RUTAS.HR_SALIDA` y `dbo.RT_RUTAS.HR_ENTRADA` como `time(0) NULL`. Verificarlo antes de publicar con `SELECT c.name, TYPE_NAME(c.user_type_id) AS tipo, c.scale, c.is_nullable FROM sys.columns c WHERE c.object_id=OBJECT_ID('dbo.RT_RUTAS') AND c.name IN ('HR_SALIDA','HR_ENTRADA');`.
4. Asignar el rol `GARITA` a los agentes y sus centros en `/Garita/Administrar`. La cuenta no necesita vínculo de piloto ni placa. Se usan los permisos de garita existentes. Garita avanza `A` a `E` solo al registrar la salida; Distribución puede seguir administrando rutas conforme a sus permisos.

## Flujo

La pantalla inicial no muestra rutas. El agente busca la **placa exacta** escribiéndola o escaneando un QR cuyo contenido sea solo la placa. En HTTPS o `localhost`, el navegador puede abrir la vista previa continua de cámara, sujeto al permiso del usuario. En una dirección HTTP pública, el botón **Escanear QR** abre la captura nativa de fotografía en los teléfonos que la admiten; el agente también puede elegir una foto guardada o escribir la placa. El QR se lee en el navegador y la imagen no se sube al servidor. La búsqueda muestra tres secciones, cada una con hasta 50 resultados por página: `A` (Abiertas), `E` (En ruta) y `C` (Cerradas sin `RT_RUTAS.HR_ENTRADA`). Se aplican placa exacta y centros autorizados. `B`, `L` y toda ruta con `LIQUIDADO=1` quedan excluidas de Garita.

### Cámara QR y HTTPS

El acceso actual por `http://46.250.169.149:8090/pedidosap/` no permite la vista previa continua de cámara: los navegadores reservan [`getUserMedia`](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia) para contextos seguros. **El cambio de código no elimina ese requisito.** En HTTP, [`capture="environment"`](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/capture) solicita al sistema del teléfono tomar una foto con la cámara posterior; algunos navegadores abren un selector de archivos en su lugar. La opción **Elegir foto guardada** y la búsqueda manual siguen disponibles. Probar esta captura en un teléfono físico usado por Garita; la emulación móvil de Chrome en una PC no reproduce el selector nativo del teléfono.

Para habilitar el escaneo continuo más adelante:

1. Configurar un nombre DNS para el portal y un certificado TLS válido y confiable para ese nombre. Crear un enlace HTTPS en el sitio IIS que conserva la aplicación `/pedidosap`, o terminar TLS en un proxy que dirija al IIS actual. No usar una dirección o certificado autogenerado que el teléfono marque como inseguro.
2. Comprobar desde el teléfono que login, `/pedidosap/Garita/Index`, `garita-qr.js` y el worker QR se cargan por HTTPS. En la consola, `window.isSecureContext` debe ser `true` y `navigator.mediaDevices.getUserMedia` debe estar disponible. Conceder el permiso de cámara al sitio.
3. Probar una placa real con QR, buscar otra placa y regresar a la lista. Revisar que las URL HTTP de fotografías u otros módulos no generen contenido mixto al migrar el resto del portal. Configurar una redirección HTTP a HTTPS únicamente después de comprobar el enlace y el login; no se incluye una redirección a una URL inexistente.

Esta corrección no requiere migración SQL ni permisos nuevos.

Para registrar la salida, la ruta debe estar `A` o `E`, sin liquidación, sin salida ni diferencia pendientes, con piloto y vehículo válidos y documentos aún sin resultado. El agente marca una sola verificación de clientes, facturas y bultos. Puede añadir observación y una foto JPG, PNG o WebP de hasta 10 MB. El precinto ya no se solicita, pero los datos históricos siguen visibles.

En una sola transacción SQL se registra primero `RT_RUTAS.HR_SALIDA` en APK66 y, si `STATUS=A`, se avanza a `E`. Si ya estaba `E`, conserva ese estado. Después se registra el evento `S` en `dbo.GaritaRutaEvento` de POS. Ambos usan la misma hora del servidor: APK66 guarda hora local de Guatemala en `time(0)` y POS guarda fecha y hora completas en UTC. Si se adjuntó una foto, se inserta en `dbo.GaritaSalidaImagen` en la misma transacción. Cualquier fallo revierte todas las escrituras. El índice único de la migración 27 impide otra salida.

Para una ruta cerrada sin liquidar y sin `HR_ENTRADA`, Garita compara placa, ruta y cargamento y pulsa **Registrar hora de entrada**. En una sola transacción se actualiza primero `RT_RUTAS.HR_ENTRADA` en APK66 y después se guarda el evento `I` en POS. La ruta continúa cerrada. El índice único de la migración 33 impide otra entrada. Si ya existía un evento de entrada en POS de una versión anterior pero falta `HR_ENTRADA` en APK66, el botón completa APK66 con la hora del evento existente, sin duplicarlo. Si APK66 ya tiene `HR_ENTRADA`, la ruta no aparece entre las pendientes.

Si algo no coincide, Garita reporta una diferencia `D` y detiene la salida. Distribución corrige o verifica los datos y un administrador resuelve la diferencia mediante `R`; después Garita vuelve a buscar la placa. Las rutas que dejan de estar `A`, `E` o `C` desaparecen de los resultados de búsqueda, pero su foto permanece disponible en el panel de Distribución. No se inventa una hora de salida para rutas históricas sin evento de garita.

## Verificación

`Tests/Pilotos/run.ps1` comprueba reglas, permisos, validación de imagen, protección CSRF, compilación C# y Razor, render de búsqueda y detalle, navegador Chrome móvil y escritorio, lector QR con cámara simulada en origen seguro y lectura de foto en HTTP inseguro, y sintaxis SQL 150 de consultas y migraciones. El script **no ejecuta consultas contra SQL Server**.

Antes de habilitar la operación real, probar con copias aisladas **de POS y APK66 en la misma instancia SQL**, pues la operación usa una transacción entre ambos catálogos: placa con rutas `E` y `C`, `C` con/sin `HR_ENTRADA`, una `C` liquidada, paginación, QR válido e inválido, permiso de cámara denegado, acceso desde otro centro, cambio de estado entre búsqueda y envío, salida con/sin foto, doble envío y regreso. Comprobar en ambas bases que un error revierte toda la operación, que `HR_SALIDA` y `HR_ENTRADA` coinciden con el evento POS en hora de Guatemala y que `STATUS` no cambia.
