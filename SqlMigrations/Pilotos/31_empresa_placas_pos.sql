/* Ejecutar manualmente en POS. Vista previa por defecto.
   Declarar el catalogo POS exacto y cambiar @Aplicar a 1 tras revisar duplicados. */
DECLARE @BaseEsperada sysname=NULL,@Aplicar bit=0;
IF OBJECT_ID(N'dbo.PilotoEmpresa',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NULL
    OR OBJECT_ID(N'dbo.PilotoVinculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVinculoHistorial',N'U') IS NULL
    THROW 51000,'Primero instalar las migraciones anteriores de pilotos en POS.',1;
IF @Aplicar=0
BEGIN
    SELECT DB_NAME() BaseSeleccionada,N'VISTA PREVIA: permitir empresas sin empleado APK66 y registrar propietario unico por placa.' Accion;
    SELECT pv.Placa,COUNT(DISTINCT pe.Usuario_Id) Empresas
    FROM dbo.PilotoEmpresa pe JOIN dbo.PilotoVehiculo pv ON pv.Usuario_Id=pe.Usuario_Id AND pv.Activo=1
    WHERE pe.Activo=1 GROUP BY pv.Placa HAVING COUNT(DISTINCT pe.Usuario_Id)>1;
    SELECT pe.Nit,pe.Nombre,COUNT(pv.Placa) PlacasVinculadas
    FROM dbo.PilotoEmpresa pe LEFT JOIN dbo.PilotoVehiculo pv ON pv.Usuario_Id=pe.Usuario_Id AND pv.Activo=1
    WHERE pe.Activo=1 GROUP BY pe.Nit,pe.Nombre ORDER BY pe.Nombre;
    RETURN;
END;
IF @BaseEsperada IS NULL OR DB_NAME()<>@BaseEsperada
    THROW 51000,'Seleccionar y declarar el catalogo POS exacto.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.PilotoEmpresaPlaca',N'U') IS NOT NULL
    THROW 51000,'La migracion 31 ya existe o esta incompleta. Revisar; no sobrescribir.',1;
IF EXISTS(SELECT 1 FROM dbo.PilotoEmpresa pe JOIN dbo.PilotoVehiculo pv ON pv.Usuario_Id=pe.Usuario_Id AND pv.Activo=1
          WHERE pe.Activo=1 GROUP BY pv.Placa HAVING COUNT(DISTINCT pe.Usuario_Id)>1)
    THROW 51000,'Una placa pertenece a varias empresas activas. Resolver los vinculos antes de aplicar.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DROP INDEX UX_PilotoVinculo_EmpleadoActivo ON dbo.PilotoVinculo;
    ALTER TABLE dbo.PilotoVinculo ALTER COLUMN Empleado_RowId int NULL;
    ALTER TABLE dbo.PilotoVinculoHistorial ALTER COLUMN EmpleadoNuevo int NULL;
    CREATE UNIQUE INDEX UX_PilotoVinculo_EmpleadoActivo ON dbo.PilotoVinculo(Empleado_RowId)
        WHERE Activo=1 AND Empleado_RowId IS NOT NULL;
    CREATE TABLE dbo.PilotoEmpresaPlaca
    (
        Placa nvarchar(15) NOT NULL CONSTRAINT PK_PilotoEmpresaPlaca PRIMARY KEY,
        Usuario_Id bigint NOT NULL CONSTRAINT FK_PilotoEmpresaPlaca_Empresa REFERENCES dbo.PilotoEmpresa(Usuario_Id),
        AsignadoUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoEmpresaPlaca_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_PilotoEmpresaPlaca_Placa CHECK(LEN(LTRIM(RTRIM(Placa)))>0)
    );
    CREATE INDEX IX_PilotoEmpresaPlaca_Usuario ON dbo.PilotoEmpresaPlaca(Usuario_Id,Placa);
    CREATE INDEX IX_PilotoVehiculo_PlacaActivo ON dbo.PilotoVehiculo(Placa,Activo,Usuario_Id);
    INSERT dbo.PilotoEmpresaPlaca(Placa,Usuario_Id)
    SELECT pv.Placa,pe.Usuario_Id
    FROM dbo.PilotoEmpresa pe JOIN dbo.PilotoVehiculo pv ON pv.Usuario_Id=pe.Usuario_Id AND pv.Activo=1
    WHERE pe.Activo=1;
    COMMIT;
    SELECT DB_NAME() BaseInstalada,N'INSTALADO' Estado;
    SELECT pe.Nit,pe.Nombre,COUNT(ep.Placa) PlacasPropias
    FROM dbo.PilotoEmpresa pe LEFT JOIN dbo.PilotoEmpresaPlaca ep ON ep.Usuario_Id=pe.Usuario_Id
    WHERE pe.Activo=1 GROUP BY pe.Nit,pe.Nombre ORDER BY pe.Nombre;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
