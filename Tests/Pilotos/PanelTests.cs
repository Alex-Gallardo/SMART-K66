using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using DiamDev.Give.Entities;
using DiamDev.Give.DAL;
using DiamDev.Give.UI.Controllers;

internal static class PanelTests
{
    static int n;
    static void Check(bool valor,string mensaje){n++;if(!valor) throw new Exception(mensaje);}
    static void Rechaza(Action accion,int status){try{accion();}catch(PilotoException e){Check(e.StatusCode==status,"Estado de rechazo");return;}throw new Exception("Faltó rechazar la operación");}
    public static int Run()
    {
        var hoy=new DateTime(2026,9,29);
        var f=new PilotoPanelFiltro();f.Validar(hoy);Check(f.Desde=="2026-09-29" && f.Liquidacion=="no","Inicio sin liquidadas");
        new PilotoPanelFiltro{Desde="2026-09-01",Hasta="2026-10-01",Liquidacion="todas"}.Validar(hoy);
        Rechaza(()=>new PilotoPanelFiltro{Desde="2026-09-01",Hasta="2026-10-02"}.Validar(hoy),400);
        Rechaza(()=>new PilotoPanelFiltro{Desde="2026-09-30",Hasta="2026-09-29"}.Validar(hoy),400);
        Rechaza(()=>new PilotoPanelFiltro{Estado="E'; DROP"}.Validar(hoy),400);
        Rechaza(()=>new PilotoPanelFiltro{Pagina=0}.Validar(hoy),400);
        Rechaza(()=>new PilotoPanelFiltro{Usuario=-1}.Validar(hoy),400);
        Rechaza(()=>PilotoPanelReglas.ExigirAcceso(true,false),403);
        Rechaza(()=>PilotoPanelReglas.ExigirAcceso(false,true),403);
        PilotoPanelReglas.ExigirAcceso(true,true);
        Check(PilotoPanelReglas.Permiso!="Pilotos.Administrar","Permisos separados");
        foreach(var metodo in new[]{"Panel","PanelDetalle","PanelImagen","PanelExportar"}) {
            var action=typeof(PilotoController).GetMethod(metodo);
            Check(action.GetCustomAttributes(typeof(HttpGetAttribute),false).Length==1,"Panel solo consulta: "+metodo);
        }
        var dao=(PilotoPanelDA)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PilotoPanelDA));
        typeof(PilotoPanelDA).GetField("apk",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(dao,"[TEST_RUTAS]");
        Check(dao.SqlCrearRutas.Contains("INTO #PanelRutas") && !dao.SqlCrearRutas.Contains("@"),"La tabla temporal se crea fuera del comando parametrizado");
        Check(dao.SqlRutas.StartsWith("INSERT #PanelRutas") && !dao.SqlRutas.Contains("INTO #PanelRutas"),"La consulta parametrizada solo inserta en la tabla existente");
        var p=new PilotoPanelPiloto{Id=10,Login="piloto",Empleado="JULIO",Operador="piloto",CuentaActiva=true,RolPiloto=true,PermisoVer=true,PermisoConfirmar=true,VinculoActivo=true,EmpleadoActivo=true,EmpleadoUnico=true};p.Centros.Add("PC");p.PlacasDisponibles.Add("ABC");p.Placas.Add("ABC");
        var datos=new PilotoRuta{Id="RUTA",Estado="E",Piloto="JULIO",Centro="PC",Placa="ABC",Fecha=hoy};
        datos.Documentos.Add(new PilotoDocumento{RowId=1,Documento="F1",Cliente="CLIENTE",Direccion="DIR"});
        datos.Version=PilotoReglas.Version(datos);
        var ruta=new PilotoPanelRuta{Datos=datos};ruta.Pilotos.Add(p);ruta.Clientes.Add(new PilotoPanelCliente{Primero=1,Documentos=new List<PilotoPanelDocumento>{new PilotoPanelDocumento{Datos=datos.Documentos[0],Fuente="APK66"}}});
        var xml="<documentos><d id=\"1\"><visito>true</visito><entrega>ENTREGADO</entrega><motivo/><observaciones/></d></documentos>";
        Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,xml,true,hoy),"Sin catálogo verificado no suma avance, aunque coincida la versión de una copia");
        Check(!PilotoPanelReglas.AplicarBorrador(ruta,11,1,datos.Version,xml,true,hoy,true),"Otro usuario no suma avance");
        Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,"vieja",xml,true,hoy,true),"Versión incompatible no suma avance");
        Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,xml.Replace("id=\"1\"","id=\"2\""),true,hoy,true),"Otro documento no suma avance");
        Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,"<mal",true,hoy,true),"XML corrupto no rompe el panel");
        Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,xml.Replace("true","false"),true,hoy,true),"Entrega sin visita no suma avance");
        ruta.Liquidada=true;Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,xml,true,hoy,true),"Liquidada no recupera borradores");ruta.Liquidada=false;
        ruta.Pilotos.Add(p);Check(!PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,xml,true,hoy,true),"Asignación ambigua no suma avance");ruta.Pilotos.RemoveAt(1);
        Check(PilotoPanelReglas.AplicarBorrador(ruta,10,1,datos.Version,xml,true,hoy,true),"Borrador vigente suma avance");
        Check(ruta.ListaParaCierre && ruta.Porcentaje==100 && ruta.Documentos.First().Fuente.StartsWith("POS"),"Avance POS separado de definitivo");
        f.Avance="listo";f.Resultado="ENTREGADO";Check(PilotoPanelReglas.Coincide(ruta,f),"Filtros de avance y resultado");f.Resultado="INCIDENCIA";Check(!PilotoPanelReglas.Coincide(ruta,f),"Incidencia filtra documentos");
        Check(PilotoPanelReglas.Csv("=1+1")=="\"'=1+1\"","CSV protege fórmulas");Check(PilotoPanelReglas.Csv(" \t@SUM(1)").StartsWith("\"'"),"CSV protege espacios iniciales");Check(PilotoPanelReglas.Csv("a\"b")=="\"a\"\"b\"","CSV codifica comillas");
        Check(PilotoPanelReglas.HoraGuatemala(new DateTime(2026,9,30,1,0,0)).Date==hoy,"UTC al día anterior en Guatemala");
        p.VinculoActivo=false;p.Revisar();Check(p.Alertas.Count>0 && !p.Coincide(datos),"Vínculo inactivo alertado");
        Console.WriteLine("OK Panel: "+n+" comprobaciones; sin SQL.");return 0;
    }
    public static int Sql()
    {
        var tipo=typeof(PilotoPanelDA);var da=(PilotoPanelDA)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(tipo);
        tipo.GetField("apk",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(da,"[TEST_RUTAS]");
        foreach(var nombre in new[]{"SqlCrearRutas","SqlRutas","SqlDocumentos","SqlPilotos","SqlAsignaciones"}) Console.WriteLine(tipo.GetProperty(nombre,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null));
        Console.WriteLine(PilotoPanelDA.SqlInstalacion);
        Console.WriteLine(tipo.GetProperty("SqlArtefactos",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null));
        Console.WriteLine(tipo.GetProperty("SqlActividad",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null));
        tipo.GetField("eventosClientes",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(da,true);
        Console.WriteLine(tipo.GetProperty("SqlArtefactos",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null));
        Console.WriteLine(tipo.GetProperty("SqlActividad",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(da,null));return 0;
    }
}
