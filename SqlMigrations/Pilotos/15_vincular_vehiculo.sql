/* MANUAL: Distribucion autoriza o revoca una placa para un usuario POS existente.
   Una cuenta puede tener varias placas; la ruta debe coincidir TAMBIEN con su
   empleado y centro. No reasigna rutas en APK66. Mantiene historial en POS.
   Completar localmente. Vista previa por defecto; sin datos reales en Git. */
DECLARE @BasePos sysname=NULL,@BaseRutas sysname=NULL;
DECLARE @UsuarioId bigint=NULL,@Placa nvarchar(15)=NULL;
DECLARE @Activar bit=1,@Motivo nvarchar(250)=NULL,@Aplicar bit=0;
IF @BasePos IS NULL OR @BaseRutas IS NULL OR DB_NAME()<>@BasePos OR @BasePos=@BaseRutas
    THROW 51000,'Declarar ambos catalogos y seleccionar la base POS correcta.',1;
SET @Placa=LTRIM(RTRIM(@Placa));
IF @UsuarioId IS NULL OR NULLIF(@Placa,N'') IS NULL OR @Activar IS NULL OR NULLIF(LTRIM(RTRIM(@Motivo)),N'') IS NULL
    THROW 51000,'Completar usuario, placa, activar/desactivar y motivo.',1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000,'Usar una ventana nueva sin transacciones ni opciones modificadas.',1;
IF OBJECT_ID(N'dbo.PilotoVehiculo',N'U') IS NULL OR OBJECT_ID(N'dbo.PilotoVehiculoHistorial',N'U') IS NULL
    THROW 51000,'Falta la instalacion del script 14.',1;
DECLARE @Sql nvarchar(max)=N'
IF @activar=1
BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.Usuario u JOIN dbo.PilotoVinculo p ON p.Usuario_Id=u.Usuario_Id
   WHERE u.Usuario_Id=@u AND u.Activo=1 AND u.Autenticar_Site=1 AND p.Activo=1)
   THROW 51000,''El usuario debe estar activo y vinculado a un piloto.'',1;
 IF NOT EXISTS(SELECT 1 FROM '+QUOTENAME(@BaseRutas)+N'.dbo.RT_VEHICULOS WHERE PLACA=@placa AND ESTADO=1)
   THROW 51000,''La placa no existe o esta inactiva en rutas.'',1;
END;
DECLARE @antes bit;
SELECT @antes=Activo FROM dbo.PilotoVehiculo WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u AND Placa=@placa;
IF @antes IS NULL AND @activar=0 THROW 51000,''No existe el vinculo a desactivar.'',1;
SELECT @u AS Usuario_Id,@placa AS Placa,@antes AS ActivoAnterior,@activar AS ActivoPropuesto,@aplicar AS Aplicar;
IF @aplicar=1 AND (@antes IS NULL OR @antes<>@activar)
BEGIN
 IF @antes IS NULL
   INSERT dbo.PilotoVehiculo(Usuario_Id,Placa,Activo) VALUES(@u,@placa,@activar);
 ELSE
   UPDATE dbo.PilotoVehiculo SET Activo=@activar,ModificadoUtc=SYSUTCDATETIME(),ModificadoPor=ORIGINAL_LOGIN()
   WHERE Usuario_Id=@u AND Placa=@placa;
 INSERT dbo.PilotoVehiculoHistorial(Usuario_Id,Placa,ActivoAnterior,ActivoNuevo,Motivo)
 VALUES(@u,@placa,@antes,@activar,@motivo);
END;';
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    EXEC sys.sp_executesql @Sql,N'@u bigint,@placa nvarchar(15),@activar bit,@motivo nvarchar(250),@aplicar bit',@UsuarioId,@Placa,@Activar,@Motivo,@Aplicar;
    COMMIT;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
