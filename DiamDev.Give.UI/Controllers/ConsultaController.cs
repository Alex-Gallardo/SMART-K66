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
    public class ConsultaController : Controller
    {
        // GET: Consulta
        [Permiso("Control.Consulta_Reservado_Pendiente_Compra.Ver_Listado")]
        public ActionResult Reservado_Pendiente_Compra(int? page)
        {
            CustomHelper.setTitle("Producto Pendiente de Compra - Reservado", "Consulta");

            List<ReporteProductoReservaPendienteCompra> Productos = new List<ReporteProductoReservaPendienteCompra>();

            try
            {
                Productos = new ProductoBL().ReporteProductoReservaPendienteCompra();
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Productos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Consulta_Producto_Stock_Maximo.Ver_Listado")]
        public ActionResult Producto_Stock_Maximo(int? page)
        {
            CustomHelper.setTitle("Producto Stock Maximo", "Consulta");

            List<ProductoStock> Productos = new List<ProductoStock>();

            try
            {
                Productos = new ProductoBL().ConsultaStockMaximo(CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Productos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Consulta_Producto_Stock_Minimo.Ver_Listado")]
        public ActionResult Producto_Stock_Minimo(int? page)
        {
            CustomHelper.setTitle("Producto Stock Minimo", "Consulta");

            List<ProductoStock> Productos = new List<ProductoStock>();

            try
            {
                Productos = new ProductoBL().ConsultaStockMinimo(CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Productos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Consulta_Productos_x_Cliente.Ver_Listado")]
        public ActionResult Productos_x_Cliente(int? page, long? ClienteId, string ProductoId)
        {
            CustomHelper.setTitle("Productos x Cliente", "Consulta");

            List<ProductosxCliente> Ventas = new List<ProductosxCliente>();

            try
            {
                if (ClienteId != null && ProductoId != null)
                {
                    Ventas = new ReciboBL().ProductosxCliente(CustomHelper.getAgenciaId(), ClienteId.Value, ProductoId);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            ViewBag.ClienteId = ClienteId;
            ViewBag.ProductoId = ProductoId;

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Ventas.ToPagedList(pageNumber, pageSize));
        }

        //Morosidad

        [Permiso("Control.Consulta_Analisis_x_Morosidad.Ver_Listado")]
        public ActionResult Analisis_x_Morosidad()
        {
            CustomHelper.setTitle("Analisis x Morosidad", "Consulta");

            return View();
        }

        [Permiso("Control.Consulta_Morosidad_Critica.Ver_Listado")]
        public ActionResult Morosidad_Critica(int? page)
        {
            CustomHelper.setTitle("Morosidad Critica", "Consulta");

            List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

            try
            {
                Recibos = new ReciboBL().ConsultaMorosidadCritica(CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Consulta_Morosidad_Alta.Ver_Listado")]
        public ActionResult Morosidad_Alta(int? page)
        {
            CustomHelper.setTitle("Morosidad Alta", "Consulta");

            List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

            try
            {
                Recibos = new ReciboBL().ConsultaMorosidadAlta(CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Consulta_Morosidad_Media.Ver_Listado")]
        public ActionResult Morosidad_Media(int? page)
        {
            CustomHelper.setTitle("Morosidad Media", "Consulta");

            List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

            try
            {
                Recibos = new ReciboBL().ConsultaMorosidadMedia(CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }

        [Permiso("Control.Consulta_Cuenta_x_Cobrar_Al_Dia.Ver_Listado")]
        public ActionResult Cuenta_x_Cobrar_al_Dia(int? page)
        {
            CustomHelper.setTitle("Cuenta x Cobrar al Dia", "Consulta");

            List<ReciboMorosidad> Recibos = new List<ReciboMorosidad>();

            try
            {
                Recibos = new ReciboBL().ConsultaMorosidadBaja(CustomHelper.getAgenciaId());
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Format("Message: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
                return View("~/Views/Shared/Error.cshtml");
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(Recibos.ToPagedList(pageNumber, pageSize));
        }


        [ActionName("ObtenerCantidadReservadaPendienteCompra")]
        public JsonResult ObtenerCantidadReservadaPendienteCompra()
        {
            return Json(new { Operacion = true, Data = new ProductoBL().CantidadReservaPendienteCompra() }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoStockMaximo")]
        public JsonResult ObtenerProductoStockMaximo()
        {
            return Json(new { Operacion = true, Data = new ProductoBL().CantidadConsultaStockMaximo(CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerProductoStockMinimo")]
        public JsonResult ObtenerProductoStockMinimo()
        {
            return Json(new { Operacion = true, Data = new ProductoBL().CantidadConsultaStockMinimo(CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }

        //Morosidad

        [ActionName("ObtenerMorosidadCritica")]
        public JsonResult ObtenerMorosidadCritica()
        {
            return Json(new { Operacion = true, Data = new ReciboBL().CantidadConsultaMorosidadCritica(CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerMorosidadAlta")]
        public JsonResult ObtenerMorosidadAlta()
        {
            return Json(new { Operacion = true, Data = new ReciboBL().CantidadConsultaMorosidadAlta(CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerMorosidadMedia")]
        public JsonResult ObtenerMorosidadMedia()
        {
            return Json(new { Operacion = true, Data = new ReciboBL().CantidadConsultaMorosidadMedia(CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }

        [ActionName("ObtenerCuentaxCobrar")]
        public JsonResult ObtenerCuentaxCobrar()
        {
            return Json(new { Operacion = true, Data = new ReciboBL().CantidadConsultaMorosidadBaja(CustomHelper.getAgenciaId()) }, JsonRequestBehavior.AllowGet);
        }
    }
}