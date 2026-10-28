# Liquidación de rutas

La vista `/Liquidacion` requiere una cuenta POS activa, habilitada para web y con el permiso `Pilotos.Liquidar`. El permiso concede consulta global, liquidación y exportación; asígnalo desde la administración de permisos solo al personal autorizado. No se necesita vínculo de piloto, centro o placa.

## Antes de publicar

1. En **APK66**, ejecutar esta consulta de solo lectura y comprobar nombres, tipos y longitudes:

```sql
SELECT t.name AS Tabla,c.name AS Columna,TYPE_NAME(c.user_type_id) AS Tipo,c.max_length,c.scale,c.is_nullable
FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
WHERE t.name IN (N'RT_RUTAS',N'RT_RUTAS_DET')
  AND c.name IN (N'STATUS',N'LIQUIDADO',N'HR_ENTRADA',N'USR_LIQ',N'FECHA_LIQ',N'U_Usuario_liquida',N'U_U_FechaLiquidacion',N'MO_ENTREGA')
ORDER BY t.name,c.column_id;
```

   `USR_LIQ` y `U_Usuario_liquida` deben admitir el login POS completo; si no, hay que ampliar esas columnas o definir un código de operador antes de publicar. No se trunca el login. Verificar que los campos de fecha admiten la hora local de Guatemala.
2. En **POS**, abrir `34_permiso_liquidacion_pos.sql` en una ventana nueva, declarar el nombre exacto de la base en `@BaseEsperada`, revisar la vista previa, y cambiar `@Aplicar` a `1`. El script solo crea el permiso y se puede volver a ejecutar.
3. Asignar `Pilotos.Liquidar` mediante los roles y permisos existentes. Comprobar acceso con una cuenta autorizada y denegación con una sin el permiso.

## Flujo

El listado muestra rutas `C` sin liquidar en todos los centros. **Ver rutas liquidadas** añade rutas `L` con `LIQUIDADO=1` y registros históricos `C` que ya tengan `LIQUIDADO=1`. La selección para Excel se conserva entre páginas con el mismo filtro; el servidor vuelve a verificar todos los identificadores. El reporte `.xlsx` incluye dos hojas: Rutas y Documentos.

Para liquidar, la ruta debe estar `C`, `LIQUIDADO=0`, con `HR_ENTRADA` y al menos un documento. Un documento necesita resultado de entrega. La acción individual marca `U_Usuario_liquida` y `U_U_FechaLiquidacion`; la acción masiva marca todos los documentos pendientes en una transacción. Si algunos no tienen resultado, se liquidan individualmente los listos y se muestra el bloqueo del lote.

Cuando todos los documentos tienen resultado, usuario y fecha de liquidación, el botón **Liquidar ruta** marca `STATUS=L`, `LIQUIDADO=1`, `USR_LIQ` y `FECHA_LIQ`. El servidor comprueba de nuevo todas las condiciones dentro de una transacción serializable. Las horas de liquidación se generan desde UTC con desfase fijo UTC−6, hora de Guatemala. No se modifican resultados de entrega ni horas de Garita. Una confirmación repetida o concurrente se rechaza sin sobrescribir datos.

La migración 34 es la única migración de este cambio. Las columnas de APK66 son existentes; si la consulta de prepublicación no coincide, detener el despliegue y revisar el esquema antes de ejecutar acciones.
