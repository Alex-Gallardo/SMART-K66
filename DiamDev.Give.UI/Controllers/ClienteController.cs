using System;
using System.Collections;
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

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class ClienteController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Vendedores = new VendedorBL().ObtenerListado(false, 0);
                var Tipos = new ClienteTipoBL().ObtenerListado();               
                             
                ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");
                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");

                this.CargaRegiones();
            }

            public void CargaRegiones() 
            {
                var Regiones = new RegionBL().ObtenerListado();

                ViewBag.Regiones = new SelectList(Regiones, "RegionId", "Nombre");
            }

            public void CargaDepartamentos()
            {
                var Departamentos = new DepartamentoBL().ObtenerListado(false);

                ViewBag.Departamentos = new SelectList(Departamentos, "DepartamentoId", "Nombre");
            }

        #endregion

        #region Metodos Publicos

            public FileResult Preview(int id, long clienteId)
            {
                ClienteFotografia FotografiaActual = new ClienteBL().Fotografia(id, clienteId);

                var content = Binario.Drawing.ImageManager.GetThumbnail(FotografiaActual.Content, 100);
                return File(content, FotografiaActual.ContentType);
            }

            public FileResult Imagen(int id, long clienteId)
            {
                ClienteFotografia FotografiaActual = new ClienteBL().Fotografia(id, clienteId);

                return File(FotografiaActual.Content, FotografiaActual.ContentType);
            }

        #endregion

        // GET: Cliente
        [Permiso("Control.Cliente.Ver_Listado")]
        public ActionResult Index(int? page, long? regionId, string search)
        {
            CustomHelper.setTitle("Cliente", "Listado");

            List<Cliente> Clientes = new List<Cliente>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Clientes = new ClienteBL().Buscar(search, CustomHelper.getEmpresaId()).ToList();
                }
                else if (regionId != null)
                {
                    Clientes = new ClienteBL().ObtenerListadoxRegionId(regionId.Value, CustomHelper.getEmpresaId()).ToList();
                }
                else if (!string.IsNullOrWhiteSpace(search) && search != null && regionId != null)
                {
                    Clientes = new ClienteBL().BuscarxRegionId(search, regionId.Value, CustomHelper.getEmpresaId()).ToList();
                }
                else
                {
                    Clientes = new ClienteBL().ObtenerListado(true, false, CustomHelper.getEmpresaId()).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.Search = search;
            ViewBag.RegionId = regionId;

            this.CargaRegiones();

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Clientes.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Cliente.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Cliente", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = "";
            ViewBag.vipNo = strAtributo;

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Cliente.Crear")]
        [HttpPost]
        public ActionResult Crear(Cliente modelo, bool vip, bool activo, ArchivoModel[] archivos)
        {
            if (ModelState.IsValid)
            {
                if (archivos != null && archivos.Count() > 0)
                {
                    modelo.Imagenes = new List<ClienteFotografia>();
                    foreach (ArchivoModel archivo in archivos)
                    {
                        if (archivo != null)
                        {
                            if (archivo.Archivo != null)
                            {
                                byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                                archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                                modelo.Imagenes.Add(new ClienteFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                            }
                        }
                    }
                }

                modelo.EmpresaId = CustomHelper.getEmpresaId();
                modelo.Vip = vip;
                modelo.Activo = activo;
                string strMensaje = new ClienteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Cliente-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = vip == true ? strAtributo : "";
            ViewBag.vipNo = vip == false ? strAtributo : "";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Cliente.Crear")]
        [HttpPost]
        [ActionName("NuevoCliente")]
        public ActionResult Crear(Cliente modelo)
        {
            if (ModelState.IsValid)
            {
                modelo.EmpresaId = CustomHelper.getEmpresaId();
                long ClienteId = new ClienteBL().GuardarML(modelo, CustomHelper.getEmpresaId());

                if (ClienteId > 0)
                {
                    return Json(new { Operacion = true, Cliente = ClienteId }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        
                 [Permiso("Control.Cliente.Crear")]
        [HttpPost]
        [ActionName("NuevaDireccion")]
        public ActionResult NuevaDireccion(string direccion,string clienteid)
        {
            DireccionCliente nn = new DireccionCliente();
            nn.Direccion = direccion;
            nn.ClienteId = Convert.ToInt64(clienteid);
            string resp = new ClienteBL().GuardarDireccion(nn);

            return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
        }

        [Permiso("Control.Cliente.Editar")]
        public ActionResult Editar(long id)
        {
            Cliente ClienteActual = new ClienteBL().ObtenerPorId(id, true, true);

            if (ClienteActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cliente", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = ClienteActual.Vip == true ? strAtributo : "";
            ViewBag.vipNo = ClienteActual.Vip == false ? strAtributo : "";

            ViewBag.activoSi = ClienteActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = ClienteActual.Activo == false ? strAtributo : "";

            this.CargaControles();
            return View(ClienteActual);
        }

        [Permiso("Control.Cliente.Editar")]
        [HttpPost]
        public ActionResult Editar(Cliente modelo, bool vip, bool activo, ArchivoModel[] archivos)
        {
            if (ModelState.IsValid)
            {
                if (archivos != null && archivos.Count() > 0)
                {
                    modelo.Imagenes = new List<ClienteFotografia>();
                    foreach (ArchivoModel archivo in archivos)
                    {
                        if (archivo != null)
                        {
                            if (archivo.Archivo != null)
                            {
                                byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                                archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                                modelo.Imagenes.Add(new ClienteFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                            }
                        }
                    }
                }

                modelo.Vip = vip;
                modelo.Activo = activo;
                string strMensaje = new ClienteBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Cliente-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }

            }

            string strAtributo = "checked='checked'";

            ViewBag.vipSi = vip == true ? strAtributo : "";
            ViewBag.vipNo = vip == false ? strAtributo : "";

            ViewBag.activoSi = activo == true ? strAtributo : "";
            ViewBag.activoNo = activo == false ? strAtributo : "";

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Cliente.Detalle")]
        public ActionResult Detalle(long id)
        {
            Cliente ClienteActual = new ClienteBL().ObtenerPorId(id, true, true, true);

            if (ClienteActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cliente", "Detalle");

            this.CargaDepartamentos();
            return View(ClienteActual);
        }

        [Permiso("Control.Cliente.Detalle")]
        public ActionResult Historial(long? id, int? page, DateTime? FechaInicial, DateTime? FechaFinal)
        {
            if (!id.HasValue)
            {
                id = 0;
            }

            if (!FechaInicial.HasValue && !FechaFinal.HasValue)
            {
                FechaInicial = DateTime.Today;
                FechaFinal = DateTime.Today;
            }

            ClienteHistorial ClienteHistorialActual = new ClienteBL().ObtenerPorIdHistorial(id.Value, FechaInicial.Value, FechaFinal.Value);

            if (ClienteHistorialActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Cliente", "Historial");         

            ViewBag.id = id;
            ViewBag.fechaInicial = FechaInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.fechaFinal = FechaFinal.Value.ToString("yyyy-MM-dd");

            int pageSize = 20;
            int pageNumber = (page ?? 1);
            return View(ClienteHistorialActual.Recibos.ToPagedList(pageNumber, pageSize));
        }

        [ActionName("ObtenerDescuento")]
        public JsonResult ObtenerDescuento(long clienteId)
        {
            if (clienteId > 0)
            {
                return Json(new { Operacion = true, Data = new ClienteBL().ObtenerDescuentoPorId(clienteId) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
                
        [ActionName("ObtenerPorNit")]
        public JsonResult ObtenerPorNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerPorNit(nit, CustomHelper.getEmpresaId());

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            int DescuentoGeneral = 0;
            if (cliente.Tipo != null)
            {
                DescuentoGeneral = cliente.Tipo.PorcentajeDescuento;
            }
            return Json(new { Operacion = true, Data = new { cliente.ClienteId, cliente.Nit, cliente.Nombre, cliente.Direccion, cliente.DPI, cliente.NoTelefono, cliente.EmailCliente, cliente.Vip, cliente.Activo, cliente.VendedorId, DescuentoGeneral } }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPorID")]
        public JsonResult ObtenerPorID(long id)
        {
            if (id == 0)
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerPorId(id, false, false);

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            int DescuentoGeneral = 0;
            if (cliente.Tipo != null)
            {
                DescuentoGeneral = cliente.Tipo.PorcentajeDescuento;
            }

            return Json(new { Operacion = true, Data = new { cliente.ClienteId, cliente.Nit, cliente.Nombre, cliente.Direccion, cliente.DPI, cliente.NoTelefono, cliente.EmailCliente, cliente.Vip, cliente.Activo, cliente.VendedorId, DescuentoGeneral } }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPorIDK66")]
        public JsonResult ObtenerPorIDK66(string id, long empresaId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerxIDK66(id, empresaId, CustomHelper.getUserId());

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }           

            return Json(new { Operacion = true, Data = cliente }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerPorIDGeneralK66")]
        public JsonResult ObtenerPorIDGeneralK66(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerxIDGeneralK66(id);

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = true, Data = cliente }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerDireccionesClienteId")]
        public JsonResult ObtenerDireccionesClienteId(long id)
        {
            if (id == 0)
            {
                return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
            }

            var cliente = new ClienteBL().ObtenerDireccionesClientePorId(id);

            if (cliente == null)
            {
                return Json(new { Operacion = true, Data = (object)null }, JsonRequestBehavior.AllowGet);
            }

         

            return Json(new { Operacion = true, Data =cliente  }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaCliente(string search)
        {
            //List<ClienteConsultaModel> Clientes = new ClienteBL().BuscarClientexNombre(search, CustomHelper.getEmpresaId());
            List<ClienteConsultaModel> Clientes = new ClienteBL().BuscarClientexNombreK66(search, CustomHelper.getUserId(), CustomHelper.getEmpresaId());
            return Json(Clientes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaClienteK66(string search, long empresaId)
        {
            var xx = CustomHelper.getUserId();
            List<ClienteConsultaModel> Clientes = new ClienteBL().BuscarClientexNombreK66(search, CustomHelper.getUserId(), empresaId);
            return Json(Clientes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ConsultaClienteVisitaK66(string search, bool bolik, bool empaques, bool faes, bool graco)
        {
            List<ClienteConsultaModel> Clientes = new ClienteBL().BuscarClientexNombreVisitaK66(search, CustomHelper.getUserId(), bolik, empaques, faes, graco);
            return Json(Clientes, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerClientexTextoLibre")]
        public JsonResult ObtenerClientexTextoLibre(string search)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                List<Cliente> Clientes = new ClienteBL().BuscarClientexTextoLibre(search, CustomHelper.getEmpresaId());
                if (Clientes != null && Clientes.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Clientes }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerClientexTextoLibreK66")]
        public JsonResult ObtenerClientexTextoLibreK66(string search, long empresaId)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                List<ClienteConsultaModel> Clientes = new ClienteBL().BuscarClientexTextoLibreK66(search, CustomHelper.getUserId(), empresaId);
                if (Clientes != null && Clientes.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Clientes }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerClienteVisitaxTextoLibreK66")]
        public JsonResult ObtenerClienteVisitaxTextoLibreK66(string search, bool bolik, bool empaques, bool faes, bool graco)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                List<ClienteConsultaModel> Clientes = new ClienteBL().BuscarClienteVisitaxTextoLibreK66(search, CustomHelper.getUserId(), bolik, empaques, faes, graco);
                if (Clientes != null && Clientes.Count() > 0)
                {
                    return Json(new { Operacion = true, Data = Clientes }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("EliminarFotografia")]
        public JsonResult EliminarFotografia(long clienteId, int id)
        {
            return Json(new { Operacion = new ClienteBL().EliminarFotografia(clienteId, id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("ExisteNIT")]
        public JsonResult ExisteNIT(string nit)
        {
            return Json(new { Operacion = new ClienteBL().VerificarNIT(nit, CustomHelper.getEmpresaId()) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("ExisteCelular")]
        public JsonResult ExisteCelular(string celular)
        {
            return Json(new { Operacion = new ClienteBL().VerificarCelular(celular) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("NuevoContacto")]
        public ActionResult NuevoContacto(ClienteContacto modelo)
        {
            string Mensaje = new ClienteBL().GuardarContacto(modelo);

            if (Mensaje.Equals("OK"))
            {
                return Json(new { Operacion = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("ObtenerClienteDigifact")]
        public JsonResult ObtenerClienteDigifact(string nit)
        {
            if (!string.IsNullOrWhiteSpace(nit))
            {
                RESPONSE ClienteActual = new FacturaBL().ObtenerCliente(nit);
                if (ClienteActual != null)
                {
                    if (!string.IsNullOrWhiteSpace(ClienteActual.NOMBRE))
                    {
                        return Json(new { Operacion = true, Data = ClienteActual }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerDireccionxClienteId")]
        public JsonResult ObtenerDireccionxClienteId(string id, long empresaId)
        {
            IList _result = new List<SelectListItem>();
            _result = new ClienteBL().ObtenerDireccionxCliente(id, empresaId).Select(m => new SelectListItem() { Text = m.Direccion, Value = m.DireccionId.ToString() }).ToList();
            return Json(_result, JsonRequestBehavior.AllowGet);
        }
    }
}