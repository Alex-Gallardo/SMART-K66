using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using PagedList;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class RolController : Controller
    {
        #region Metodos Privados

            private List<Permiso> Permisos()
            {
                return new PermisoBL().ObtenerListado();
            }

        #endregion

        // GET: Rol
        [Permiso("Control.Rol.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Rol", "Listado");

            List<Rol> Roles = new List<Rol>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    Roles = new RolBL().Buscar(search);
                }
                else
                {
                    Roles = new RolBL().ObtenerListado().ToList();
                }
            }
            catch (Exception)
            {
            }

            ViewBag.Search = search;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Roles.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Rol.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Rol", "Nuevo");

            return View(new Rol() { PermisoIds = Permisos() });
        }

        [HttpPost]
        [Permiso("Control.Rol.Crear")]
        public ActionResult Crear(Rol modelo, string[] permisosIds)
        {
            if (permisosIds == null || permisosIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un permiso");
            }

            modelo.Permisos = new List<RolPermiso>();
            for (int i = 0; i < permisosIds.Length; i++)
            {
                RolPermiso Permiso = new RolPermiso();
                Permiso.PermisoId = permisosIds[i];
                modelo.Permisos.Add(Permiso);
            }

            if (ModelState.IsValid)
            {               
                string strMensaje = new RolBL().Guardar(modelo);

                if (strMensaje.Contains("OK"))
                {
                    TempData["Rol-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            modelo.PermisoIds = Permisos();
            return View(modelo);
        }

        [Permiso("Control.Rol.Editar")]
        public ActionResult Editar(int id)
        {
            Rol RolActual = new RolBL().ObtenerPorId(id);

            if (RolActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Rol", "Editar");

            RolActual.PermisoIds = Permisos();
            return View(RolActual);
        }

        [HttpPost]
        [Permiso("Control.Rol.Editar")]
        public ActionResult Editar(Rol modelo, string[] permisosIds)
        {
            if (permisosIds == null || permisosIds.Length == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un permiso");
            }
                       
            modelo.Permisos = new List<RolPermiso>();
            for (int i = 0; i < permisosIds.Length; i++)
            {
                RolPermiso Permiso = new RolPermiso();
                Permiso.PermisoId = permisosIds[i];
                modelo.Permisos.Add(Permiso);
            }

            if (ModelState.IsValid)
            {
                string strMensaje = new RolBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Rol-Success"] = strMensaje;
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", strMensaje);
                }
            }

            modelo.PermisoIds = Permisos();
            return View(modelo);
        }
    }
}