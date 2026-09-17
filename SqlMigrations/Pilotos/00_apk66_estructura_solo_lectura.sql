/* PILOTOS - PRIMER DIAGNOSTICO APK66 (SOLO LECTURA, REVISION 2)
   En SSMS: abrir una ventana NUEVA conectada a APK66 y ejecutar TODO.
   Solo consulta catalogos del sistema: no lee filas de rutas/usuarios,
   no ejecuta procedimientos, no cambia datos, esquema ni permisos.
   No usa NOLOCK, transacciones explicitas, tablas temporales ni SQL dinamico.
   LOCK_TIMEOUT limita la espera por bloqueos, NO la duracion total.
   Si hay timeout/error, enviar el mensaje; no reintentar en bucle.
   Enviar todas las secciones, incluidas las vacias.
*/
IF DB_NAME() <> N'APK66'
BEGIN
    RAISERROR(N'Seleccione APK66 en SSMS antes de ejecutar este diagnostico.', 16, 1);
    RETURN;
END;
IF @@TRANCOUNT <> 0 OR (2 & @@OPTIONS) = 2
BEGIN
    RAISERROR(N'Use una ventana nueva sin transaccion abierta ni IMPLICIT_TRANSACTIONS.', 16, 1);
    RETURN;
END;

-- SET LOCK_TIMEOUT requiere un literal, no acepta una variable.
-- Exigir el valor inicial permite restaurarlo sin SQL dinamico.
IF @@LOCK_TIMEOUT <> -1
BEGIN
    RAISERROR(N'Abra una ventana nueva con LOCK_TIMEOUT inicial (-1). No se ha cambiado la sesion.', 16, 1);
    RETURN;
