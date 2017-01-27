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
                var Roles = new RolBL().ObtenerListado();
                var Agencias = new AgenciaBL().ObtenerListado(false);

                ViewBag.Roles = new SelectList(Roles, "RolId", "Nombre");
                ViewBag.Agencias = new SelectList(Agencias, "AgenciaId", "Nombre");
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

            ViewBag.ReiniciarPasswordSi = "";
            ViewBag.ReiniciarPasswordNo = strAtributo;

            ViewBag.AutenticarSiteSi = strAtributo;
            ViewBag.AutenticarSiteNo = "";

            ViewBag.AutenticarAndroidSi = "";
            ViewBag.AutenticarAndroidNo = strAtributo;

            this.CargaControles();
            return View();
        }

        [HttpPost]
        [Permiso("Control.Usuario.Crear")]
        public ActionResult Crear(Usuario modelo, int[] rolesIds, long[] agenciasIds, bool autenticarSite, bool autenticarAndroid, bool activo, bool reiniciarPassword)
        {
            if (rolesIds == null || rolesIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un rol");
            }

            if (agenciasIds == null || agenciasIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos una agencia");
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

                modelo.AutenticarSite = autenticarSite;
                modelo.AutenticarAndroid = autenticarAndroid;
                modelo.Activo = activo;
                modelo.ReiniciarPassword = reiniciarPassword;  
           
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

            ViewBag.ReiniciarPasswordSi = reiniciarPassword == true ? strAtributo : "";
            ViewBag.ReiniciarPasswordNo = reiniciarPassword == false ? strAtributo : "";

            ViewBag.AutenticarSiteSi = autenticarSite == true ? strAtributo : "";
            ViewBag.AutenticarSiteNo = autenticarSite == false ? strAtributo : "";

            ViewBag.AutenticarAndroidSi = autenticarAndroid == true ? strAtributo : "";
            ViewBag.AutenticarAndroidNo = autenticarAndroid == false ? strAtributo : "";

            ViewBag.RolesIds = rolesIds;
            ViewBag.AgenciasIds = agenciasIds;

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

            string strAtributo = "checked='checked'";

            ViewBag.ActivoSi = UsuarioActual.Activo == true ? strAtributo : "";
            ViewBag.ActivoNo = UsuarioActual.Activo == false ? strAtributo : "";

            ViewBag.ReiniciarPasswordSi = UsuarioActual.ReiniciarPassword == true ? strAtributo : "";
            ViewBag.ReiniciarPasswordNo = UsuarioActual.ReiniciarPassword == false ? strAtributo : "";

            ViewBag.AutenticarSiteSi = UsuarioActual.AutenticarSite == true ? strAtributo : "";
            ViewBag.AutenticarSiteNo = UsuarioActual.AutenticarSite == false ? strAtributo : "";

            ViewBag.AutenticarAndroidSi = UsuarioActual.AutenticarAndroid == true ? strAtributo : "";
            ViewBag.AutenticarAndroidNo = UsuarioActual.AutenticarAndroid == false ? strAtributo : "";

            this.CargaControles();
            return View(UsuarioActual);
        }

        [HttpPost]
        [Permiso("Control.Usuario.Editar")]
        public ActionResult Editar(Usuario modelo, int[] rolesIds, long[] agenciasIds, bool autenticarSite, bool autenticarAndroid, bool activo, bool reiniciarPassword)
        {
            if (rolesIds == null || rolesIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un rol");
            }

            if (agenciasIds == null || agenciasIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos una agencia");
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

                modelo.AutenticarSite = autenticarSite;
                modelo.AutenticarAndroid = autenticarAndroid;
                modelo.Activo = activo;
                modelo.ReiniciarPassword = reiniciarPassword;

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

            ViewBag.ReiniciarPasswordSi = reiniciarPassword == true ? strAtributo : "";
            ViewBag.ReiniciarPasswordNo = reiniciarPassword == false ? strAtributo : "";

            ViewBag.RolesIds = rolesIds;
            ViewBag.AgenciasIds = agenciasIds;

            ViewBag.AutenticarSiteSi = autenticarSite == true ? strAtributo : "";
            ViewBag.AutenticarSiteNo = autenticarSite == false ? strAtributo : "";

            ViewBag.AutenticarAndroidSi = autenticarAndroid == true ? strAtributo : "";
            ViewBag.AutenticarAndroidNo = autenticarAndroid == false ? strAtributo : "";

            this.CargaControles();
            return View(modelo);
        }
    }
}