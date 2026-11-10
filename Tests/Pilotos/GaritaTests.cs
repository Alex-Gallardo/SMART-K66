using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Controllers;
using DiamDev.Give.DAL;

internal static class GaritaTests
{
    private static int checks;
    private static void Check(bool ok,string mensaje) {checks++;if(!ok) throw new Exception(mensaje);}
    private static void Rechaza(Action a,int codigo,string mensaje)
    {try{a();}catch(PilotoException e){Check(e.StatusCode==codigo,mensaje);return;}throw new Exception("No rechazó: "+mensaje);}
    private static GaritaRuta Ruta()
    {
        var r=new GaritaRuta{Id="PP26-1",Fecha=new DateTime(2026,10,20),Piloto="JUAN",Placa="P-123",Centro="PALIN",Estado="E",VehiculoDisponible=true};
        r.Documentos.Add(new PilotoDocumento{RowId=1,Tipo="FACTURA",Documento="F1",Cliente="CLIENTE",Bultos=2});
        r.Documentos.Add(new PilotoDocumento{RowId=2,Tipo="FACTURA",Documento="F2",Cliente="CLIENTE",Bultos=3});
        r.Version=GaritaReglas.Version(r);return r;
    }
    public static int Run()
    {
        var r=Ruta();
        Check(r.PuedeRegistrarSalida && r.PuedeReportarDiferencia && r.Clientes==1 && r.Bultos==5,"ruta en curso con carga y cliente");
        Check(GaritaReglas.EstadoCliente(r.Documentos)=="pending","cliente sin resultados conserva borde neutral");
        r.Documentos[0].Entrega="ENTREGADO";r.Documentos[1].Entrega="ENTREGADO";
        Check(GaritaReglas.EstadoCliente(r.Documentos)=="delivered","todas las facturas entregadas marcan el cliente en verde");
        r.Documentos[0].Entrega=" entregado ";Check(GaritaReglas.EstadoCliente(r.Documentos)=="delivered","estado histórico con espacios o minúsculas mantiene el color correcto");r.Documentos[0].Entrega="ENTREGADO";
        r.Documentos[1].Entrega="NO ENTREGADO";
        Check(GaritaReglas.EstadoCliente(r.Documentos)=="warning","una factura no entregada marca el cliente en naranja");
        r.Documentos[1].Entrega="INCIDENCIA";
        Check(GaritaReglas.EstadoCliente(r.Documentos)=="warning","una incidencia marca el cliente en naranja");
        r.Documentos[0].Entrega=null;r.Documentos[1].Entrega=null;
        var xml="<documentos><d id=\"1\"><visito>true</visito><entrega>ENTREGADO</entrega><motivo/><observaciones>Foto en recepción</observaciones></d><d id=\"2\"><visito>false</visito><entrega>NO ENTREGADO</entrega><motivo>TIEMPO RUTA</motivo><observaciones>No hubo tiempo</observaciones></d></documentos>";
        Check(GaritaReglas.AplicarResultadoTemporal(r.Documentos,xml) && GaritaReglas.Resultado(r.Documentos[0])=="ENTREGADO" && GaritaReglas.Resultado(r.Documentos[1])=="NO ENTREGADO" && r.Documentos[1].Entrega==null,"resultado POS se muestra provisional sin alterar APK66");
        Check(GaritaReglas.EstadoCliente(r.Documentos)=="warning" && r.Documentos[1].ObservacionPiloto=="No hubo tiempo","resultado POS afecta solo la presentación del cliente");
        var borradorInvalido=Ruta();
        Check(!GaritaReglas.AplicarResultadoTemporal(borradorInvalido.Documentos,xml.Replace("id=\"2\"","id=\"3\"")) && borradorInvalido.Documentos.All(d=>d.ResultadoPiloto==null),"borrador con otro documento se ignora completo");
        Check(!GaritaReglas.AplicarResultadoTemporal(borradorInvalido.Documentos,"<mal") && borradorInvalido.Documentos.All(d=>d.ResultadoPiloto==null),"borrador corrupto no rompe la consulta");
        borradorInvalido.Documentos[0].Entrega="ENTREGADO";
        Check(!GaritaReglas.AplicarResultadoTemporal(borradorInvalido.Documentos,xml),"APK66 definitivo no se sobreescribe con POS");
        var c=new GaritaRegistroSalida{RutaId=r.Id,Version=r.Version,Solicitud=Guid.NewGuid(),CargaVerificada=true};
        GaritaReglas.ValidarSalida(c,r);Check(c.Observacion=="","observación opcional normalizada");
        c.CargaVerificada=false;Rechaza(()=>GaritaReglas.ValidarSalida(c,r),400,"verificación de carga requerida");c.CargaVerificada=true;
        c.FotoNombre="salida.jpg";c.FotoContenido=new byte[]{0xff,0xd8,0xff,0x01};GaritaReglas.ValidarSalida(c,r);Check(c.FotoContenido.Length==4,"foto opcional válida");
        c.FotoNombre="salida.exe";Rechaza(()=>GaritaReglas.ValidarSalida(c,r),400,"foto con extensión inválida");c.FotoContenido=null;c.FotoNombre=null;
        c.Version="vieja";Rechaza(()=>GaritaReglas.ValidarSalida(c,r),409,"versión vieja");c.Version=r.Version;
        r.Documentos[0].Bultos=6;Check(GaritaReglas.Version(r)!=c.Version,"cambio de bultos invalida confirmación");r.Documentos[0].Bultos=2;
        r.Placa="OTRA";Check(GaritaReglas.Version(r)!=c.Version,"cambio de placa invalida confirmación");r.Placa="P-123";
        r.VehiculoDisponible=false;Check(GaritaReglas.Version(r)!=c.Version && !r.PuedeRegistrarSalida && r.PuedeReportarDiferencia,"vehículo inactivo impide registrar pero permite reportar");r.VehiculoDisponible=true;
        r.Diferencia="Faltan bultos";Check(!r.PuedeRegistrarSalida && !r.PuedeReportarDiferencia,"diferencia bloquea salida");Rechaza(()=>GaritaReglas.ValidarSalida(c,r),409,"diferencia pendiente");r.Diferencia=null;
        r.Estado="A";r.Version=GaritaReglas.Version(r);c.Version=r.Version;
        Check(r.PuedeRegistrarSalida && r.PuedeReportarDiferencia,"ruta abierta permite verificar y registrar salida");GaritaReglas.ValidarSalida(c,r);
        foreach(var estado in new[]{"B","C","L","X"}) {r.Estado=estado;Check(!r.PuedeRegistrarSalida && !r.PuedeReportarDiferencia,"estado "+estado+" no sale");Rechaza(()=>GaritaReglas.ValidarSalida(c,r),409,"estado "+estado+" rechazado");}
        r.Estado="E";r.Version=GaritaReglas.Version(r);c.Version=r.Version;
        r.SalidaUtc=DateTime.UtcNow;Check(!r.PuedeRegistrarSalida && !r.PuedeReportarDiferencia,"salida no se registra dos veces");r.SalidaUtc=null;
        r.HoraSalidaApk=new TimeSpan(12,30,0);Check(!r.PuedeRegistrarSalida && !r.PuedeReportarDiferencia,"salida previa en APK66 no se duplica");Check(GaritaReglas.Version(r)!=c.Version,"hora APK66 invalida versión");r.HoraSalidaApk=null;
        r.Liquidada=true;Check(!r.PuedeRegistrarSalida,"liquidada no sale");r.Liquidada=false;
        r.Documentos[0].Visito=true;Check(!r.PuedeRegistrarSalida,"documento trabajado no sale");r.Documentos[0].Visito=null;
        r.Documentos.Clear();Check(!r.PuedeRegistrarSalida && r.PuedeReportarDiferencia,"sin documentos se reporta diferencia");
        var regreso=Ruta();regreso.Estado="C";regreso.Version=GaritaReglas.Version(regreso);
        var entrada=new GaritaRegistroEntrada{RutaId=regreso.Id,PlacaBusqueda=regreso.Placa,Version=regreso.Version,Solicitud=Guid.NewGuid()};
        Check(regreso.PuedeRegistrarEntrada && !regreso.PuedeRegistrarSalida,"cerrada sin liquidar permite entrada sin cambiar estado");GaritaReglas.ValidarEntrada(entrada,regreso);
        Check(entrada.Observacion=="","entrada sin observación ni foto es válida");
        entrada.Observacion="Regresó\ncon carga";entrada.FotoNombre="regreso.png";entrada.FotoContenido=new byte[]{137,80,78,71,13,10,26,10,1};
        GaritaReglas.ValidarEntrada(entrada,regreso);Check(entrada.Observacion=="Regresó con carga","observación de entrada normalizada y foto válida");
        entrada.FotoNombre="regreso.exe";Rechaza(()=>GaritaReglas.ValidarEntrada(entrada,regreso),400,"entrada rechaza tipo de foto inválido");
        entrada.FotoNombre="regreso.png";entrada.FotoContenido=null;entrada.Observacion=new string('x',251);
        Rechaza(()=>GaritaReglas.ValidarEntrada(entrada,regreso),400,"entrada rechaza observación larga");entrada.Observacion=null;
        entrada.Version="vieja";Rechaza(()=>GaritaReglas.ValidarEntrada(entrada,regreso),409,"entrada rechaza versión antigua");entrada.Version=regreso.Version;
        regreso.EntradaUtc=new DateTime(2026,10,20,23,15,0,DateTimeKind.Utc);Check(regreso.PuedeRegistrarEntrada,"evento POS previo permite completar hora faltante en APK66");GaritaReglas.ValidarEntrada(entrada,regreso);regreso.EntradaUtc=null;
        regreso.HoraEntradaApk=new TimeSpan(16,40,0);Check(!regreso.PuedeRegistrarEntrada && GaritaReglas.Version(regreso)!=entrada.Version,"entrada en APK66 impide duplicado");Rechaza(()=>GaritaReglas.ValidarEntrada(entrada,regreso),409,"no repetir entrada");regreso.HoraEntradaApk=null;
        regreso.Liquidada=true;Check(!regreso.PuedeRegistrarEntrada,"liquidada excluida del regreso");regreso.Liquidada=false;
        regreso.Estado="E";Check(!regreso.PuedeRegistrarEntrada,"en ruta no permite registrar entrada");
        var hoyLocal=new DateTime(2026,10,29);
        var desde=GaritaReglas.CerradasDesde(hoyLocal);var hasta=GaritaReglas.CerradasAntesDe(hoyLocal);
        Check(desde==new DateTime(2026,10,26) && hasta==new DateTime(2026,10,30),"listado C abarca hoy y tres días anteriores");
        Check(new DateTime(2026,10,26)>=desde && new DateTime(2026,10,29,23,59,59)<hasta,"días 0 a 3 incluidos");
        Check(new DateTime(2026,10,25,23,59,59)<desde && new DateTime(2026,10,30)>=hasta,"día 4 y fechas futuras fuera del listado");
        regreso.Estado="C";regreso.Fecha=new DateTime(2026,10,20);regreso.Version=GaritaReglas.Version(regreso);entrada.Version=regreso.Version;
        Check(regreso.PuedeRegistrarEntrada,"ruta C antigua admite entrada por enlace directo");GaritaReglas.ValidarEntrada(entrada,regreso);
        Rechaza(()=>GaritaReglas.Id(" "),400,"id vacío");Rechaza(()=>GaritaReglas.Id(new string('A',16)),400,"id largo");
        Check(GaritaReglas.Placa(" c-123abc ")=="C-123ABC","placa normalizada");
        Rechaza(()=>GaritaReglas.Placa(" "),400,"placa vacía");
        Rechaza(()=>GaritaReglas.Placa(new string('A',16)),400,"placa larga");
        Check(GaritaReglas.Limpiar("a\nb",250)=="a b","nota multilinea normalizada");
        Rechaza(()=>GaritaReglas.Limpiar("a\tb",250),400,"nota con controles");
        Check(GaritaReglas.Ver!=GaritaReglas.RegistrarSalida && GaritaReglas.RegistrarSalida!=GaritaReglas.Administrar,"permisos separados");
        var roles=UsuarioRolPlan.Crear(new[]{1,2},new[]{1,2,3,3});
        Check(roles.Agregar.SequenceEqual(new[]{3}) && roles.Quitar.Length==0,"agregar GARITA conserva los roles existentes");
        roles=UsuarioRolPlan.Crear(new[]{1,2,3},new[]{2,3});
        Check(roles.Agregar.Length==0 && roles.Quitar.SequenceEqual(new[]{1}),"quitar solo el rol desmarcado");
        roles=UsuarioRolPlan.Crear(new[]{1,2},new[]{1,2});
        Check(roles.Agregar.Length==0 && roles.Quitar.Length==0,"guardar roles iguales no reinsertará claves existentes");
        foreach(var method in new[]{"RegistrarSalida","RegistrarEntrada","Diferencia","Centro","ResolverDiferencia"})
        {var m=typeof(GaritaController).GetMethods().Single(x=>x.Name==method);Check(m.IsDefined(typeof(HttpPostAttribute),true)&&m.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"POST y anti CSRF "+method);}
        Check(typeof(GaritaController).IsDefined(typeof(AuthorizeAttribute),true),"controlador autenticado");
        Check(typeof(GaritaController).GetMethod("FotoSalida").IsDefined(typeof(HttpGetAttribute),true),"foto protegida por controlador autenticado");
        Check(typeof(GaritaController).GetMethod("FotoEntrada").IsDefined(typeof(HttpGetAttribute),true),"foto de entrada protegida por controlador autenticado");
        Check(typeof(GaritaController).GetMethod("FotoPiloto").IsDefined(typeof(HttpGetAttribute),true),"foto del piloto consultable solo por controlador autenticado");
        Check(typeof(PilotoController).GetMethod("PanelFotoSalida").IsDefined(typeof(HttpGetAttribute),true),"foto de salida consultable en panel administrativo");
        Check(typeof(PilotoController).GetMethod("PanelFotoEntrada").IsDefined(typeof(HttpGetAttribute),true),"foto de entrada consultable en panel administrativo");
        Console.WriteLine("OK garita: "+checks+" comprobaciones");return 0;
    }
    public static int Sql()
    {
        var sql=GaritaRutaDA.SqlRegistrarSalida;
        Check(sql.Contains("INSERT dbo.GaritaRutaEvento") && sql.Contains("OUTPUT INSERTED.Id") && sql.Contains("@solicitud") && sql.Contains("N'S'"),"Garita audita salida en POS");
        var salidaSql=GaritaRutaDA.SqlActualizarHoraSalida("[TEST_RUTAS]");
        Check(salidaSql.Contains("SET HR_SALIDA=CONVERT(time(0)") && salidaSql.Contains("STATUS=CASE WHEN STATUS=N'A' THEN N'E' ELSE STATUS END") && salidaSql.Contains("STATUS IN(N'A',N'E')") && salidaSql.Contains("HR_SALIDA IS NULL") && salidaSql.Contains("LIQUIDADO"),"salida cambia A a E y conserva E junto a la hora en APK66");
        var entradaSql=GaritaRutaDA.SqlActualizarHoraEntrada("[TEST_RUTAS]");
        Check(entradaSql.Contains("SET HR_ENTRADA=CONVERT(time(0)") && entradaSql.Contains("STATUS=N'C'") && entradaSql.Contains("LIQUIDADO") && !entradaSql.Contains("HR_SALIDA"),"entrada actualiza HR_ENTRADA sin tocar HR_SALIDA en APK66");
        Check(GaritaRutaDA.SqlRegistrarEntrada.Contains("Tipo,Solicitud") && GaritaRutaDA.SqlRegistrarEntrada.Contains("N'I'") && GaritaRutaDA.SqlRegistrarEntrada.Contains("Observacion") && GaritaRutaDA.SqlRegistrarEntrada.Contains("OUTPUT INSERTED.Id"),"Garita audita entrada y obtiene vínculo de foto en POS");
        var lista=GaritaRutaDA.SqlBuscarPorPlaca("[TEST_RUTAS]");
        Check(lista.Contains("r.PLACA=@placa AND r.STATUS=N'E'") && lista.Contains("GaritaCentro") && lista.Contains("FETCH NEXT 50 ROWS ONLY"),"consulta de placa paginada y limitada al centro");
        var abiertas=GaritaRutaDA.SqlBuscarPorPlaca("[TEST_RUTAS]",false,true);
        Check(abiertas.Contains("r.PLACA=@placa AND r.STATUS=N'A'") && abiertas.Contains("LIQUIDADO") && abiertas.Contains("GaritaCentro") && abiertas.Contains("FETCH NEXT 50 ROWS ONLY"),"abiertas paginadas por placa y centro, sin liquidar");
        var cerradas=GaritaRutaDA.SqlBuscarPorPlaca("[TEST_RUTAS]",true);
        var conteoCerradas=GaritaRutaDA.SqlContarPorPlaca("[TEST_RUTAS]",true);
        Check(cerradas.Contains("r.STATUS=N'C'") && cerradas.Contains("r.HR_ENTRADA IS NULL") && cerradas.Contains("LIQUIDADO") && cerradas.Contains("FETCH NEXT 50 ROWS ONLY") &&
              conteoCerradas.Contains("r.FECHA_RUTA>=@desdeCerradas AND r.FECHA_RUTA<@hastaCerradas") &&
              cerradas.Contains("r.FECHA_RUTA>=@desdeCerradas AND r.FECHA_RUTA<@hastaCerradas"),"conteo y página C usan el mismo límite de 0 a 3 días");
        Check(!lista.Contains("@desdeCerradas") && !abiertas.Contains("@desdeCerradas") && !GaritaRutaDA.SqlContarPorPlaca("[TEST_RUTAS]").Contains("@desdeCerradas") && !GaritaRutaDA.SqlContarPorPlaca("[TEST_RUTAS]",false,true).Contains("@desdeCerradas"),"A y E conservan el alcance actual");
        Check(GaritaRutaDA.SqlHoyGuatemala.Contains("SYSUTCDATETIME()") && GaritaRutaDA.SqlHoyGuatemala.Contains("HOUR,-6") && !GaritaRutaDA.SqlActualizarHoraEntrada("[TEST_RUTAS]").Contains("@desdeCerradas"),"hora Guatemala para listado sin limitar la entrada directa");
        Check(lista.Contains("ConductorReal") && lista.Contains("PilotoConductorRuta") && lista.Contains("PilotoEmpresa"),"Garita muestra conductor real únicamente desde la asignación de empresa");
        Console.WriteLine(sql);Console.WriteLine(abiertas);Console.WriteLine(lista);Console.WriteLine(cerradas);Console.WriteLine(conteoCerradas);return 0;
    }
    public static int SqlHours()
    {
        Console.WriteLine(GaritaRutaDA.SqlActualizarHoraSalida("[TEST_RUTAS]"));
        Console.WriteLine(GaritaRutaDA.SqlActualizarHoraEntrada("[TEST_RUTAS]"));
        Console.WriteLine(GaritaRutaDA.SqlRegistrarSalida);
        Console.WriteLine(GaritaRutaDA.SqlRegistrarEntrada);
        return 0;
    }
}
