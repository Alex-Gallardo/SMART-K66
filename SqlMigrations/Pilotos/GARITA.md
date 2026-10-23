# Control de salida y entrada de garita

La vista `/Garita` usa el login POS. El rol `GARITA` permite consultar rutas de los centros asignados y registrar salida, entrada o diferencias con los permisos existentes `Garita.Rutas.Ver` y `Garita.Rutas.ConfirmarSalida`. No se crean permisos adicionales. `GARITA_ADMIN` permite asignar centros y resolver diferencias; por sí solo no concede acceso operativo. El servidor comprueba permisos y centro en cada consulta y envío.

## Instalación

1. Aplicar primero las migraciones de garita ya existentes `27_garita_salida_pos.sql` y, si corresponde a la instalación, `28_garita_reparar_acceso_pos.sql`.
2. Antes de publicar esta versión, ejecutar `29_garita_foto_salida_pos.sql` en POS. El archivo muestra una vista previa por defecto. Comprobar `SELECT DB_NAME() AS BaseActual`, copiar el nombre exacto a `@BaseEsperada` y cambiar `@Aplicar` a `1`. Revisar y probar primero en una copia aislada. La aplicación mostrará un error de instalación en Garita si falta la tabla nueva.
3. Asignar el rol `GARITA` a los agentes y sus centros en `/Garita/Administrar`. La cuenta no necesita vínculo de piloto ni placa. Distribución conserva el cambio de estado de la ruta.
4. Antes de publicar la entrada, ejecutar en este orden: `33_garita_horas_ruta_apk66.sql` en el catálogo APK66, `34_garita_entrada_pos.sql` en POS y `35_garita_reconciliar_salidas_apk66.sql` nuevamente en APK66. Cada archivo empieza en vista previa. Declarar los nombres exactos de catálogo y activar `@Aplicar=1` solo tras revisar el resultado. La migración 35 copia a APK66 salidas POS anteriores únicamente donde la hora de la ruta está vacía y la ruta no está liquidada. Probar primero en una copia aislada de ambas bases.
5. `36_garita_verificar_horas_solo_lectura.sql` compara una ruta concreta entre APK66 y POS después de registrar salida y entrada; debe indicar `COINCIDE` en ambos casos. No modifica datos.

## Flujo

La pantalla inicial no muestra rutas. El agente busca la **placa exacta** y recibe primero las rutas `E` (“En ruta”) y debajo las rutas `C` (“Cerrada”) sin hora de entrada. Ambas secciones excluyen liquidadas, respetan los centros autorizados y se paginan por separado hasta 50 resultados. Cada resultado muestra ID, fecha, piloto, centro, placa, clientes, documentos y bultos. Las cerradas muestran “Sin hora de entrada registrada”; si falta la salida, se avisa sin impedir el regreso.

Para registrar la salida, la ruta debe seguir `E`, sin liquidación, sin salida ni diferencia pendientes, con piloto y vehículo válidos y documentos aún sin resultado. El agente marca una sola verificación de clientes, facturas y bultos. Puede añadir observación y una foto JPG, PNG o WebP de hasta 10 MB. El precinto ya no se solicita, pero los datos históricos siguen visibles.

El registro escribe `RT_RUTAS.GARITA_FECHA_SALIDA` en hora Guatemala e inserta un evento `S` en `dbo.GaritaRutaEvento` con la misma hora en UTC. Ambas escrituras y la foto opcional comparten transacción: un fallo revierte todas. **No modifica `RT_RUTAS.STATUS`**. El índice único de la migración 27 impide registrar dos salidas para la misma ruta.

Cuando la ruta está `C`, no liquidada y sin entrada, el agente comprueba el regreso y pulsa **Registrar hora de entrada**. El servidor toma la hora automáticamente y escribe `RT_RUTAS.GARITA_FECHA_ENTRADA` junto con un evento POS `I` en la misma transacción. El índice único de la migración 34 evita duplicados. La ruta desaparece de la sección de entradas pendientes. `MO_HR_ENTRADA` de `RT_RUTAS_DET` permanece como hora de cada documento; no representa el regreso del vehículo. Garita y el panel de Distribución muestran las dos horas de ruta y los agentes registrados en POS.

Si algo no coincide, Garita reporta una diferencia `D` y detiene la salida. Distribución corrige o verifica los datos y un administrador resuelve la diferencia mediante `R`; después Garita vuelve a buscar la placa. Las rutas que dejan de estar `E` desaparecen de la primera sección. Al cerrarse, aparecen en la segunda si aún no tienen entrada y no están liquidadas. No se inventa una hora de salida para rutas históricas sin evento de garita.

## Verificación

`Tests/Pilotos/run.ps1` comprueba reglas, permisos, validación de imagen, protección CSRF, compilación C# y Razor, render de búsqueda y detalle, navegador Chrome móvil y escritorio, y sintaxis SQL 150 de las consultas y la migración. El script **no ejecuta consultas contra SQL Server**.

Antes de habilitar la operación real, probar en una copia aislada de POS y APK66: una placa con rutas `E` y `C`, paginación independiente, exclusión de liquidadas, placa sin rutas, acceso desde otro centro, cambio de estado entre búsqueda y envío, salida con y sin foto, doble envío y entrada duplicada. Comprobar que un fallo al guardar la foto deja cero eventos y ninguna hora en APK66; que la entrada desaparece de pendientes, que las dos horas y eventos coinciden y que `RT_RUTAS.STATUS` no cambia por Garita.
