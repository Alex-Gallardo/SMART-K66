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
    public class ReporteController : Controller
    {

        #region Metodos Privados

            private void CargaControles()
            {
                var Centros = new AgenciaBL().ObtenerListado(false, CustomHelper.getUserId());

                if (Centros != null && Centros.Count() > 0)
                {
                    Centros.Insert(0, new Agencia() { AgenciaId = 0, Nombre = "General" });
                }

                ViewBag.Centros = new SelectList(Centros, "AgenciaId", "Nombre");
            }

            private void CargaPrecios()
            {
                var Precios = new PrecioBL().ObtenerListado();
                ViewBag.Precios = new SelectList(Precios, "PrecioId", "Nombre");
            }

            private void CargaProveedores()
            {
                var Proveedores = new ProveedorBL().ObtenerListado(false);
                ViewBag.Proveedores = new SelectList(Proveedores, "ProveedorId", "Nombre");
            }

            private void CargaProductos()
            {
                var Productos = new ProductoBL().ObtenerListado(true, false, true);
                ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
            }

            private void CargaPersonal()
            {
                var Personals = new PersonalBL().ObtenerListado(true, false);

                ViewBag.Personals = new SelectList(Personals, "PersonalId", "Nombre");
            }

        #endregion

        // GET: Reporte
        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario()
        {
            CustomHelper.setTitle("Inventario", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.InventarioxPresentacion")]
        public ActionResult InventarioxPresentacion()
        {
            CustomHelper.setTitle("Inventario x Presentación", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.InventarioTransito")]
        public ActionResult InventarioTransito()
        {
            CustomHelper.setTitle("Inventario Transito", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.InventarioxPrecioVenta")]
        public ActionResult InventarioxPrecioVenta()
        {
            CustomHelper.setTitle("Inventario x Precio Venta", "Reporte");

            this.CargaControles();
            this.CargaPrecios();
            return View();
        }

        [Permiso("Control.Reporte.Cierre")]
        public ActionResult Cierre()
        {
            CustomHelper.setTitle("Cierre del Día", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.Ingreso")]
        public ActionResult Ingreso()
        {
            CustomHelper.setTitle("Ingreso", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.IngresoxProveedor")]
        public ActionResult IngresoxProveedor()
        {
            CustomHelper.setTitle("Ingreso x Proveedor", "Reporte");

            this.CargaControles();
            this.CargaProveedores();
            return View();
        }

        [Permiso("Control.Reporte.IngresoxProducto")]
        public ActionResult IngresoxProducto()
        {
            CustomHelper.setTitle("Ingreso x Producto", "Reporte");

            this.CargaControles();
            this.CargaProductos();
            return View();
        }

        [Permiso("Control.Reporte.Egreso")]
        public ActionResult Egreso()
        {
            CustomHelper.setTitle("Egreso", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia()
        {
            CustomHelper.setTitle("Ganancia", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.Diario")]
        public ActionResult Diario()
        {
            CustomHelper.setTitle("Libro Diario", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.Mayor")]
        public ActionResult Mayor()
        {
            CustomHelper.setTitle("Libro Mayor", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.BalanceSaldo")]
        public ActionResult Balance_Saldo()
        {
            CustomHelper.setTitle("Balance de Saldos", "Reporte");

            this.CargaControles();
            return View();
        }

        [Permiso("Control.Reporte.Personal_Horario_General")]
        public ActionResult Horario_General()
        {
            CustomHelper.setTitle("Horario General del Personal", "Reporte");
            return View();
        }

        [Permiso("Control.Reporte.Personal_Horario")]
        public ActionResult Horario()
        {
            CustomHelper.setTitle("Horario Personal", "Reporte");

            this.CargaPersonal();
            return View();
        }
    }
}