END;
BEGIN TRY
    SET LOCK_TIMEOUT 3000;

    SELECT N'01_ENTORNO' AS SECCION, DB_NAME() AS BASE_ACTUAL,
        CONVERT(nvarchar(30), SERVERPROPERTY('ProductVersion')) AS VERSION_SQL,
        CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation')) AS COLLATION,
        HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION') AS PUEDE_VER_DEFINICIONES;

    SELECT N'02_OBJETOS_RT' AS SECCION, s.name AS ESQUEMA, o.name AS OBJETO,
        o.type_desc AS TIPO
    FROM sys.objects o
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE o.is_ms_shipped = 0 AND o.name LIKE N'RT[_]%'
    ORDER BY s.name, o.name;

    SELECT N'03_COLUMNAS' AS SECCION, s.name AS ESQUEMA, t.name AS TABLA,
        c.column_id AS ORDEN, c.name AS COLUMNA, ts.name AS ESQUEMA_TIPO,
        ty.name AS TIPO, c.max_length AS LONGITUD_BYTES, c.precision, c.scale,
        c.is_nullable, c.is_identity, c.is_computed, c.collation_name,
        dc.definition AS VALOR_DEFAULT, cc.definition AS EXPRESION_CALCULADA
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.columns c ON c.object_id = t.object_id
    JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    JOIN sys.schemas ts ON ts.schema_id = ty.schema_id
    LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
    LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
    WHERE t.name LIKE N'RT[_]%'
    ORDER BY s.name, t.name, c.column_id;

    SELECT N'04_CLAVES_E_INDICES' AS SECCION, s.name AS ESQUEMA, t.name AS TABLA,
        i.name AS INDICE, i.type_desc, i.is_primary_key, i.is_unique,
        i.is_unique_constraint, i.is_disabled, i.has_filter, i.filter_definition,
        ic.key_ordinal, ic.index_column_id, c.name AS COLUMNA,
        ic.is_descending_key, ic.is_included_column
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.indexes i ON i.object_id = t.object_id
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE t.name LIKE N'RT[_]%' AND i.index_id > 0 AND i.is_hypothetical = 0
    ORDER BY s.name, t.name, i.index_id, ic.index_column_id;

    SELECT N'05_RELACIONES' AS SECCION, fk.name AS CLAVE_FORANEA,
        ps.name AS ESQUEMA_ORIGEN, pt.name AS TABLA_ORIGEN, pc.name AS COLUMNA_ORIGEN,
        rs.name AS ESQUEMA_DESTINO, rt.name AS TABLA_DESTINO, rc.name AS COLUMNA_DESTINO,
        fkc.constraint_column_id AS ORDEN, fk.is_disabled, fk.is_not_trusted,
        fk.update_referential_action_desc, fk.delete_referential_action_desc
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
    JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
    JOIN sys.columns pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
    JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
    JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
    JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
    WHERE pt.name LIKE N'RT[_]%' OR rt.name LIKE N'RT[_]%'
    ORDER BY ps.name, pt.name, fk.name, fkc.constraint_column_id;

    SELECT N'06_RESTRICCIONES' AS SECCION, s.name AS ESQUEMA, t.name AS TABLA,
        ck.name AS RESTRICCION, ck.definition, ck.is_disabled, ck.is_not_trusted
    FROM sys.check_constraints ck
    JOIN sys.tables t ON t.object_id = ck.parent_object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name LIKE N'RT[_]%'
    ORDER BY s.name, t.name, ck.name;

    SELECT N'07_TRIGGERS' AS SECCION, s.name AS ESQUEMA, t.name AS TABLA,
        tr.name AS TRIGGER_NOMBRE, tr.is_disabled, tr.is_instead_of_trigger,
        ev.type_desc AS EVENTO,
        CASE WHEN sm.definition IS NULL THEN 0 ELSE 1 END AS DEFINICION_VISIBLE
    FROM sys.triggers tr
    JOIN sys.tables t ON t.object_id = tr.parent_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    LEFT JOIN sys.trigger_events ev ON ev.object_id = tr.object_id
    LEFT JOIN sys.sql_modules sm ON sm.object_id = tr.object_id
    WHERE t.name LIKE N'RT[_]%'
    ORDER BY s.name, t.name, tr.name, ev.type_desc;

    -- Incluye referencias registradas hacia/desde objetos RT en esta base.
    -- No descubre SQL dinamico ni consultas de otras aplicaciones/bases.
    SELECT N'08_DEPENDENCIAS' AS SECCION,
        OBJECT_SCHEMA_NAME(d.referencing_id) AS ESQUEMA_MODULO,
        OBJECT_NAME(d.referencing_id) AS MODULO,
        o.type_desc AS TIPO_MODULO, d.referenced_server_name,
        d.referenced_database_name, d.referenced_schema_name,
        d.referenced_entity_name, d.is_ambiguous
    FROM sys.sql_expression_dependencies d
    LEFT JOIN sys.objects o ON o.object_id = d.referencing_id
    WHERE d.referenced_entity_name LIKE N'RT[_]%'
       OR o.name LIKE N'RT[_]%'
    ORDER BY ESQUEMA_MODULO, MODULO, d.referenced_entity_name;

    -- Inventario de codigo SQL relacionado; no devuelve el codigo fuente.
    -- La coincidencia textual es una pista, no demuestra una dependencia.
    SELECT N'09_MODULOS_CANDIDATOS' AS SECCION, s.name AS ESQUEMA,
        o.name AS MODULO, o.type_desc AS TIPO,
        CASE WHEN sm.definition IS NULL THEN 0 ELSE 1 END AS DEFINICION_VISIBLE
    FROM sys.objects o
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    LEFT JOIN sys.sql_modules sm ON sm.object_id = o.object_id
    WHERE o.is_ms_shipped = 0 AND o.type IN ('P', 'V', 'FN', 'IF', 'TF', 'TR')
      AND (o.name LIKE N'RT[_]%' OR sm.definition LIKE N'%RT[_]%'
           OR EXISTS (SELECT 1 FROM sys.triggers tr
                      JOIN sys.tables t ON t.object_id = tr.parent_id
                      WHERE tr.object_id = o.object_id AND t.name LIKE N'RT[_]%'))
    ORDER BY s.name, o.name;

    SELECT N'10_SINONIMOS_RT' AS SECCION, s.name AS ESQUEMA,
        sn.name AS SINONIMO, sn.base_object_name AS OBJETO_DESTINO
    FROM sys.synonyms sn
    JOIN sys.schemas s ON s.schema_id = sn.schema_id
    WHERE sn.name LIKE N'RT[_]%' OR sn.base_object_name LIKE N'%RT[_]%'
    ORDER BY s.name, sn.name;

    SET LOCK_TIMEOUT -1;
END TRY
BEGIN CATCH
    SET LOCK_TIMEOUT -1;
    SELECT N'ERROR_DIAGNOSTICO' AS SECCION, ERROR_NUMBER() AS NUMERO,
        ERROR_LINE() AS LINEA, ERROR_MESSAGE() AS MENSAJE;
END CATCH;
