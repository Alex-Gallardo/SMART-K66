/*
 * Ejecutar en la base SQL Server de SMART-K66 seleccionada explícitamente
 * por el operador. No ejecutar en HANA.
 *
 * Este script registra la capacidad global; NO la otorga a ningún rol.
 * Asignar Control.OTIF.VerTodos en la administración de roles solo a quienes
 * deban consultar todas las empresas y todos los vendedores de OTIF.
 */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Permiso', N'U') IS NULL OR OBJECT_ID(N'dbo.Rol_Permiso', N'U') IS NULL
    THROW 57001, 'Faltan las tablas de seguridad Permiso/Rol_Permiso.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1 FROM dbo.Permiso
        WHERE Nombre = N'Control.OTIF.VerTodos'
          AND (Descripcion <> N'Ver todos los vendedores y empresas del dashboard OTIF'
               OR Modulo <> N'OTIF')
    )
        THROW 57002, 'El permiso OTIF ya existe con un contrato diferente.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Permiso WITH (UPDLOCK, HOLDLOCK)
        WHERE Nombre = N'Control.OTIF.VerTodos'
    )
        INSERT INTO dbo.Permiso (Nombre, Descripcion, Modulo)
        VALUES (N'Control.OTIF.VerTodos',
                N'Ver todos los vendedores y empresas del dashboard OTIF', N'OTIF');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT Nombre, Descripcion, Modulo
FROM dbo.Permiso
WHERE Nombre = N'Control.OTIF.VerTodos';
