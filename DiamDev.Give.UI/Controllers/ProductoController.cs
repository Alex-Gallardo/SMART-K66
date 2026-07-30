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
                var Unidades = new UnidadBL().ObtenerListado(false);
                var Precios = new PrecioBL().ObtenerListado();
                var Envases = new List<ComboModel>() { new ComboModel() { ID = 1, Nombre = "Sí" }, new ComboModel() { ID = 2, Nombre = "No" } };
                
                ViewBag.Unidades = new SelectList(Unidades, "UnidadId", "Nombre");
                ViewBag.Precios = new SelectList(Precios, "PrecioId", "Nombre");
                ViewBag.Envases = new SelectList(Envases, "ID", "Nombre");

                this.CargaCategorias();
                this.CargaMarcas();
            }

            private void CargaCategorias() 
            {
                var Categorias = new ProductoCategoriaBL().ObtenerListado(false);

                ViewBag.Categorias = new SelectList(Categorias, "ProductoCategoriaId", "Nombre");
            }

            private void CargaMarcas() 
            {
                var Marcas = new MarcaBL().ObtenerListado(false);

                ViewBag.Marcas = new SelectList(Marcas, "MarcaId", "Nombre");
            }

            private void CargaAgencias()
            {            
                var Agencias = new AgenciaBL().ObtenerListado(true, CustomHelper.getUserId());
                          
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");              
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
                    Productos = new ProductoBL().Buscar(search, CustomHelper.getEmpresaId()).ToList();
                }
                else
                {
                    Productos = new ProductoBL().ObtenerProductos(CustomHelper.getEmpresaId()).ToList();
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
        private void SaveFile(HttpPostedFileBase file, string fileName)
        {
            var path = System.IO.Path.Combine(Server.MapPath("~/Content/Files/"), fileName);
            var data = new byte[file.ContentLength];
            file.InputStream.Read(data, 0, file.ContentLength);
            //ProductoBL blll = new ProductoBL();
            //Producto prod = blll.ObtenerPorId(ProductoId);
            //prod.FotografiaApp = fileName;
            //string guardar = blll.Guardar(prod);
            using (var sw = new System.IO.FileStream(path, System.IO.FileMode.Create))
            {
                sw.Write(data, 0, data.Length);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            if (file != null)
            {
                if (!file.FileName.EndsWith(".jpg") && !file.FileName.EndsWith(".jpeg") && !file.FileName.EndsWith(".png"))
                    return View();

                var fileName = DateTime.Now.ToString("yyyyMMddHHmm.") + file.FileName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last();
                SaveFile(file, fileName);
               // string resp = UploadRecordsToDataBase(fileName);

                return RedirectToAction("Detalle");

            }

            // Tu podras decidir que hacer aqui
            // si el archivo es nulo
            return View();

        }
        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Kardex(long? AgenciaId, string ProductoId, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Kardex x Producto", "Listado");

            List<KardexMovimientoModel> Movimientos = new List<KardexMovimientoModel>();

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            try
            {
                if (AgenciaId != null && !string.IsNullOrWhiteSpace(ProductoId))
                {
                    Movimientos = new ProductoBL().KardexMovimientoModel(AgenciaId.Value, ProductoId, FechaInicial.Value, FechaFinal.Value).ToList();    
                }                
            }
            catch (Exception)
            {
            }

            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");
            ViewBag.agenciaId = AgenciaId;
            ViewBag.productoId = ProductoId;

            this.CargaAgencias();
            return View(Movimientos);
        }

        [Permiso("Control.Producto.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Producto", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.identificadorSi = "";
            ViewBag.identificadorNo = strAtributo;

            ViewBag.loteSi = "";
            ViewBag.loteNo = strAtributo;

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Producto.Crear")]
        [HttpPost]
        public ActionResult Crear(Producto modelo,  int[] precioIds, decimal[] valorIds, bool identificador, bool activo, ArchivoModel[] archivos, HttpPostedFileBase fotografiaApp)
        {
            if (precioIds == null || precioIds.Length == 0)
            {
                ModelState.AddModelError("", "Verificar opciones de precios");
            }           

            if (ModelState.IsValid)
            {
                modelo.Niveles = new List<ProductoNivelPrecio>();
               
                modelo.Precios = new List<ProductoPrecio>();
                for (int i = 0; i < precioIds.Length; i++)
                {
                    ProductoPrecio Precio = new ProductoPrecio();
                    Precio.PrecioId = precioIds[i];
                    Precio.Valor = valorIds[i];

                    modelo.Precios.Add(Precio);
                }

                if (fotografiaApp != null)
                {
                    modelo.Fotografia = new ProductoFotografia();
                    if (fotografiaApp != null)
                    {
                        byte[] FileData = new byte[fotografiaApp.ContentLength + 1];
                        fotografiaApp.InputStream.Read(FileData, 0, fotografiaApp.ContentLength);

                        modelo.FotografiaApp = fotografiaApp.FileName.Replace(" ", "_");
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
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

                modelo.TieneEnvase = false;
                modelo.TieneIdentificador = identificador;
                modelo.TieneLote = false;
                modelo.Activo = activo;

                modelo.EmpresaId = CustomHelper.getEmpresaId();

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

            ViewBag.identificadorSi = identificador == true ? strAtributo : "";
            ViewBag.identificadorNo = identificador == false ? strAtributo : "";          

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

            ViewBag.identificadorSi = ProductoActual.TieneIdentificador == true ? strAtributo : "";
            ViewBag.identificadorNo = ProductoActual.TieneIdentificador == false ? strAtributo : "";             

            ViewBag.activoSi = ProductoActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = ProductoActual.Activo == false ? strAtributo : "";           

            this.CargaControles();
            return View(ProductoActual);
        }

        [Permiso("Control.Producto.Editar")]
        [HttpPost]
        public ActionResult Editar(Producto modelo, int[] precioIds, decimal[] valorIds, bool identificador, bool activo, ArchivoModel[] archivos, HttpPostedFileBase fotografiaApp)
        {
            if (precioIds == null || precioIds.Length == 0)
            {
                ModelState.AddModelError("", "Verificar opciones de precios");
            }          

            if (ModelState.IsValid)
            {
                modelo.Niveles = new List<ProductoNivelPrecio>();             

                modelo.Precios = new List<ProductoPrecio>();
                for (int i = 0; i < precioIds.Length; i++)
                {
                    ProductoPrecio Precio = new ProductoPrecio();
                    Precio.PrecioId = precioIds[i];
                    Precio.Valor = valorIds[i];

                    modelo.Precios.Add(Precio);
                }

                if (fotografiaApp != null)
                {
                    modelo.Fotografia = new ProductoFotografia();
                    if (fotografiaApp != null)
                    {
                        byte[] FileData = new byte[fotografiaApp.ContentLength + 1];
                        fotografiaApp.InputStream.Read(FileData, 0, fotografiaApp.ContentLength);

                        modelo.FotografiaApp = fotografiaApp.FileName.Replace(" ", "_");
                        modelo.Fotografia = new ProductoFotografia() { Nombre = fotografiaApp.FileName, Content = FileData, ContentType = fotografiaApp.ContentType, Length = fotografiaApp.ContentLength };
                    }
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

                modelo.TieneEnvase = modelo.EnvaseId == 1 ? true : false;
                modelo.TieneIdentificador = identificador;               
                modelo.Activo = activo;

                modelo.EmpresaId = CustomHelper.getEmpresaId();
                
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

            ViewBag.identificadorSi = identificador == true ? strAtributo : "";
            ViewBag.identificadorNo = identificador == false ? strAtributo : "";        

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

            ProductoActual.EnvaseId = ProductoActual.TieneEnvase ? 1 : 2;
            return View(ProductoActual);
        }

        [Permiso("Control.Producto.Eliminar")]
        public ActionResult Eliminar(string id)
        {
            Producto ProductoActual = new ProductoBL().HistorialPorProductoId(id, true);

            if (ProductoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto", "Eliminar");

            return View(ProductoActual);
        }

        [Permiso("Control.Producto.Eliminar")]
        [HttpPost]
        public ActionResult Eliminar(Producto modelo)
        {
            string strMensaje = new ProductoBL().Eliminar(modelo);

            if (strMensaje.Equals("OK"))
            {
                TempData["Eliminar_Producto-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Producto ProductoActual = new ProductoBL().HistorialPorProductoId(modelo.ProductoId, true);

            if (ProductoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto", "Eliminar");

            return View(ProductoActual);
        }

        [Permiso("Control.Producto.Consulta")]
        public ActionResult Consulta(int? page, string search)
        {
            CustomHelper.setTitle("Producto", "Consulta");

            List<InventarioModel> Productos = new List<InventarioModel>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Productos = new ProductoBL().BuscarExistenciaxAgencia(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Productos = new ProductoBL().ObtenerProductosExistenciaxAgencia(CustomHelper.getUserId()).ToList();
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

        [Permiso("Control.Producto.Consulta_Administrativa")]
        public ActionResult Consulta_Administrativa(int? page, string search)
        {
            CustomHelper.setTitle("Producto", "Consulta Administrativa");

            List<InventarioModel> Productos = new List<InventarioModel>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Productos = new ProductoBL().ConsultaAdministrativaExistenciaxAgencia(search, CustomHelper.getUserId()).ToList();
                }
                else
                {
                    Productos = new ProductoBL().ConsultaAdministrativaProductosExistenciaxAgencia(CustomHelper.getUserId()).ToList();
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
                Producto ProductoActual = new ProductoBL().ObtenerExistenciaPorAgenciaYProductoSinEscala(agenciaId, productoId, presentacionId, true, empleado);
                if (ProductoActual != null)
                {
                    return Json(new { Operacion = true, Data = ProductoActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoxEscalaPrecio")]
        public JsonResult ObtenerProductoxEscalaPrecio(long agenciaId, string productoId, long presentacionId, int cantidad, bool empleado)
        {
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                Producto ProductoActual = new ProductoBL().ObtenerPrecioPorAgenciaYProducto(agenciaId, productoId, presentacionId, cantidad, true, empleado);
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

        [ActionName("ObtenerProductoxTextoLibre")]
        public JsonResult ObtenerProductoxTextoLibre(string search)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                List<Producto> Productos = new ProductoBL().BuscarProductoxTextoLibre(search, CustomHelper.getEmpresaId());                
                if (Productos != null && Productos.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Productos }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoxTextoLibreK66")]
        public JsonResult ObtenerProductoxTextoLibreK66(string search, string clienteId, long empresaId)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                List<ProductoK66> Productos = new ProductoK66BL().BuscarProductoxTextoLibreK66(search, clienteId, CustomHelper.getUserId(), empresaId);
                if (Productos != null && Productos.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Productos }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerBodegasPorProducto")]
        public JsonResult ObtenerBodegasPorProducto(string id,string CardCode, long empresaId)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                List<Warehouse> Productos = new ProductoK66BL().BuscarBodegasxProducto(id,  CardCode, empresaId);
                if (Productos != null && Productos.Count() > 0)
                {
                    IList _result = new List<SelectListItem>();
                    _result = Productos.Select(m => new SelectListItem() { Text = m.Nombre, Value = m.WarehouseId.ToString() }).ToList();
                    return Json(_result, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerContadorBodegasPorProducto")]
        public JsonResult ObtenerContadorBodegasPorProducto(string ItemCode, string Bodega, long empresaId)
        {
            if (!string.IsNullOrWhiteSpace(ItemCode))
            {
                ResponseContadorBodega Producto = new ProductoK66BL().BuscarContadorBodegaxProducto(ItemCode, Bodega, empresaId);
                if (Producto.Contador > 0)
                {
                    return Json(new { Operacion = true, Data = Producto }, JsonRequestBehavior.AllowGet);
                }else
                {
                    return Json(new { Operacion = false}, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerFotografiaProducto")]
        public JsonResult ObtenerFotografiaProducto(string productoId)
        {
            if (!string.IsNullOrWhiteSpace(productoId))
            {
                ProductoFotografia FotografiaActual = new ProductoBL().Fotografia(1, productoId);
                if (FotografiaActual != null)
                {
                    return Json(new { Operacion = true, Data = string.Format("data:{0};base64,{1}", FotografiaActual.ContentType, Convert.ToBase64String(FotografiaActual.Content)) }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaProductoAutocomplementar(string search, bool? id = null)
        {
            List<Producto> Productos = new ProductoBL().BuscarProductoxAutocompletar(search, id);
            return Json(Productos, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaProductoAutocomplementarK66(string search, string clienteId, long empresaId)
        {
            List<ProductoK66> Productos = new ProductoK66BL().BuscarProductoxNombreK66(search, clienteId, CustomHelper.getUserId(), empresaId);
            return Json(Productos, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaProductoAutocomplementarExistencia(string search,long agenciaid, bool? id = null)
        {
            //List<Producto> Productos = new ProductoBL().BuscarProductoxAutocompletarExistencia(search, CustomHelper.getAgenciaId(), id);
            List<Producto> Productos = new ProductoBL().BuscarProductoxAutocompletarExistencia(search, agenciaid, CustomHelper.getEmpresaId(), id);
            return Json(Productos, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("EliminarFotografia")]
        public JsonResult EliminarFotografia(string productoId, int id)
        {
            return Json(new { Operacion = new ProductoBL().EliminarFotografia(productoId, id) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoID")]
        public JsonResult ObtenerProductoID(string id, long agenciaId)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerProductoID(id, agenciaId).Select(m => new SelectListItem() { Text = m.ID, Value = m.ID }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoxCategoriaId")]
        public JsonResult ObtenerProductoxCategoriaId(long id)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerProductoPorCategoriaId(id).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.ProductoId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerLotexProductoId")]
        public JsonResult ObtenerLotexProductoId(string id)
        {
            IList _result = new List<SelectListItem>();
            _result = new ProductoBL().ObtenerLotesxProductoId(id, CustomHelper.getAgenciaId()).Select(m => new SelectListItem() { Text = m.Nombre, Value = m.Lote }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerLotexId")]
        public JsonResult ObtenerLotexId(string id, string lote)
        {
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(lote))
            {
                ProductoLote LoteActual = new ProductoBL().ObtenerLotexId(id, CustomHelper.getAgenciaId(), lote);
                if (LoteActual != null)
                {
                    return Json(new { Operacion = true, Data = LoteActual }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerLotes")]
        public JsonResult ObtenerLotes(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                return Json(new { Operacion = true, Data = new ProductoBL().ObtenerLotes(id, CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPorIDK66")]
        public JsonResult ObtenerPorIDK66(string id, string unidad, string clienteId, string direccionId, long empresaId, int cantidad)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var productoActual = new ProductoK66BL().ObtenerxIDK66(id, unidad, clienteId, direccionId, CustomHelper.getUserId(), empresaId, cantidad);

            if (productoActual == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = true, Data = productoActual }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPrecioxCantidad")]
        public JsonResult ObtenerPrecioxCantidad(string id, string clienteId, int cantidad, long empresaId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var precioActual = new ProductoK66BL().ObtenerPrecioxCantidad(empresaId, id, clienteId, cantidad);

            if (precioActual == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = true, Data = precioActual }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetExistenciasxProducto(string id, string clienteId, long empresaId)
        {
            return PartialView("_ProductoExistencias", new ProductoK66BL().ObtenerExistenciaxIDK66(id, clienteId, CustomHelper.getUserId(), empresaId));
        }

        #region Kardex 

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Ingreso(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Ingreso", "Detalle");

            return View(MovimientoActual);
        }

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Egreso(long id)
        {
            Movimiento MovimientoActual = new MovimientoBL().ObtenerPorId(id, false);

            if (MovimientoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Producto Egreso", "Detalle");

            return View(MovimientoActual);
        }

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Recibo(long id)
        {
            Recibo ReciboActual = new ReciboBL().ObtenerPorId(id, true, true);

            if (ReciboActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Recibo", "Detalle");

            return View(ReciboActual);
        }

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Factura(long id)
        {
            Factura FacturaActual = new FacturaBL().ObtenerPorId(id, true, true, false);

            if (FacturaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Factura", "Detalle");

            return View(FacturaActual);
        }

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Egreso_x_Ajuste(long id)
        {
            Egreso EgresoActual = new EgresoBL().ObtenerPorId(id, true);

            if (EgresoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Egreso", "Detalle");

            return View(EgresoActual);
        }

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Traslado(long id)
        {
            Traslado TrasladoActual = new TrasladoBL().ObtenerPorId(id, true);

            if (TrasladoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Traslado", "Detalle");

            return View(TrasladoActual);
        }

        [Permiso("Control.Producto_Kardex.Ver_Listado")]
        public ActionResult Credito(long id)
        {
            Credito CreditoActual = new CreditoBL().ObtenerPorId(id, true);

            if (CreditoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Credito", "Detalle");

            return View(CreditoActual);
        }

        #endregion
    }
}