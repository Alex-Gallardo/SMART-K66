/* PILOTOS - ESTRUCTURA DE SEGURIDAD POS (SOLO LECTURA)
   Abrir una ventana NUEVA de SSMS conectada a POS-SmartK66.
   Solo catalogos: no extrae usuarios, contrasenas, datos de clientes ni rutas.
   No crea/actualiza roles, permisos, menus o vinculaciones.
   LOCK_TIMEOUT limita espera por bloqueos, no la duracion total.
   Enviar todas las secciones, incluidas las vacias y cualquier error.
*/
IF DB_NAME() <> N'POS-SmartK66'
BEGIN
    RAISERROR(N'Seleccione POS-SmartK66 en SSMS antes de ejecutar este diagnostico.', 16, 1);
    RETURN;
END;
IF @@TRANCOUNT <> 0 OR (2 & @@OPTIONS) = 2
BEGIN
    RAISERROR(N'Use una ventana nueva sin transaccion abierta ni IMPLICIT_TRANSACTIONS.', 16, 1);
    RETURN;
END;

DECLARE @LockTimeoutAnterior int = @@LOCK_TIMEOUT;
BEGIN TRY
    SET LOCK_TIMEOUT 3000;

    SELECT N'01_ENTORNO_POS' AS SECCION, DB_NAME() AS BASE_ACTUAL,
        CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation')) AS COLLATION,
        HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION') AS PUEDE_VER_DEFINICIONES;

    SELECT N'02_COLUMNAS_POS' AS SECCION, s.name AS ESQUEMA, t.name AS TABLA,
        c.column_id AS ORDEN, c.name AS COLUMNA, ty.name AS TIPO,
        c.max_length AS LONGITUD_BYTES, c.precision, c.scale, c.is_nullable,
        c.is_identity, c.collation_name, dc.definition AS VALOR_DEFAULT
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.columns c ON c.object_id = t.object_id
    JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
    WHERE t.name IN (N'Usuario', N'Rol', N'Usuario_Rol', N'Permiso', N'Rol_Permiso',
                     N'Menu', N'Empresa', N'Agencia', N'Usuario_Empresa', N'Usuario_Agencia')
       OR t.name LIKE N'%Piloto%' OR t.name LIKE N'RT[_]%'
    ORDER BY s.name, t.name, c.column_id;

    SELECT N'03_CLAVES_POS' AS SECCION, s.name AS ESQUEMA, t.name AS TABLA,
        i.name AS INDICE, i.is_primary_key, i.is_unique, i.is_disabled,
        i.filter_definition, ic.key_ordinal, c.name AS COLUMNA, ic.is_included_column
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.indexes i ON i.object_id = t.object_id
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE (t.name IN (N'Usuario', N'Rol', N'Usuario_Rol', N'Permiso', N'Rol_Permiso',
                      N'Menu', N'Empresa', N'Agencia', N'Usuario_Empresa', N'Usuario_Agencia')
           OR t.name LIKE N'%Piloto%' OR t.name LIKE N'RT[_]%')
      AND i.index_id > 0 AND i.is_hypothetical = 0
    ORDER BY s.name, t.name, i.index_id, ic.index_column_id;

    SELECT N'04_RELACIONES_POS' AS SECCION, fk.name AS CLAVE_FORANEA,
        OBJECT_SCHEMA_NAME(pt.object_id) AS ESQUEMA_ORIGEN,
        pt.name AS TABLA_ORIGEN, pc.name AS COLUMNA_ORIGEN,
        OBJECT_SCHEMA_NAME(rt.object_id) AS ESQUEMA_DESTINO,
        rt.name AS TABLA_DESTINO, rc.name AS COLUMNA_DESTINO,
        fkc.constraint_column_id AS ORDEN, fk.is_disabled, fk.is_not_trusted
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
    JOIN sys.columns pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
    JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
    JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
    WHERE pt.name IN (N'Usuario', N'Rol', N'Usuario_Rol', N'Permiso', N'Rol_Permiso',
                      N'Menu', N'Usuario_Empresa', N'Usuario_Agencia')
       OR pt.name LIKE N'%Piloto%' OR rt.name LIKE N'%Piloto%'
    ORDER BY ESQUEMA_ORIGEN, pt.name, fk.name, fkc.constraint_column_id;

    SET LOCK_TIMEOUT @LockTimeoutAnterior;
END TRY
BEGIN CATCH
    SET LOCK_TIMEOUT @LockTimeoutAnterior;
    SELECT N'ERROR_DIAGNOSTICO' AS SECCION, ERROR_NUMBER() AS NUMERO,
        ERROR_LINE() AS LINEA, ERROR_MESSAGE() AS MENSAJE;
END CATCH;
