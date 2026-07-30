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
    public class Producto_CategoriaController : Controller
    {
        // GET: Producto Categoria
        [Permiso("Control.Producto_Categoria.Ver_Listado")]
        public ActionResult Index(int? page, string search)
        {
            CustomHelper.setTitle("Categoría de Producto", "Listado");

            List<ProductoCategoria> ProductoCategorias = new List<ProductoCategoria>();

            try
            {
                if (!string.IsNullOrWhiteSpace(search) && search != null)
                {
                    ProductoCategorias = new ProductoCategoriaBL().Buscar(search).ToList();
                }
                else
                {
                    ProductoCategorias = new ProductoCategoriaBL().ObtenerListado().ToList();
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
            return View(ProductoCategorias.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Producto_Categoria.Crear")]
        public ActionResult Crear()
        {
            CustomHelper.setTitle("Categoría de Producto", "Nueva");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = strAtributo;
            ViewBag.activoNo = "";

            return View();
        }

        [Permiso("Control.Producto_Categoria.Crear")]
        [HttpPost]
        public ActionResult Crear(ProductoCategoria modelo, bool activo, HttpPostedFileBase fotografiaApp)
        {
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

            if (ModelState.IsValid)
            {
                modelo.Activo = activo;
                string strMensaje = new ProductoCategoriaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto_Categoria-Success"] = strMensaje;
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

            return View(modelo);
        }

        [Permiso("Control.Producto_Categoria.Editar")]
        public ActionResult Editar(long id)
        {
            ProductoCategoria ProductoCategoriaActual = new ProductoCategoriaBL().ObtenerPorId(id);

            if (ProductoCategoriaActual == null)
            {
                return HttpNotFound();
            }

            CustomHelper.setTitle("Categoría de Producto", "Editar");

            string strAtributo = "checked='checked'";

            ViewBag.activoSi = ProductoCategoriaActual.Activo == true ? strAtributo : "";
            ViewBag.activoNo = ProductoCategoriaActual.Activo == false ? strAtributo : "";

            return View(ProductoCategoriaActual);
        }

        [Permiso("Control.Producto_Categoria.Editar")]
        [HttpPost]
        public ActionResult Editar(ProductoCategoria modelo, bool activo, HttpPostedFileBase fotografiaApp)
        {
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

            if (ModelState.IsValid)
            {
                string strMensaje = new ProductoCategoriaBL().Guardar(modelo);

                if (strMensaje.Equals("OK"))
                {
                    TempData["Producto_Categoria-Success"] = strMensaje;
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

            return View(modelo);
        }
    }
}