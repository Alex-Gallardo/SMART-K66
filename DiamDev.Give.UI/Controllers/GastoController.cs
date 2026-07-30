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
using System.Data;
using Microsoft.Reporting.WebForms;
using DiamDev.Give.DAL;
using System.Data.Entity;

namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class GastoController : Controller
    {
        #region Metodos Privados

            private void CargaControles()
            {
                var Proveedores = new ProveedorBL().ObtenerListado(false);
                var Tipos = new TipoCompraBL().ObtenerListado();
                var Categorias = new CategoriaGastoBL().ObtenerListado(false);

                ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
                ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
                ViewBag.Categorias = new SelectList(Categorias, "CategoriaId", "Nombre");
            }

        #endregion

        #region Metodos Publicos

            public FileResult Preview(int id, long gastoId)
            {
                GastoFotografia FotografiaActual = new GastoBL().Fotografia(id, gastoId);

                var content = Binario.Drawing.ImageManager.GetThumbnail(FotografiaActual.Content, 100);
                return File(content, FotografiaActual.ContentType);
            }

            public FileResult Imagen(int id, long gastoId)
            {
                GastoFotografia FotografiaActual = new GastoBL().Fotografia(id, gastoId);

                return File(FotografiaActual.Content, FotografiaActual.ContentType);
            }

        #endregion

        // GET: Gasto
        [Permiso("Control.Gasto.Ver_Listado")]
        public ActionResult Index(DateTime? FechaInicial, DateTime? FechaFinal)
        {
            CustomHelper.setTitle("Gasto", "Listado");
            List<Gasto> Gastos = new List<Gasto>();
         
            try
            {
                if (!FechaInicial.HasValue && !FechaFinal.HasValue)
                {
                    FechaInicial = DateTime.Today;
                    FechaFinal = DateTime.Today;
                }

                Gastos = new GastoBL().ObtenerListadoxFecha(FechaInicial.Value, FechaFinal.Value, CustomHelper.getUserId()).ToList();            
            }
            catch (Exception)
            {}
           
            return View(Gastos);
        }

        [Permiso("Control.Gasto.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Gasto", "Nuevo");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Gasto.Crear")]
        [HttpPost]
        public ActionResult Crear(Gasto modelo, ArchivoModel[] archivos)
        {
            if (archivos != null && archivos.Count() > 0)
            {
                modelo.Fotografias = new List<GastoFotografia>();
                foreach (ArchivoModel archivo in archivos)
                {
                    byte[] FileData = new byte[archivo.Archivo.ContentLength + 1];
                    archivo.Archivo.InputStream.Read(FileData, 0, archivo.Archivo.ContentLength);
                    modelo.Fotografias.Add(new GastoFotografia() { Nombre = archivo.Archivo.FileName, Content = FileData, ContentType = archivo.Archivo.ContentType, Length = archivo.Archivo.ContentLength });
                }
            }

            modelo.AgenciaId = CustomHelper.getAgenciaId();
            modelo.UsrCreo = CustomHelper.getUserId();       

            if (ModelState.IsValid)
            {
                string strMensaje = new GastoBL().Guardar(modelo);
                if (strMensaje.Equals("OK"))
                {
                    TempData["Gasto-Success"] = strMensaje;
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

        [Permiso("Control.Gasto.Detalle")]
        public ActionResult Detalle(long id)
        {
            Gasto GastoActual = new GastoBL().ObtenerPorId(id, true, true);

            if (GastoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Gasto", "Detalle");

            return View(GastoActual);
        }

        [Permiso("Control.Gasto.Anular")]
        public ActionResult Anular(long id)
        {
            Gasto GastoActual = new GastoBL().ObtenerPorId(id, true, true);

            if (GastoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Gasto", "Anular");

            return View(GastoActual);
        }

        [Permiso("Control.Gasto.Anular")]
        [HttpPost]
        public ActionResult Anular(long gastoId, string comentario)
        {
            string strMensaje = new GastoBL().Anular(gastoId, comentario, CustomHelper.getUserId());
            if (strMensaje.Equals("OK"))
            {
                TempData["Gasto_Anular-Success"] = strMensaje;
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", strMensaje);
            }

            Gasto GastoActual = new GastoBL().ObtenerPorId(gastoId, true, true);

            if (GastoActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Gasto", "Anular");

            return View(GastoActual);
        }
    }
}