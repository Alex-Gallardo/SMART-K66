/* MANUAL: alta de un vinculo, una vez creado el usuario con rol PILOTO en POS.
   No actualiza cuentas ni asignaciones de rutas. Por defecto solo muestra el plan.
   No reutilizar un vinculo activo para otra persona; desactivarlo primero, con el
   portal detenido, y conservar la auditoria. Completar parametros localmente. */
DECLARE @BasePos sysname=NULL, @BaseRutas sysname=NULL;
DECLARE @UsuarioId bigint=NULL, @EmpleadoRowId int=NULL;
DECLARE @Operador nvarchar(15)=NULL; -- Codigo de auditoria acordado con Distribucion, sin truncar el login.
DECLARE @Centro nvarchar(15)=NULL; -- Un centro por alta; otros centros requieren revision explicita.
DECLARE @Aplicar bit=0;
IF @BasePos IS NULL OR @BaseRutas IS NULL OR DB_NAME()<>@BasePos OR @BasePos=@BaseRutas
    THROW 51000, 'Completar los catalogos y seleccionar la base POS indicada.', 1;
IF @UsuarioId IS NULL OR @EmpleadoRowId IS NULL OR NULLIF(LTRIM(RTRIM(@Operador)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@Centro)),N'') IS NULL
    THROW 51000, 'Completar usuario, empleado, codigo de auditoria y centro.', 1;
IF @@TRANCOUNT<>0 OR (2 & @@OPTIONS)=2 OR @@LOCK_TIMEOUT<>-1 OR (16384 & @@OPTIONS)=16384
    THROW 51000, 'Usar una ventana nueva sin transacciones ni opciones de sesion modificadas.', 1;
DECLARE @Sql nvarchar(max)=N'
IF NOT EXISTS(SELECT 1 FROM dbo.Usuario u WHERE u.Usuario_Id=@u AND u.Activo=1 AND u.Autenticar_Site=1
 AND NOT EXISTS(SELECT 1 FROM dbo.Usuario otro WHERE otro.Login=u.Login AND otro.Usuario_Id<>u.Usuario_Id)
 AND EXISTS(SELECT 1 FROM dbo.Usuario_Rol ur JOIN dbo.Rol r ON r.Rol_Id=ur.Rol_Id WHERE ur.Usuario_Id=u.Usuario_Id AND r.Nombre=N''PILOTO''))
 THROW 51000, ''El usuario debe estar activo, habilitado para web y tener rol PILOTO.'', 1;
IF NOT EXISTS(SELECT 1 FROM '+QUOTENAME(@BaseRutas)+N'.dbo.RT_EMPLEADOS e WHERE e.ROWID=@e AND e.ESTADO=1 AND e.CARGO=N''PILOTO''
 AND NULLIF(LTRIM(RTRIM(e.NOMBRE)),N'''') IS NOT NULL
 AND NOT EXISTS(SELECT 1 FROM '+QUOTENAME(@BaseRutas)+N'.dbo.RT_EMPLEADOS otro WHERE otro.CARGO=N''PILOTO'' AND otro.NOMBRE=e.NOMBRE AND otro.ROWID<>e.ROWID))
 THROW 51000, ''Empleado inactivo, inexistente o con nombre ambiguo.'', 1;
IF EXISTS(SELECT 1 FROM dbo.PilotoVinculo WITH(UPDLOCK,HOLDLOCK) WHERE Usuario_Id=@u OR (Activo=1 AND (Empleado_RowId=@e OR Codigo_Operador=@op)))
 THROW 51000, ''Ya existe el vinculo, empleado o codigo activo. No se sobrescribe.'', 1;
SELECT @u AS Usuario_Id,@e AS Empleado_RowId,@op AS Codigo_Operador,@centro AS Centro_Dist,@aplicar AS Aplicar;
IF @aplicar=1
BEGIN
 INSERT dbo.PilotoVinculo(Usuario_Id,Empleado_RowId,Codigo_Operador,Activo) VALUES(@u,@e,@op,1);
 INSERT dbo.PilotoCentro(Usuario_Id,Centro_Dist) VALUES(@u,@centro);
END;';
SET LOCK_TIMEOUT 3000;
SET XACT_ABORT ON;
BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;
    EXEC sys.sp_executesql @Sql,N'@u bigint,@e int,@op nvarchar(15),@centro nvarchar(15),@aplicar bit',@UsuarioId,@EmpleadoRowId,@Operador,@Centro,@Aplicar;
    COMMIT;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    SET XACT_ABORT OFF;
    SET LOCK_TIMEOUT -1;
    THROW;
END CATCH;
