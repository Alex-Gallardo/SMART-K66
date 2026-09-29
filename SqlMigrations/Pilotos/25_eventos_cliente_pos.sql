/* POS: conserva la finalización de clientes después de eliminar el borrador al cerrar.
   No modifica rutas ni documentos de APK66. No reconstruye eventos históricos. */
SET NOCOUNT ON;
DECLARE @BaseEsperada sysname=N'POS-SmartK66_DEV', @Aplicar bit=0;
IF DB_NAME()<>@BaseEsperada THROW 51000,'Seleccionar la base POS exacta.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
SELECT DB_NAME() BaseSeleccionada,@Aplicar Aplicar,
    CASE WHEN OBJECT_ID(N'dbo.PilotoClienteEvento',N'U') IS NULL THEN N'POR CREAR' ELSE N'EXISTE' END Estado;
IF @Aplicar=0 RETURN;
IF OBJECT_ID(N'dbo.Usuario',N'U') IS NULL THROW 51000,'Falta la tabla de usuarios POS.',1;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 3000;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.PilotoClienteEvento',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PilotoClienteEvento (
            Id bigint IDENTITY NOT NULL CONSTRAINT PK_PilotoClienteEvento PRIMARY KEY,
            CatalogoRutas sysname NOT NULL,
            Ruta_Id nvarchar(15) NOT NULL,
            Primer_RowId int NOT NULL,
            Usuario_Id bigint NOT NULL,
            Version varchar(44) NOT NULL,
            Actor nvarchar(50) NOT NULL,
            FechaUtc datetime2(3) NOT NULL CONSTRAINT DF_PilotoClienteEvento_Fecha DEFAULT(SYSUTCDATETIME()),
            CONSTRAINT FK_PilotoClienteEvento_Usuario FOREIGN KEY(Usuario_Id) REFERENCES dbo.Usuario(Usuario_Id),
            CONSTRAINT CK_PilotoClienteEvento_Row CHECK(Primer_RowId>0)
        );
        CREATE INDEX IX_PilotoClienteEvento_Ruta ON dbo.PilotoClienteEvento(CatalogoRutas,Ruta_Id,FechaUtc DESC);
        CREATE UNIQUE INDEX UX_PilotoClienteEvento_Cliente ON dbo.PilotoClienteEvento(CatalogoRutas,Usuario_Id,Ruta_Id,Primer_RowId,Version);
    END;
    IF COL_LENGTH(N'dbo.PilotoClienteEvento',N'CatalogoRutas') IS NULL OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'Version') IS NULL
       OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'Id') IS NULL OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'Ruta_Id') IS NULL
       OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'Primer_RowId') IS NULL OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'Usuario_Id') IS NULL
       OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'Actor') IS NULL OR COL_LENGTH(N'dbo.PilotoClienteEvento',N'FechaUtc') IS NULL
        THROW 51000,'La tabla de eventos existente no es compatible. Revisar sin sobrescribir.',1;
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    SELECT N'INSTALADO: nuevos eventos de cliente con catálogo de origen; sin reconstrucción histórica.' Resultado;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
