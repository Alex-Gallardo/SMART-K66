using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using PagedList;
using System.Collections;
using System.Data;
using Microsoft.Reporting.WebForms;
using DiamDev.Give.DAL;
using System.Data.Entity;
using OfficeOpenXml;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class Producto_Ingreso_x_IDController : Controller
    {
        #region Metodos Privados

        private void CargaControles()
        {
            var Categorias = new MovimientoCategoriaBL().ObtenerListado(true);
            var Agencias = new AgenciaBL().ObtenerListado(false, CustomHelper.getUserId());
            var Proveedores = new ProveedorBL().ObtenerListado(false);
            var Estados = new MovimientoEstadoBL().ObtenerListado();

            ViewBag.Categorias = new SelectList(Categorias, "MovimientoCategoriaId", "Nombre");
            ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
            ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
            ViewBag.Estados = new SelectList(Estados, "MovimientoEstadoId", "Nombre");
        }

        private byte[] GetReportBytes(string reportPath, DataSet reportDataSource, decimal pageWidth = 13.38m, decimal pageHeight = 8.5m, decimal MarginLeft = 1m, decimal MarginRight = 1m)
        {

            byte[] reportBytes = null;

            // Se crea la instancia del reporte y se cargan sus datos.
            LocalReport reporte = new LocalReport() { ReportPath = reportPath };
            reporte.DataSources.Add(new ReportDataSource("MovimientoEncabezado", reportDataSource.Tables[0]));
            reporte.DataSources.Add(new ReportDataSource("MovimientoDetalle", reportDataSource.Tables[1]));

            string deviceInfo =
                "<DeviceInfo>" +
                "  <OutputFormat>PDF</OutputFormat>" + // Formato del documento PDF
                "  <PageWidth>" + pageWidth + "in</PageWidth>" + // Ancho de 8.5 pulgadas para paginas oficio
                "  <PageHeight>" + pageHeight + "in</PageHeight>" + // Alto de 13.38 pulgadas para paginas oficio
                "  <MarginTop>0.5in</MarginTop>" + // margen superior de 0.5 pulgadas
                "  <MarginLeft>" + MarginLeft + "</MarginLeft>" + // margen izquierdo de 1 pulgada
                "  <MarginRight>" + MarginRight + "</MarginRight>" + // margen derecho de 1 pulgada.
                "  <MarginBottom>0.5in</MarginBottom>" + // margen inferior de 0.5 pulgadas.
                "</DeviceInfo>";

            string mimeType;
            string encoding;
            string fileNameExtension;
            Warning[] warnings;
            string[] streams;

            // Se renderiza el reporte.
            reportBytes = reporte.Render("PDF",
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings);

            return reportBytes;

        }

        #endregion

        // GET: Producto_Ingreso_x_ID
        [Permiso("Control.Producto_Ingreso.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Producto Ingreso x ID", "Listado");

            List<Movimiento> Movimientos = new List<Movimiento>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                Movimientos = new MovimientoBL().ObtenerListadoPorFecha(FechaInicial.Value, FechaFinal.Value, 3, CustomHelper.getUserId()).ToList();
            }
            catch (Exception)
            {
            }

            return View(Movimientos);
        }

        [Permiso("Control.Producto_Ingreso.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Producto Ingreso x ID", "Nuevo");

            this.CargaControles();
            return View(new Movimiento() { AgenciaId = CustomHelper.getAgenciaId() });
        }

        [Permiso("Control.Producto_Ingreso.Crear")]
        [HttpPost]
        public ActionResult Crear(Movimiento modelo, string[] productoIds, string[] nombreProductoIds, long[] presentacionIds, string[] nombrePresentacionIds, decimal[] cantidadIds, int[] minimoIds, int[] maximoIds, decimal[] precioCostoIds, decimal[] precioVentaIds, string[] IDIds)
        {
            if (productoIds == null || productoIds.Length == 0)
            {
                ModelState.AddModelError("", "Para realizar un ingreso debe de asignar productos");
            }
                        
            modelo.UsrCreo = CustomHelper.getUserId();

            if (modelo.MovimientoEstadoId == 1)
            {
                modelo.Cancelado = true;                
            }
            
            if (ModelState.IsValid)
            {
                modelo.Detalles = new List<MovimientoDetalle>();
                for (int i = 0; i < productoIds.Length; i++)
                {
                    MovimientoDetalle Detalle = new MovimientoDetalle();
                    Detalle.ProductoId = productoIds[i];
                    Detalle.UnidadId = presentacionIds[i];
                    Detalle.Cantidad = cantidadIds[i];
                    Detalle.Minimo = minimoIds[i];
                    Detalle.Maximo = maximoIds[i];
                    Detalle.PrecioCosto = precioCostoIds[i];
                    Detalle.Precio = precioVentaIds[i];
                    Detalle.ID = IDIds[i];

                    modelo.Detalles.Add(Detalle);
                }

                modelo.MovimientoTipoId = 3;
                modelo.Operado = false;

                string strMensaje = new MovimientoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto-Ingreso-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            ViewBag.productoIds = productoIds;
            ViewBag.nombreProductoIds = nombreProductoIds;
            ViewBag.presentacionIds = presentacionIds;
            ViewBag.nombrePresentacionIds = nombrePresentacionIds;
            ViewBag.cantidadIds = cantidadIds;
            ViewBag.minimoIds = minimoIds;
            ViewBag.maximoIds = maximoIds;
            ViewBag.precioCostoIds = precioCostoIds;
            ViewBag.precioVentaIds = precioVentaIds;
            ViewBag.IDIds = IDIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Producto_Ingreso.Editar")]
        public ActionResult Editar(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Ingreso x ID", "Editar");

            this.CargaControles();
            return View(MovimientoActual);
        }

        [Permiso("Control.Producto_Ingreso.Editar")]
        [HttpPost]
        public ActionResult Editar(Movimiento modelo)
        {
            if (ModelState.IsValid)
            {                
                string strMensaje = new MovimientoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {                  
                    TempData["Producto-Ingreso-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Producto_Ingreso.Aprobar")]
        public ActionResult Aprobar(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Ingreso x ID", "Aprobar");

            this.CargaControles();
            return View(MovimientoActual);
        }

        [Permiso("Control.Producto_Ingreso.Aprobar")]
        [HttpPost]
        public ActionResult Aprobar(Movimiento modelo)
        {
            modelo.Operado = true;
            string strMensaje = new MovimientoBL().Aprobar(modelo, CustomHelper.getUserId());

            if (strMensaje.Equals("OK"))
            {
                using (var db = new GiveContext())
                {
                    var agencia = db.Agencias.FirstOrDefault(a => a.AgenciaId == modelo.AgenciaId);
                    if (agencia != null)
                    {
                        modelo.Detalles = new List<MovimientoDetalle>();
                        modelo.Detalles = db.MovimientoDetalles.Where(x => x.MovimientoId == modelo.MovimientoId).ToList();

                        foreach (var p in modelo.Detalles)
                        {
                            var productoId = p.ProductoId;
                            var producto = db.Productos.Include(pr => pr.Marca).FirstOrDefault(pr => pr.ProductoId == productoId);
                            var existencia = db.ProductoInventarios.FirstOrDefault(pr => pr.ProductoId == productoId && pr.AgenciaId == agencia.AgenciaId);
                            decimal existenciaActual = 0;

                            if (producto == null) continue;

                            if (existencia != null)
                            {
                                existenciaActual = existencia.Cantidad;                                
                            }

                            db.RegistrosKardex.Add(new RegistroKardex
                            {
                                FechaHora = DateTime.Now,
                                Fecha = DateTime.Today,
                                ProductoId = p.ProductoId,
                                ProductoCodigo = producto.Codigo,
                                ProductoNombre = producto.Nombre,
                                ProductoDescripcion = producto.Descripcion,
                                MarcaId = producto.MarcaId,
                                MarcaNombre = producto.Marca.Nombre,
                                DocumentoNumero = modelo.MovimientoId.ToString(),
                                AgenciaId = modelo.AgenciaId,
                                AgenciaNombre = agencia.Nombre,
                                TipoRegistro = "Ingreso Manual",
                                IngresoCantidadTienda = p.Cantidad,
                                IngresoCostoTienda = p.PrecioCosto,
                                ExistenciaFinalTienda = existenciaActual
                            });
                        }

                        db.SaveChanges();
                    }
                }

                TempData["Producto-Ingreso_Aprobar-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            return View(modelo);
        }

        [Permiso("Control.Producto_Ingreso.Anular")]
        public ActionResult Anular(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Ingreso x ID", "Anular");

            return View(MovimientoActual);
        }

        [Permiso("Control.Producto_Ingreso.Anular")]
        [HttpPost]
        public ActionResult Anular(long MovimientoId, string Comentario)
        {
            string strMensaje = new MovimientoBL().Anular(MovimientoId, Comentario, CustomHelper.getUserId(), 1);
            if (strMensaje.Equals("OK"))
            {
                TempData["Producto-Ingreso_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(MovimientoId);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Ingreso x ID", "Anular");

            return View(MovimientoActual);
        }

        [Permiso("Control.Producto_Ingreso.Detalle")]
        public ActionResult Detalle(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Ingreso x ID", "Detalle");

            return View(MovimientoActual);
        }       

        [Permiso("Control.Producto_Ingreso.Detalle")]
        public ActionResult Excel(long id)
        {
            List<EtiquetaModel> Etiquetas = new MovimientoBL().GenerarEtiquetas(id);

            if (Etiquetas == null)
            {
                return HttpNotFound();
            }

            if (Etiquetas.Count() == 0)
            {
                return HttpNotFound();
            }
            
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Etiqueta");
                ws.Cells["A1"].Value = "Codigo";
                ws.Cells["B1"].Value = "Barra";
                ws.Cells["C1"].Value = "Descripcion";
                ws.Cells["D1"].Value = "Precio";
                ws.Cells["E1"].Value = "Copia";

                var fila = 1;
                foreach (var item in Etiquetas)
                {
                    fila++;
                    ws.Cells[fila, 1].Value = item.Codigo;
                    ws.Cells[fila, 2].Value = item.Barra;
                    ws.Cells[fila, 3].Value = item.Descripcion;
                    ws.Cells[fila, 4].Value = item.Precio;
                    ws.Cells[fila, 5].Value = item.Copia;
                }

                using (var range = ws.Cells[1, 1, fila, 5])
                {
                    range.AutoFitColumns();
                }

                return File(pck.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("etiquetas_{0}.xlsx", id));
            }

        }

        [Permiso("Control.Reporte.Boleta_Ingreso")]
        public ActionResult Boleta(long Id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(Id);

            if (MovimientoActual != null)
            {
                DataSet Movimiento = new DataSet("Inventario");

                DataTable Encabezado = new DataTable("MovimientoEncabezado");
                DataTable Detalle = new DataTable("MovimientoDetalle");

                Encabezado.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Encabezado.Columns.Add(new DataColumn("Agencia", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Direccion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Descripcion", typeof(string)));
                Encabezado.Columns.Add(new DataColumn("Fecha", typeof(DateTime)));
                Encabezado.Columns.Add(new DataColumn("Categoria", typeof(string)));

                Encabezado.Rows.Add(MovimientoActual.MovimientoId, MovimientoActual.Agencia.Nombre, MovimientoActual.Proveedor.Nombre, MovimientoActual.Proveedor.Direccion, MovimientoActual.Descripcion, MovimientoActual.Fecha.ToString("dd/MM/yyyy"), MovimientoActual.MovimientoCategoria.Nombre);

                Detalle.Columns.Add(new DataColumn("MovimientoId", typeof(long)));
                Detalle.Columns.Add(new DataColumn("ProductoId", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Nombre", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Presentacion", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Cantidad", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Precio", typeof(decimal)));
                Detalle.Columns.Add(new DataColumn("Minimo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Maximo", typeof(string)));
                Detalle.Columns.Add(new DataColumn("Marca", typeof(string)));

                if (MovimientoActual.Detalles != null && MovimientoActual.Detalles.Count() > 0)
                {
                    foreach (var DetalleActual in MovimientoActual.Detalles)
                    {
                        Detalle.Rows.Add(MovimientoActual.MovimientoId, DetalleActual.ProductoId, string.Format("{0} - {1}", DetalleActual.Producto.Codigo, DetalleActual.Producto.Nombre), DetalleActual.Unidad.Nombre, DetalleActual.Cantidad, DetalleActual.PrecioCosto, 0, 0, DetalleActual.ID);
                    }
                }

                Movimiento.Tables.Add(Encabezado);
                Movimiento.Tables.Add(Detalle);

                // Se define la ruta del reporte
                var reportPath = Server.MapPath("~/Reports/ReportMovIngresoID.rdlc");

                // se obtienen los bytes del reporte en pdf
                var bytes = GetReportBytes(reportPath, Movimiento, 8.5m, 11.0m, 0.2m, 0m);

                return File(bytes, "application/pdf");

            }

            return View();
        }
       
        [HttpPost]
        [ActionName("Eliminar")]
        public JsonResult Eliminar(long movimientoId, string productoId)
        {
            return Json(new { Operacion = new MovimientoBL().Eliminar(movimientoId, productoId) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("NuevoProducto")]
        public JsonResult NuevoProducto(MovimientoDetalle modelo)
        {
            return Json(new { Operacion = new MovimientoBL().NuevoProducto(modelo) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("ValidarID")]
        public JsonResult ValidarID(string ID)
        {
            return Json(new { Operacion = new MovimientoBL().ValidarID(ID) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPresentacionPorProducto")]
        public JsonResult PresentacionListado(string id)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerPresentacionPorProductoId(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.UnidadId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}