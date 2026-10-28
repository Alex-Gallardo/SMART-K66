using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web.Mvc;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Controllers;
using OfficeOpenXml;

internal static class LiquidacionTests
{
    private static int checks;
    private static void Check(bool ok,string nombre) {checks++;if(!ok) throw new Exception("Liquidación: "+nombre);}
    private static void Rechaza(Action accion,int codigo,string nombre)
    {try{accion();throw new Exception("Liquidación permitió "+nombre);}catch(PilotoException e){Check(e.StatusCode==codigo,nombre);}}
    private static PilotoLiquidacionDA Dao()
    {var da=(PilotoLiquidacionDA)FormatterServices.GetUninitializedObject(typeof(PilotoLiquidacionDA));typeof(PilotoLiquidacionDA).GetField("apk",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(da,"[APK_TEST]");return da;}
    public static int Sql()
    {
        var da=Dao();Console.WriteLine(da.SqlContar);Console.WriteLine(da.SqlListar);Console.WriteLine(da.SqlLiquidarDocumento);Console.WriteLine(da.SqlLiquidarTodos);Console.WriteLine(da.SqlLiquidarRuta);return 0;
    }
    public static int Run()
    {
        var hoy=new DateTime(2026,10,28);
        var filtro=new PilotoLiquidacionFiltro();filtro.Validar(hoy);
        Check(filtro.Pagina==1 && filtro.Desde=="2026-09-28" && filtro.Hasta=="2026-10-28" && filtro.FechaFin==hoy.AddDays(1),"filtro inicial");
        Rechaza(()=>new PilotoLiquidacionFiltro{Desde="2025-01-01",Hasta="2026-10-28"}.Validar(hoy),400,"período excesivo");
        Rechaza(()=>new PilotoLiquidacionFiltro{Pagina=-1}.Validar(hoy),400,"página negativa");
        Rechaza(()=>PilotoLiquidacionReglas.Id(""),400,"ruta vacía");
        Rechaza(()=>PilotoLiquidacionReglas.Rutas(new string[0]),400,"Excel vacío");
        Check(PilotoLiquidacionReglas.Rutas(new[]{"C-1","C-1","C-2"}).Length==2,"selección única");
        var r=new PilotoLiquidacionRuta{Id="R1",Estado="C",TotalDocumentos=2,DocumentosConResultado=1,HoraEntrada=new TimeSpan(14,0,0)};
        Check(r.PuedeLiquidarDocumentos && !r.PuedeLiquidarRuta,"liquidación individual antes de completar resultados");
        r.DocumentosConResultado=2;r.DocumentosLiquidados=1;Check(!r.PuedeLiquidarRuta,"ruta espera todos los documentos");
        r.DocumentosLiquidados=2;Check(r.PuedeLiquidarRuta,"ruta lista después de documentos");
        r.HoraEntrada=null;Check(!r.PuedeLiquidarDocumentos && !r.PuedeLiquidarRuta,"entrada de Garita obligatoria");
        r.HoraEntrada=new TimeSpan(14,0,0);r.Estado="E";Check(!r.PuedeLiquidarRuta,"E no liquidable");
        r.Estado="C";r.Liquidada=true;Check(!r.PuedeLiquidarRuta,"doble liquidación bloqueada");
        var d=new PilotoLiquidacionDocumento{UsuarioLiquida="USR",FechaLiquida=hoy};Check(d.Liquidado && !d.Inconsistente,"documento completo");
        d.FechaLiquida=null;Check(!d.Liquidado && d.Inconsistente,"marca parcial detectada");
        var da=Dao();
        var uno=da.SqlLiquidarDocumento;var todos=da.SqlLiquidarTodos;var ruta=da.SqlLiquidarRuta;
        Check(uno.Contains("U_Usuario_liquida=@actor") && uno.Contains("U_U_FechaLiquidacion=@fecha") && uno.Contains("ROWID=@rowid") && uno.Contains("MO_ENTREGA"),"documento individual con resultado");
        Check(todos.Contains("U_Usuario_liquida=@actor") && todos.Contains("ID_RUTA=@id") && !todos.Contains("ROWID=@rowid"),"lote de documentos pendientes");
        Check(ruta.Contains("STATUS=N'L',LIQUIDADO=1,USR_LIQ=@actor,FECHA_LIQ=@fecha") && ruta.Contains("STATUS=N'C'") && ruta.Contains("HR_ENTRADA IS NOT NULL") && ruta.Contains("NOT EXISTS") && ruta.Contains("MO_ENTREGA"),"ruta solo tras todos los documentos");
        Check(da.SqlListar.Contains("FETCH NEXT 50 ROWS ONLY") && da.SqlListar.Contains("@ver=1") && da.SqlListar.Contains("r.STATUS IN(N'C',N'L')"),"listado paginado incluye liquidadas solo con filtro");
        Check(PilotoLiquidacionReglas.Permiso=="Pilotos.Liquidar","permiso separado");
        var controlador=typeof(LiquidacionController);
        Check(Attribute.IsDefined(controlador,typeof(AuthorizeAttribute)),"controlador exige login");
        foreach(var accion in new[]{"LiquidarDocumento","LiquidarTodos","LiquidarRuta","Exportar"})
            Check(Attribute.IsDefined(controlador.GetMethod(accion),typeof(ValidateAntiForgeryTokenAttribute)),"POST protegido "+accion);
        var reporte=new PilotoLiquidacionReporte();r.Id="R1";r.Piloto="=SUM(1)";r.Fecha=hoy;r.Placa="C-123";r.Centro="PALIN";r.Documentos.Add(new PilotoLiquidacionDocumento{RutaId="R1",RowId=1,Tipo="FACTURA",Numero="F1",Cliente="CLIENTE",Resultado="ENTREGADO",UsuarioLiquida="USR",FechaLiquida=hoy});reporte.Rutas.Add(r);
        var bytes=PilotoLiquidacionExcel.Crear(reporte);
        using(var excel=new ExcelPackage(new MemoryStream(bytes))) {
            Check(excel.Workbook.Worksheets.Count==2 && excel.Workbook.Worksheets["Rutas"]!=null && excel.Workbook.Worksheets["Documentos"]!=null,"dos hojas Excel");
            Check(Convert.ToString(excel.Workbook.Worksheets["Rutas"].Cells[2,6].Value).StartsWith("'="),"protección de fórmula Excel");
            Check(Convert.ToString(excel.Workbook.Worksheets["Documentos"].Cells[2,4].Value)=="F1","documentos seleccionados en Excel");
        }
        Console.WriteLine("OK Liquidación: "+checks+" comprobaciones; sin escribir en SQL.");return 0;
    }
}
