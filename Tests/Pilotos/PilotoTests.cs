using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Controllers;

internal static class PilotoTests
{
    static int comprobaciones;
    static void Check(bool ok, string name) { comprobaciones++; if(!ok) throw new Exception(name); }
    static void Rechaza(Action test, int status, string name) {
        try { test(); } catch(PilotoException e) { Check(e.StatusCode==status,name); return; }
        throw new Exception("No se rechazo: "+name);
    }
    static PilotoRuta Ruta() {
        var r=new PilotoRuta { Id="DEMO-1", Estado="E", Placa="DEMO", Piloto="PILOTO DEMO", Centro="DEMO", Fecha=new DateTime(2026,1,1), PuedeCerrar=true };
        r.Documentos.Add(new PilotoDocumento {RowId=1,Tipo="FACTURA",Documento="D1",Bultos=2});
        r.Documentos.Add(new PilotoDocumento {RowId=2,Tipo="ENVIO",Documento="D2",Bultos=1});
        r.Version=PilotoReglas.Version(r); return r;
    }
    static PilotoCierre Cierre(PilotoRuta r) {
        return new PilotoCierre {RutaId=r.Id,Version=r.Version,Solicitud=Guid.NewGuid(),Documentos=new List<PilotoResultado> {
            new PilotoResultado {RowId=1,Visito=true,Entrega="ENTREGADO"},
            new PilotoResultado {RowId=2,Visito=false,Entrega="NO ENTREGADO",Motivo="Destino cerrado"}
        }};
    }
    public static int Main() {
        try {
            var r=Ruta(); var c=Cierre(r);
            PilotoReglas.ValidarCierre(r,c); Check(c.Documentos[1].Entrega=="NO ENTREGADO","preserva fallo al cerrar");
            c.Documentos[1].Entrega="INCIDENCIA"; PilotoReglas.ValidarCierre(r,c); Check(true,"incidencia permite cierre con motivo");
            foreach(var estado in new[]{"A","C","X",null,"Z"}) {
                r=Ruta(); c=Cierre(r); r.Estado=estado; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),409,"estado "+estado);
            }
            r=Ruta(); c=Cierre(r); c.Version="obsoleta"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),409,"version obsoleta");
            c=Cierre(r); c.RutaId="OTRA"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"ruta ajena");
            c=Cierre(r); c.Solicitud=Guid.Empty; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"solicitud vacia");
            c=Cierre(r); c.Documentos.RemoveAt(1); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"documento omitido");
            c=Cierre(r); c.Documentos[1].RowId=1; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"documento duplicado");
            c=Cierre(r); c.Documentos[1].RowId=999; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"documento ajeno");
            c=Cierre(r); c.Documentos[1].Motivo=" "; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"fallo sin motivo");
            c=Cierre(r); c.Documentos[0].Visito=false; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"entregado sin visita");
            c=Cierre(r); c.Documentos[0].Visito=null; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"visita omitida");
            c=Cierre(r); c.Documentos[0].Entrega="X"; Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"resultado no permitido");
            c=Cierre(r); c.Documentos[1].Motivo=new string('x',151); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"motivo largo");
            var version=r.Version; r.Placa="OTRO"; Check(PilotoReglas.Version(r)!=version,"cambio de vehiculo invalida formulario");
            r=Ruta(); r.Documentos[0].Observaciones="Cambio en oficina"; Check(PilotoReglas.Version(r)!=version,"observaciones invalida formulario");
            r=Ruta(); r.Documentos[0].Entrega="INCIDENCIA"; Check(PilotoReglas.Version(r)!=version,"monitoreo invalida formulario");
            r=Ruta(); c=Cierre(r); var huella=PilotoReglas.HuellaSolicitud(c);
            c.Documentos.Reverse(); Check(PilotoReglas.HuellaSolicitud(c)==huella,"reordenar no cambia idempotencia");
            c.Documentos[0].Motivo="Otro"; Check(PilotoReglas.HuellaSolicitud(c)!=huella,"reintento diferente no coincide");
            Rechaza(()=>PilotoReglas.HuellaSolicitud(null),400,"null request");
            c.Documentos=Enumerable.Range(1,201).Select(i=>new PilotoResultado { RowId=i }).ToList();
            Rechaza(()=>PilotoReglas.HuellaSolicitud(c),400,"limite de formulario");
            r=Ruta(); r.Documentos.Clear(); c=Cierre(r); c.Documentos.Clear(); Rechaza(()=>PilotoReglas.ValidarCierre(r,c),400,"ruta vacia");
            var now=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
            var d=new PilotoDesafio("DEMO",now); Check(d.Codigo.Length==6,"codigo de seis digitos");
            Check(d.Verificar(d.Codigo,now),"codigo valido"); Check(!d.Verificar(d.Codigo,now),"codigo de un solo uso");
            d=new PilotoDesafio("DEMO",now); Check(!d.Verificar(d.Codigo,now.AddMinutes(5)),"vence a los cinco minutos");
            d=new PilotoDesafio("DEMO",now); for(int i=0;i<5;i++) Check(!d.Verificar("incorrecto",now),"intento incorrecto");
            Check(!d.Verificar(d.Codigo,now),"bloqueo tras cinco intentos");
            var posted=new NameValueCollection { {"RutaId","DEMO-1"},{"Solicitud",Guid.NewGuid().ToString()}, {"Version",version},
                {"Documentos[0].RowId","1"},{"Documentos[0].Visito","true"},{"Documentos[0].Entrega","ENTREGADO"},
                {"Documentos[1].RowId","2"},{"Documentos[1].Visito","false"},{"Documentos[1].Entrega","NO ENTREGADO"},{"Documentos[1].Motivo","Ausente"},
                {"Login","OTRO"},{"Piloto","OTRO"},{"Estado","X"} };
            var binding=new ModelBindingContext {ModelMetadata=ModelMetadataProviders.Current.GetMetadataForType(null,typeof(PilotoCierre)),ModelName="",ValueProvider=new NameValueCollectionValueProvider(posted,CultureInfo.InvariantCulture)};
            var model=(PilotoCierre)new DefaultModelBinder().BindModel(new ControllerContext(),binding);
            Check(binding.ModelState.IsValid && model.Documentos.Count==2 && model.Documentos[1].Visito==false,"contrato real MVC preserva false");
            PilotoReglas.ValidarCierre(Ruta(),model);
            Check(typeof(PilotoCierre).GetProperty("Login")==null && typeof(PilotoCierre).GetProperty("Estado")==null,"identidad y estado no se enlazan");
            var action=typeof(PilotoController).GetMethod("Completar");
            Check(action.IsDefined(typeof(HttpPostAttribute),true) && action.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true),"cierre POST y CSRF");
            Check(typeof(PilotoController).IsDefined(typeof(AuthorizeAttribute),true),"controlador autenticado");
            Check(typeof(PilotoController).GetMethod("Anular")==null,"sin accion anular");
            Console.WriteLine("OK: "+comprobaciones+" comprobaciones de reglas, concurrencia, formulario y desafio."); return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
