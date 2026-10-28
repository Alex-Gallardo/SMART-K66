using System;
using System.Drawing;
using System.Linq;
using DiamDev.Give.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DiamDev.Give.UI.Controllers
{
    public static class PilotoLiquidacionExcel
    {
        private static string Texto(string valor)
        {
            valor=valor??"";
            return valor.TrimStart().Length>0 && "=+-@".Contains(valor.TrimStart()[0]) ? "'"+valor : valor;
        }
        private static void Encabezado(ExcelWorksheet hoja,string[] titulos)
        {
            for(var i=0;i<titulos.Length;i++) hoja.Cells[1,i+1].Value=titulos[i];
            using(var c=hoja.Cells[1,1,1,titulos.Length]) {
                c.Style.Font.Bold=true;c.Style.Font.Color.SetColor(Color.White);
                c.Style.Fill.PatternType=ExcelFillStyle.Solid;c.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(17,57,82));
            }
            hoja.View.FreezePanes(2,1);
            hoja.Cells[1,1,1,titulos.Length].AutoFilter=true;
        }
        public static byte[] Crear(PilotoLiquidacionReporte reporte)
        {
            if(reporte==null || reporte.Rutas==null || reporte.Rutas.Count==0) throw new PilotoException(400,"Selecciona rutas para el reporte.");
            if(reporte.Rutas.Sum(r=>r.Documentos.Count)>10000) throw new PilotoException(400,"El reporte supera 10.000 documentos. Selecciona menos rutas.");
            using(var paquete=new ExcelPackage()) {
                var rutas=paquete.Workbook.Worksheets.Add("Rutas");
                Encabezado(rutas,new[]{"Ruta","Fecha de ruta","Estado","Liquidada","Placa","Piloto o transporte","Centro","Hora de salida","Hora de entrada","Documentos","Documentos liquidados","Usuario liquidador","Fecha de liquidación"});
                var fila=2;
                foreach(var r in reporte.Rutas) {
                    object[] datos={Texto(r.Id),r.Fecha,r.Estado,r.Liquidada?"Sí":"No",Texto(r.Placa),Texto(r.Piloto),Texto(r.Centro),r.HoraSalida.HasValue?r.HoraSalida.Value.ToString(@"hh\:mm\:ss"):"",r.HoraEntrada.HasValue?r.HoraEntrada.Value.ToString(@"hh\:mm\:ss"):"",r.TotalDocumentos,r.DocumentosLiquidados,Texto(r.UsuarioLiquida),r.FechaLiquida.HasValue?(object)r.FechaLiquida.Value:null};
                    for(var col=0;col<datos.Length;col++) rutas.Cells[fila,col+1].Value=datos[col];
                    fila++;
                }
                rutas.Cells[2,2,fila-1,2].Style.Numberformat.Format="dd/mm/yyyy";
                rutas.Cells[2,13,fila-1,13].Style.Numberformat.Format="dd/mm/yyyy hh:mm";
                rutas.Cells[rutas.Dimension.Address].AutoFitColumns();
                var documentos=paquete.Workbook.Worksheets.Add("Documentos");
                Encabezado(documentos,new[]{"Ruta","ROWID","Tipo","Número","Cliente","Dirección","Bultos","Resultado","Motivo","Observación","Liquidado","Usuario liquidador","Fecha de liquidación"});
                fila=2;
                foreach(var r in reporte.Rutas) foreach(var d in r.Documentos) {
                    object[] datos={Texto(r.Id),d.RowId,Texto(d.Tipo),Texto(d.Numero),Texto(d.Cliente),Texto(d.Direccion),d.Bultos,Texto(d.Resultado),Texto(d.Motivo),Texto(d.Observacion),d.Liquidado?"Sí":"No",Texto(d.UsuarioLiquida),d.FechaLiquida.HasValue?(object)d.FechaLiquida.Value:null};
                    for(var col=0;col<datos.Length;col++) documentos.Cells[fila,col+1].Value=datos[col];
                    fila++;
                }
                if(fila>2) {documentos.Cells[2,7,fila-1,7].Style.Numberformat.Format="#,##0.###";documentos.Cells[2,13,fila-1,13].Style.Numberformat.Format="dd/mm/yyyy hh:mm";}
                documentos.Cells[documentos.Dimension.Address].AutoFitColumns();
                return paquete.GetAsByteArray();
            }
        }
    }
}
