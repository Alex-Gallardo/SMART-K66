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
        var r=new GaritaRuta{Id="PP26-1",Fecha=new DateTime(2026,10,20),Piloto="JUAN",Placa="P-123",Centro="PALIN",Estado="A",VehiculoDisponible=true};
        r.Documentos.Add(new PilotoDocumento{RowId=1,Tipo="FACTURA",Documento="F1",Cliente="CLIENTE",Bultos=2});
        r.Documentos.Add(new PilotoDocumento{RowId=2,Tipo="FACTURA",Documento="F2",Cliente="CLIENTE",Bultos=3});
        r.Version=GaritaReglas.Version(r);return r;
    }
    public static int Run()
    {
        var r=Ruta();
        Check(r.PuedeSalir && r.Clientes==1 && r.Bultos==5,"ruta abierta con carga y cliente");
        var c=new GaritaConfirmacion{RutaId=r.Id,Version=r.Version,Solicitud=Guid.NewGuid(),PilotoVehiculoVerificados=true,CargaVerificada=true};
        GaritaReglas.Validar(c,r);Check(c.Precinto=="" && c.Observacion=="","campos opcionales normalizados");
        c.CargaVerificada=false;Rechaza(()=>GaritaReglas.Validar(c,r),400,"dos verificaciones requeridas");c.CargaVerificada=true;
        c.Version="vieja";Rechaza(()=>GaritaReglas.Validar(c,r),409,"versión vieja");c.Version=r.Version;
        r.Documentos[0].Bultos=6;Check(GaritaReglas.Version(r)!=c.Version,"cambio de bultos invalida confirmación");r.Documentos[0].Bultos=2;
        r.Placa="OTRA";Check(GaritaReglas.Version(r)!=c.Version,"cambio de placa invalida confirmación");r.Placa="P-123";
        r.VehiculoDisponible=false;Check(GaritaReglas.Version(r)!=c.Version,"vehículo desactivado invalida confirmación");r.VehiculoDisponible=true;
        r.Diferencia="Faltan bultos";Check(!r.PuedeSalir,"diferencia bloquea salida");Rechaza(()=>GaritaReglas.Validar(c,r),409,"diferencia no confirmable");r.Diferencia=null;
        r.Estado="E";Check(!r.PuedeSalir,"en ruta no confirma otra vez");r.Estado="A";
        r.Liquidada=true;Check(!r.PuedeSalir,"liquidada no sale");r.Liquidada=false;
        r.Documentos[0].Visito=true;Check(!r.PuedeSalir,"documento trabajado no sale");r.Documentos[0].Visito=null;
        r.Documentos.Clear();Check(!r.PuedeSalir,"sin documentos no sale");
        Rechaza(()=>GaritaReglas.Id(" "),400,"id vacío");Rechaza(()=>GaritaReglas.Id(new string('A',16)),400,"id largo");
        Check(GaritaReglas.Limpiar("a\nb",250)=="a b","nota multilinea normalizada");
        Rechaza(()=>GaritaReglas.Limpiar("a\tb",250),400,"nota con controles");
        Check(GaritaReglas.Ver!=GaritaReglas.Salir && GaritaReglas.Salir!=GaritaReglas.Administrar,"permisos separados");
        var roles=UsuarioRolPlan.Crear(new[]{1,2},new[]{1,2,3,3});
        Check(roles.Agregar.SequenceEqual(new[]{3}) && roles.Quitar.Length==0,"agregar GARITA conserva los roles existentes");
        roles=UsuarioRolPlan.Crear(new[]{1,2,3},new[]{2,3});
        Check(roles.Agregar.Length==0 && roles.Quitar.SequenceEqual(new[]{1}),"quitar solo el rol desmarcado");
        roles=UsuarioRolPlan.Crear(new[]{1,2},new[]{1,2});
        Check(roles.Agregar.Length==0 && roles.Quitar.Length==0,"guardar roles iguales no reinsertará claves existentes");
        foreach(var method in new[]{"Confirmar","Diferencia","Centro","ResolverDiferencia"})
        {var m=typeof(GaritaController).GetMethods().Single(x=>x.Name==method);Check(m.IsDefined(typeof(HttpPostAttribute),true)&&m.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"POST y anti CSRF "+method);}
        Check(typeof(GaritaController).IsDefined(typeof(AuthorizeAttribute),true),"controlador autenticado");
        Console.WriteLine("OK garita: "+checks+" comprobaciones");return 0;
    }
    public static int Sql()
    {
        var da=(GaritaRutaDA)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GaritaRutaDA));
        typeof(GaritaRutaDA).GetField("apk",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(da,"[TEST_RUTAS]");
        var sql=da.SqlConfirmarEstado;
        Check(sql.Contains("STATUS=N'E'") && sql.Contains("STATUS=N'A'") && sql.Contains("ISNULL(LIQUIDADO,0)=0"),"transición A a E con control de liquidación");
        var rutas=da.SqlRutasCentro;
        Check(rutas.Contains("r.STATUS IN(N'A',N'E')") && rutas.Contains("g.Usuario_Id=@usuario") && rutas.Contains("g.Centro_Dist=r.CENTRO_DIST") && rutas.Contains("numero<=25"),"listado A y E limitado a los centros del agente");
        Console.WriteLine(sql);Console.WriteLine(rutas);return 0;
    }
}
