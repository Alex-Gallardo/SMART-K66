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

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class ProductoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Categorias = new ProductoCategoriaBL().ObtenerListado(false);
                var Marcas = new MarcaBL().ObtenerListado(false);
                var Unidades = new UnidadBL().ObtenerListado(false);
                var Precios = new PrecioBL().ObtenerListado();

                ViewBag.Categorias = new SelectList(Categorias, "ProductoCategoriaId", "Nombre");
                ViewBag.Marcas = new SelectList(Marcas, "MarcaId", "Nombre");
                ViewBag.Unidades = new SelectList(Unidades, "UnidadId", "Nombre");
                ViewBag.Precios = new SelectList(Precios, "PrecioId", "Nombre");
            }

        #endregion

        #region Metodos Publicos

            public FileResult Preview(int id, string documentoId)
            {
                ProductoFotografia FotografiaActual = new ProductoBL().Fotografia(id, documentoId);

                var content = Binario.Drawing.ImageManager.GetThumbnail(FotografiaActual.Content, 100);
                return File(content, FotografiaActual.ContentType);
            }

            public FileResult Imagen(int id, string documentoId)
            {
                ProductoFotografia FotografiaActual = new ProductoBL().Fotografia(id, documentoId);

                return File(FotografiaActual.Content, FotografiaActual.ContentType);
            }

        #endregion

        // GET: Producto
        [Permiso("Control.Producto.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Producto", "Listado");

            List<Producto> Productos = new List<Producto>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Productos = new ProductoBL().Buscar(search).ToList();
                }
                else
                {
                    Productos = new ProductoBL().ObtenerListado().ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Productos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Producto.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Producto", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Producto.Crear")]
        [HttpPost]
        public ActionResult Crear(Producto modelo, int[] precioIds, decimal[] valorIds, bool activo, ArchivoModel[] archivos)
        {
            if (precioIds == null || precioIds.Length == 0)
            {
                ModelState.AddModelError("", "Verificar opciones de precios");
            }

            if (ModelState.IsValid)
            {
                modelo.Precios = new List<ProductoPrecio>();
                for (int i = 0; i < precioIds.Length; i++)
                {
                    ProductoPrecio Precio = new ProductoPrecio();
                    Precio.PrecioId = precioIds[i];
                    Precio.Valor = valorIds[i];

                    modelo.Precios.Add(Precio);
                }

                if (archivos != null && archivos.Count() > 0)
                {
                    modelo.Imagenes = new List<ProductoFotografia>();
                    foreach (ArchivoModel archivo in archivos)
                    {
                        if (archivo != null)
                        {
                            if (archivo.Archivo != null)
                            {
                                byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                                archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                                modelo.Imagenes.Add(new ProductoFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                            }
                        }
                    }
                }

                modelo.Activo = activo;
                string strMensaje = new ProductoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            ViewBag.precioIds = precioIds;
            ViewBag.valorIds = valorIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Producto.Editar")]
        public ActionResult Editar(string id)
        {
            Producto ProductoActual = new ProductoBL().ObtenerPorId(id, true, false, true);

            if (ProductoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto", "Editar");

            if (ProductoActual.Precios != null && ProductoActual.Precios.Count() > 0)
            {
                ViewBag.precioIds = ProductoActual.Precios.Select(x => x.PrecioId);
                ViewBag.valorIds = ProductoActual.Precios.Select(x => x.Valor);
            }
            else
            {
                ViewBag.precioIds = 0;
                ViewBag.valorIds = 0;
            }

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = ProductoActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = ProductoActual.Activo == false ? strAtributo : "";

            this.CargaControles();
            return View(ProductoActual);
        }

        [Permiso("Control.Producto.Editar")]
        [HttpPost]
        public ActionResult Editar(Producto modelo, int[] precioIds, decimal[] valorIds, bool activo, ArchivoModel[] archivos)
        {
            if (precioIds == null || precioIds.Length == 0)
            {
                ModelState.AddModelError("", "Verificar opciones de precios");
            }

            if (ModelState.IsValid)
            {
                modelo.Precios = new List<ProductoPrecio>();
                for (int i = 0; i < precioIds.Length; i++)
                {
                    ProductoPrecio Precio = new ProductoPrecio();
                    Precio.PrecioId = precioIds[i];
                    Precio.Valor = valorIds[i];

                    modelo.Precios.Add(Precio);
                }

                if (archivos != null && archivos.Count() > 0)
                {
                    modelo.Imagenes = new List<ProductoFotografia>();
                    foreach (ArchivoModel archivo in archivos)
                    {
                        if (archivo != null)
                        {
                            if (archivo.Archivo != null)
                            {
                                byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                                archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                                modelo.Imagenes.Add(new ProductoFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                            }
                        }
                    }
                }

                modelo.Activo = activo;
                string strMensaje = new ProductoBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            ViewBag.precioIds = precioIds;
            ViewBag.valorIds = valorIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Producto.Detalle")]
        public ActionResult Detalle(string id)
        {
            Producto ProductoActual = new ProductoBL().HistorialPorProductoId(id, true);

            if (ProductoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto", "Detalle");

            return View(ProductoActual);
        }

        [Permiso("Control.Producto.Consulta")]
        public ActionResult Consulta(int? page, string search)
        {
            CustomHelper.setTitle("Producto", "Consulta");

            List<InventarioModel> Productos = new List<InventarioModel>();

            try
            {
                //if (!string.IsNullOrWhiteSpace(search) && search != null)
                //{
                //    Productos = new ProductoBL().BuscarExistencia(search).ToList();
                //}
                //else
                //{
                //    Productos = new ProductoBL().ObtenerProductoConExistencia().ToList();
                //}
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Productos.ToPagedList(pageNumber, pageSize));
        }

        [ActionName("ObtenerProductoPorCategoriaIdYMarcaId")]
        public JsonResult ProductoListado(long categoriaId, long marcaId)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerProductoPorCategoriaIdYMarcaId(categoriaId, marcaId).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.ProductoId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPrecioPorProductoId")]
        public JsonResult PrecioListado(string productoId, long presentacionId)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerPrecioPorProductoId(productoId, presentacionId).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.Valor.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPrecioActualPorProductoId")]
        public JsonResult ObtenerPrecio(string productoId, long presentacionId)
        {
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                ProductoPrecio PrecioActual = new ProductoBL().ObtenerPrecioActualPorProductoId(productoId, presentacionId);
                if (PrecioActual != null)
                {
                    return Json(new { Operacion = true, Data = PrecioActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProducto")]
        public JsonResult ObtenerProducto(long agenciaId, string productoId, long presentacionId, bool empleado)
        {
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                Producto ProductoActual = new ProductoBL().ObtenerExistenciaPorAgenciaYProducto(agenciaId, productoId, presentacionId, true, empleado);
                if (ProductoActual != null)
                {
                    return Json(new { Operacion = true, Data = ProductoActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoxBarra")]
        public JsonResult ObtenerProductoxBarra(string barra)
        {
            if (!string.IsNullOrWhiteSpace(barra))
            {
                Producto ProductoActual = new ProductoBL().ObtenerProductoxBarra(barra);
                if (ProductoActual != null)
                {
                    return Json(new { Operacion = true, Data = ProductoActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }
}