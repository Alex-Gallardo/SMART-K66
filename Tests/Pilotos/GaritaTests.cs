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
        r.Estado="A";Check(!r.PuedeRegistrarSalida && !r.PuedeReportarDiferencia,"ruta abierta espera a Distribución");Rechaza(()=>GaritaReglas.ValidarSalida(c,r),409,"A no registra salida");r.Estado="E";
        r.SalidaUtc=DateTime.UtcNow;Check(!r.PuedeRegistrarSalida && !r.PuedeReportarDiferencia,"salida no se registra dos veces");r.SalidaUtc=null;
        r.Liquidada=true;Check(!r.PuedeRegistrarSalida,"liquidada no sale");r.Liquidada=false;
        r.Documentos[0].Visito=true;Check(!r.PuedeRegistrarSalida,"documento trabajado no sale");r.Documentos[0].Visito=null;
        r.Documentos.Clear();Check(!r.PuedeRegistrarSalida && r.PuedeReportarDiferencia,"sin documentos se reporta diferencia");
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
        foreach(var method in new[]{"RegistrarSalida","Diferencia","Centro","ResolverDiferencia"})
        {var m=typeof(GaritaController).GetMethods().Single(x=>x.Name==method);Check(m.IsDefined(typeof(HttpPostAttribute),true)&&m.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"POST y anti CSRF "+method);}
        Check(typeof(GaritaController).IsDefined(typeof(AuthorizeAttribute),true),"controlador autenticado");
        Check(typeof(GaritaController).GetMethod("FotoSalida").IsDefined(typeof(HttpGetAttribute),true),"foto protegida por controlador autenticado");
        Check(typeof(PilotoController).GetMethod("PanelFotoSalida").IsDefined(typeof(HttpGetAttribute),true),"foto de salida consultable en panel administrativo");
        Console.WriteLine("OK garita: "+checks+" comprobaciones");return 0;
    }
    public static int Sql()
    {
        var sql=GaritaRutaDA.SqlRegistrarSalida;
        Check(sql.Contains("INSERT dbo.GaritaRutaEvento") && sql.Contains("OUTPUT INSERTED.Id") && sql.Contains("@solicitud") && sql.Contains("N'S'") && !sql.Contains("RT_RUTAS"),"Garita registra evento sin cambiar estado de ruta");
        var lista=GaritaRutaDA.SqlBuscarPorPlaca("[TEST_RUTAS]");
        Check(lista.Contains("r.PLACA=@placa AND r.STATUS=N'E'") && lista.Contains("GaritaCentro") && lista.Contains("FETCH NEXT 50 ROWS ONLY"),"consulta de placa paginada y limitada al centro");
        Console.WriteLine(sql);Console.WriteLine(lista);return 0;
    }
}
