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
    public class UsuarioController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId(), false);
                var Departamentos = new DepartamentoBL().ObtenerListado(true);
                var Empresas = new EmpresaBL().ObtenerListado();
                var Roles = new RolBL().ObtenerListado();
                var Agencias = new AgenciaBL().ObtenerListado(false);
                var AgenciaConsultas = new AgenciaBL().ObtenerListado(false);

                ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");
                ViewBag.Departamentos = new SelectList(Departamentos, "DepartamentoId", "Nombre");
                ViewBag.Empresas = new SelectList(Empresas, "EmpresaId", "Nombre");
                ViewBag.Roles = new SelectList(Roles, "RolId", "Nombre");
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
                ViewBag.AgenciaConsultas = new SelectList(AgenciaConsultas, "AgenciaId", "Nombre");

            }

        #endregion

        // GET: Usuario
        [Permiso("Control.Usuario.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Usuario", "Listado");

            List<Usuario> Usuarios = new List<Usuario>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Usuarios = new UsuarioBL().Buscar(search).ToList();
                }
                else
                {
                    Usuarios = new UsuarioBL().ObtenerListado();
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
            return View(Usuarios.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Usuario.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Usuario", "Nuevo");

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = strAtributo;
            ViewBag.ActivoNo = "";

            ViewBag.TokenSi = "";
            ViewBag.TokenNo = strAtributo;

            this.CargaControles();
            return View();
        }

        [HttpPost]
        [Permiso("Control.Usuario.Crear")]
        public ActionResult Crear(Usuario modelo, int[] rolesIds, long[] agenciasIds, long[] empresasIds, string[] codigoIds, string[] serieSapIds , bool activo, bool token)
        {
            if (rolesIds == null || rolesIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un rol");
            }

            if (agenciasIds == null || agenciasIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos una agencia");
            }

            if (empresasIds == null || empresasIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos una empresa");
            }

            if (ModelState.IsValid)
            {
                modelo.Roles = new List<UsuarioRol>();
                for (int i = 0; i < rolesIds.Length; i++)
                {
                    modelo.Roles.Add(new UsuarioRol() { RolId = rolesIds[i] });
                }

                modelo.Agencias = new List<UsuarioAgencia>();
                for (int i = 0; i < agenciasIds.Length; i++)
                {
                    modelo.Agencias.Add(new UsuarioAgencia() { AgenciaId = agenciasIds[i] });
                }

                if (empresasIds == null)
                {
                    throw new Exception("empresasIds NULL");
                }

                if (codigoIds == null)
                {
                    throw new Exception("codigoIds NULL");
                }

                if (serieSapIds == null)
                {
                    throw new Exception("serieSapIds NULL");
                }

                if (modelo == null)
                {
                    throw new Exception("modelo NULL");
                }

                modelo.Empresas = new List<UsuarioEmpresa>();
                for (int i = 0; i < empresasIds.Length; i++)
                {
                    modelo.Empresas.Add(new UsuarioEmpresa() { EmpresaId = empresasIds[i], Codigo = codigoIds[i],SERIE_SAP = serieSapIds[i] });
                }

                modelo.AutenticarSite = true;
                modelo.AutenticarAndroid = true;
                modelo.Activo = activo;
                modelo.Token = token;
                modelo.ReiniciarPassword = false;  
           
                string strMensaje = new UsuarioBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Usuario-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";

            ViewBag.TokenSi = token == true ? strAtributo : "";
            ViewBag.TokenNo = token == false ? strAtributo : "";

            ViewBag.RolesIds = rolesIds;
            ViewBag.AgenciasIds = agenciasIds;

            ViewBag.EmpresasIds = empresasIds;
            ViewBag.CodigoIds = codigoIds;
            ViewBag.serieSapIds = serieSapIds;

            this.CargaControles();
            return View(modelo);
        }

        [Permiso("Control.Usuario.Editar")]
        public ActionResult Editar(long id)
        {
            Usuario UsuarioActual = new UsuarioBL().ObtenerPorId(id, true);

            if (UsuarioActual == null || UsuarioActual.UsuarioId == 0)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Usuario", "Editar");

            if (UsuarioActual.Roles != null && UsuarioActual.Roles.Count() > 0)
            {
                ViewBag.RolesIds = UsuarioActual.Roles.Select(x => x.RolId);
            }
            else
            {
                ViewBag.RolesIds = 0;
            }

            if (UsuarioActual.Agencias != null && UsuarioActual.Agencias.Count() > 0)
            {
                ViewBag.AgenciasIds = UsuarioActual.Agencias.Select(x => x.AgenciaId);
            }
            else
            {
                ViewBag.AgenciasIds = 0;
            }

            if (UsuarioActual.Empresas != null && UsuarioActual.Empresas.Count() > 0)
            {
                ViewBag.EmpresasIds = UsuarioActual.Empresas.Select(x => x.EmpresaId);
                ViewBag.CodigoIds = UsuarioActual.Empresas.Select(x => x.Codigo);
                ViewBag.serieSapIds = UsuarioActual.Empresas.Select(x => x.SERIE_SAP);
            }
            else
            {
                ViewBag.EmpresasIds = 0;
                ViewBag.CodigoIds = "";
                ViewBag.serieSapIds = "";
            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = UsuarioActual.Activo == true ? strAtributo : "";
            ViewBag.ActivoNo = UsuarioActual.Activo == false ? strAtributo : "";

            ViewBag.TokenSi = UsuarioActual.Token == true ? strAtributo : "";
            ViewBag.TokenNo = UsuarioActual.Token == false ? strAtributo : "";

            this.CargaControles();
            return View(UsuarioActual);
        }

        [HttpPost]
        [Permiso("Control.Usuario.Editar")]
        public ActionResult Editar(Usuario modelo, int[] rolesIds, long[] agenciasIds, long[] empresasIds, string[] codigoIds,string [] serieSapIds, bool activo, bool token)
        {
            if (rolesIds == null || rolesIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un rol");
            }

            if (agenciasIds == null || agenciasIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos una agencia");
            }

            if (empresasIds == null || empresasIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos una empresa");
            }

            if (ModelState.IsValid)
            {
                modelo.Roles = new List<UsuarioRol>();
                for (int i = 0; i < rolesIds.Length; i++)
                {
                    modelo.Roles.Add(new UsuarioRol() { RolId = rolesIds[i] });
                }

                modelo.Agencias = new List<UsuarioAgencia>();
                for (int i = 0; i < agenciasIds.Length; i++)
                {
                    modelo.Agencias.Add(new UsuarioAgencia() { AgenciaId = agenciasIds[i] });
                }

                modelo.Empresas = new List<UsuarioEmpresa>();
                if (serieSapIds == null)
                {
                    throw new Exception("serieSapIds viene NULL");
                }
                if (codigoIds == null)
                {
                    throw new Exception("codigoIds viene NULL");
                }
                for (int i = 0; i < empresasIds.Length; i++)
                {
                    modelo.Empresas.Add(new UsuarioEmpresa() { EmpresaId = empresasIds[i], Codigo = codigoIds[i], SERIE_SAP = serieSapIds[i] });
                }

                modelo.AutenticarSite = true;
                modelo.AutenticarAndroid = true;
                modelo.Activo = activo;
                modelo.Token = token;
                modelo.ReiniciarPassword = false;

                string strMensaje = new UsuarioBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Usuario-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = activo == true ? strAtributo : "";
            ViewBag.ActivoNo = activo == false ? strAtributo : "";

            ViewBag.TokenSi = token == true ? strAtributo : "";
            ViewBag.TokenNo = token == false ? strAtributo : "";

            ViewBag.RolesIds = rolesIds;
            ViewBag.AgenciasIds = agenciasIds;            

            this.CargaControles();
            return View(modelo);
        }

        [ActionName("AutorizarPrecioSupervisor")]
        [HttpPost]
        public JsonResult AutorizarPrecioSupervisor(string usuario, string password)
        {
            if (!string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(password))
            {
                return Json(new { Operacion = new UsuarioBL().Autorizar(usuario, password) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerVendedoresSap")]
        public JsonResult ObtenerVendedoresSap( long id)
        {
             
                List<ModelSale> Productos = new UsuarioBL().BuscarVendedoresxEmpresa(id);
            if (Productos != null && Productos.Count() > 0)
            {

                IList _result = new List<SelectListItem>();
                    _result = Productos.Select(m => new SelectListItem() { Text = m.Codigo, Value = m.Codigo }).ToList();
                    return Json(_result, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Operacion = false }, JsonRequestBehavior.AllowGet);
        }
    }

    
}