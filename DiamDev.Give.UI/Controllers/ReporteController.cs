using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.App_Start;
using DiamDev.Give.UI.Models;
using PagedList;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using DiamDev.Give.BLL;
using System.IO;
using System.Configuration;
using System.Data; 

    
namespace DiamDev.Give.UI.Controllers
{
    [Authorize]
    [Seguridad]
    [HandleError]
    public class ReporteController : Controller
    {

        #region Metodos Privados

        private void CargaControles(bool pCargaCentroxUsuario, bool centroGeneral = true)
        {
            var Centros = new AgenciaBL().ObtenerListado(pCargaCentroxUsuario, CustomHelper.getUserId());

            if (centroGeneral)
            {
                if (Centros != null && Centros.Count() > 0)
                {
                    Centros.Insert(0, new Agencia() { AgenciaId = 0, Nombre = "General" });
                }
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

        private void CargaMarcas()
        {
            var Marcas = new MarcaBL().ObtenerListado(false);
            ViewBag.Marcas = new SelectList(Marcas, "MarcaId", "Nombre");
        }

        private void CargarPersonal()
        {
            var Personal = new PersonalBL().ObtenerListado(false);
            ViewBag.Personals = new SelectList(Personal, "PersonalId", "Nombre");
        }

        private void CargarProductoCategorias()
        {
            var Categorias = new ProductoCategoriaBL().ObtenerListado(false);
            ViewBag.Categorias = new SelectList(Categorias, "ProductoCategoriaId", "Nombre");
        }

        private void cargaVendedores()
        {
            var Vendedores = new VendedorBL().ObtenerVendedoresPorAgencia(CustomHelper.getAgenciaId());
            ViewBag.Vendedores = new SelectList(Vendedores, "VendedorId", "Nombre");
        }

        private void cargaUsuarios()
        {
            var Usuarios = new UsuarioBL().ObtenerUsuarioxAgenciaId(CustomHelper.getAgenciaId());
            ViewBag.Usuarios = new SelectList(Usuarios, "UsuarioId", "Nombre");
        }

        private void cargaTransportes()
        {
            var Transportes = new TransporteBL().ObtenerListado();
            ViewBag.Transportes = new SelectList(Transportes, "TransporteId", "Nombre");
        }

        private void cargaProductosIDs()
        {
            var Productos = new ProductoBL().ObtenerProductosConIDs();
            ViewBag.Productos = new SelectList(Productos, "ProductoId", "Nombre");
        }

        private void cargaTiposDeClientes()
        {
            var Tipos = new ClienteTipoBL().ObtenerListado();
            ViewBag.Tipos = new SelectList(Tipos, "TipoId", "Nombre");
        }

        private void cargaTecnicos()
        {
            var Usuarios = new UsuarioBL().ObtenerTecnicos();
            ViewBag.Usuarios = new SelectList(Usuarios, "UsuarioId", "Nombre");
        }

        private void cargaFormas()
        {
            var Formas = new FormaPagoBL().ObtenerListado(false);
            ViewBag.Formas = new SelectList(Formas, "FormaPagoId", "Nombre");
        }

        private void cargaEstadosReserva()
        {
            var Estados = new List<ComboModel>() { new ComboModel() { ID = 1, Nombre = "Sí" }, new ComboModel() { ID = 2, Nombre = "No" } };
            ViewBag.Estados = new SelectList(Estados, "ID", "Nombre");
        }

        private void cargaCategoriaGastos()
        {
            var Categorias = new CategoriaGastoBL().ObtenerListado(false);
            ViewBag.Categorias = new SelectList(Categorias, "CategoriaId", "Nombre");
        }

        #endregion

        // GET: Reporte
        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario()
        {
            CustomHelper.setTitle("Inventario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        #region Crystal Reports — Utilitarios

        /// <summary>
        /// Aplica las credenciales HANA a todas las tablas del .rpt
        /// SIN reemplazar el objeto ConnectionInfo completo.
        /// Reemplazarlo borra el tipo de driver (B1CRHPROXY) y Crystal falla.
        /// </summary>
        private void AplicarConexionHana(ReportDocument reporte, string databaseOverride = null)
        {
            string servidor = ConfigurationManager.AppSettings["HANA_Server"];
            string baseDatos = databaseOverride ?? ConfigurationManager.AppSettings["HANA_Database"];
            string usuario = ConfigurationManager.AppSettings["HANA_User"];
            string password = ConfigurationManager.AppSettings["HANA_Password"];

            SetCredenciales(reporte, servidor, baseDatos, usuario, password);

            foreach (ReportDocument sub in reporte.Subreports)
                SetCredenciales(sub, servidor, baseDatos, usuario, password);
        }

        /// <summary>
        /// Aplica credenciales HANA usando 4 claves de AppSettings independientes.
        /// Útil para reportes que conectan a un schema HANA distinto (ej: APK66/SISTEMAS).
        /// </summary>
        private void AplicarConexionHanaConClaves(ReportDocument reporte,
            string keyServer, string keyDatabase, string keyUser, string keyPassword)
        {
            string servidor = ConfigurationManager.AppSettings[keyServer];
            string baseDatos = ConfigurationManager.AppSettings[keyDatabase];
            string usuario = ConfigurationManager.AppSettings[keyUser];
            string password = ConfigurationManager.AppSettings[keyPassword];

            SetCredenciales(reporte, servidor, baseDatos, usuario, password);

            foreach (ReportDocument sub in reporte.Subreports)
                SetCredenciales(sub, servidor, baseDatos, usuario, password);
        }

        private void SetCredenciales(ReportDocument rpt,
            string servidor, string db, string user, string pwd)
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table tabla in rpt.Database.Tables)
            {
                TableLogOnInfo logOn = tabla.LogOnInfo;

                // ⚠️ Asignamos propiedad por propiedad — NO reemplazamos el objeto.
                //    Esto conserva el driver B1CRHPROXY que el .rpt trae embebido.
                logOn.ConnectionInfo.ServerName = servidor;   // sapserver:30013
                logOn.ConnectionInfo.DatabaseName = db;         // SBOESCOCESA
                logOn.ConnectionInfo.UserID = user;       // SYSTEM
                logOn.ConnectionInfo.Password = pwd;

                tabla.ApplyLogOnInfo(logOn);
            }
        }

        /// <summary>
        /// Aplica las credenciales SQL Server a todas las tablas del .rpt
        /// Lee la cadena de conexión desde Web.config por nombre.
        /// Mismo patrón que AplicarConexionHana — propiedad por propiedad.
        /// </summary>
        private void AplicarConexionSql(ReportDocument reporte,
            string connectionStringName = "GiveContext")
        {
            var cs = System.Configuration.ConfigurationManager
                         .ConnectionStrings[connectionStringName].ConnectionString;

            var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(cs);

            string servidor = builder.DataSource;          // K66-APPS
            string baseDatos = builder.InitialCatalog;       // POS-SmartK66_DEV
            string usuario = builder.UserID;               // sa
            string password = builder.Password;

            SetCredencialesSql(reporte, servidor, baseDatos, usuario, password);

            foreach (ReportDocument sub in reporte.Subreports)
                SetCredencialesSql(sub, servidor, baseDatos, usuario, password);
        }

        private void SetCredencialesSql(ReportDocument rpt,
            string servidor, string db, string user, string pwd)
        {
            foreach (CrystalDecisions.CrystalReports.Engine.Table tabla in rpt.Database.Tables)
            {
                TableLogOnInfo logOn = tabla.LogOnInfo;

                logOn.ConnectionInfo.ServerName = servidor;
                logOn.ConnectionInfo.DatabaseName = db;
                logOn.ConnectionInfo.UserID = user;
                logOn.ConnectionInfo.Password = pwd;

                tabla.ApplyLogOnInfo(logOn);
            }
        }

        /// <summary>
        /// Aplica credenciales HANA a un subreporte específico buscándolo por nombre exacto.
        /// ⚠️ El nombre debe coincidir con el de la pestaña del subreporte en Crystal Reports.
        /// Si no lo encuentra, no lanza excepción — solo loguea en Debug.
        /// Preserva el driver B1CRHPROXY del .rpt porque usa SetCredenciales (propiedad x propiedad).
        /// </summary>
        private void AplicarConexionHanaSubreporte(ReportDocument reporte,
            string nombreSubreporte,
            string keyServer, string keyDatabase, string keyUser, string keyPassword)
        {
            string servidor = ConfigurationManager.AppSettings[keyServer];
            string baseDatos = ConfigurationManager.AppSettings[keyDatabase];
            string usuario = ConfigurationManager.AppSettings[keyUser];
            string password = ConfigurationManager.AppSettings[keyPassword];

            bool encontrado = false;

            foreach (ReportDocument sub in reporte.Subreports)
            {
                // OrdinalIgnoreCase por si Crystal agrega espacios o cambia capitalización
                if (string.Equals(sub.Name, nombreSubreporte, StringComparison.OrdinalIgnoreCase))
                {
                    // SetCredenciales preserva el driver B1CRHPROXY — no reemplaza el objeto
                    SetCredenciales(sub, servidor, baseDatos, usuario, password);
                    encontrado = true;
                    break;
                }
            }

            if (!encontrado)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AplicarConexionHanaSubreporte] ADVERTENCIA: subreporte '{nombreSubreporte}' " +
                    $"no encontrado. Subreportes disponibles: " +
                    string.Join(", ", reporte.Subreports
                        .Cast<ReportDocument>()
                        .Select(s => $"'{s.Name}'")));
            }
        }

        private void AplicarConexionHanaSoloMainConClaves(
            ReportDocument reporte,
            string keyServer,
            string keyDatabase,
            string keyUser,
            string keyPassword)
                {
                    string servidor = ConfigurationManager.AppSettings[keyServer];
                    string baseDatos = ConfigurationManager.AppSettings[keyDatabase];
                    string usuario = ConfigurationManager.AppSettings[keyUser];
                    string password = ConfigurationManager.AppSettings[keyPassword];

                    SetCredenciales(reporte, servidor, baseDatos, usuario, password);
                }

        private string ResolverSchemaSapDesdeEmpresa(string empresa)
        {
            string emp = (empresa ?? "").Trim().ToUpperInvariant();

            switch (emp)
            {
                case "BOLIK":
                case "20210705001":
                    return "SBOBOLIK";

                case "FAES":
                case "20210705003":
                    return "SBOESCOCESA";

                case "GRACO":
                case "20210705004":
                    return "SBO_GRACO";

                default:
                    return "SBO_GRACO";
            }
        }

        private void AplicarConexionHanaB1Subreporte(
            ReportDocument reporte,
            string nombreSubreporte,
            string schemaSap)
        {
            string serverBase = ConfigurationManager.AppSettings["HANA_Server"];      // sapserver:30013
            string tenant = ConfigurationManager.AppSettings["HANA_TenantDB"];        // NDB
            string usuario = ConfigurationManager.AppSettings["HANA_User"];
            string password = ConfigurationManager.AppSettings["HANA_Password"];

            // En tus capturas Crystal muestra el ServerName como NDB@sapserver:30013
            string servidorCrystal = string.IsNullOrWhiteSpace(tenant)
                ? serverBase
                : tenant + "@" + serverBase;

            bool encontrado = false;

            foreach (ReportDocument sub in reporte.Subreports)
            {
                if (string.Equals(sub.Name, nombreSubreporte, StringComparison.OrdinalIgnoreCase))
                {
                    SetCredenciales(sub, servidorCrystal, schemaSap, usuario, password);
                    encontrado = true;
                    break;
                }
            }

            if (!encontrado)
            {
                throw new Exception(
                    "No se encontró el subreporte '" + nombreSubreporte + "'. Subreportes disponibles: " +
                    string.Join(", ", reporte.Subreports.Cast<ReportDocument>().Select(s => "'" + s.Name + "'")));
            }
        }

        /// <summary>
        /// Exporta el ReportDocument a PDF y lo devuelve inline en el browser.
        /// Llama a Close/Dispose siempre para evitar leaks de memoria en IIS.
        /// </summary>
        private FileResult ExportarPdf(ReportDocument rpt, string nombreArchivo)
        {
            Stream stream = rpt.ExportToStream(
                CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);

            rpt.Close();
            rpt.Dispose();

            Response.AddHeader("Content-Disposition",
                $"inline; filename={nombreArchivo}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

            return File(stream, "application/pdf");
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        //  REGIÓN: CRYSTAL REPORTS — HANA
        //  Patrón A: Los .rpt tienen SQL embebido con conexión B1CRHPROXY.
        //  Solo cambiamos las credenciales en runtime, conservando el driver.
        // ════════════════════════════════════════════════════════════════════════
        // ════════════════════════════════════════════════════════════════════════
        //  ACCIONES PÚBLICAS — Una por cada .rpt existente en Reports/Crystal/
        //  Los 3 reportes usan B1CRHPROXY con SQL Command embebido.
        //  No se usa SetDataSource; solo se sobreescriben las credenciales HANA.
        // ════════════════════════════════════════════════════════════════════════
        // ════════════════════════════════════════════════════════════════════════
        //  CRYSTAL REPORTS — HANA  (parámetros verificados con DiagParametros)
        // ════════════════════════════════════════════════════════════════════════
        public ActionResult DespachosEnRutaDia(
            string empresa = "",
            string agente = "",
            string fechaInicio = "",
            string fechaFin = "",
            string cliente = "",
            string pedido = "")
        {
            var rpt = new ReportDocument();

            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Reporte despachos en ruta.rpt"));

                // 1. Reporte principal: HANA APK66
                // Tu diagnóstico confirma que el main report usa 192.168.192.227 / APK66 / SISTEMAS.
                AplicarConexionHanaSoloMainConClaves(
                    rpt,
                    "HANA_Server_APK66",
                    "HANA_Database_APK66",
                    "HANA_User_APK66",
                    "HANA_Password_APK66");

                // 2. Subreporte: HANA SAP B1 vía B1CRHPROXY
                // El subreporte DatosFacturaHANA está diseñado contra SBO_GRACO,
                // pero lo cambiamos dinámicamente según la empresa seleccionada.
                string schemaSap = ResolverSchemaSapDesdeEmpresa(empresa);

                AplicarConexionHanaB1Subreporte(
                    rpt,
                    "DatosFacturaHANA",
                    schemaSap);

                // 3. Parámetros del reporte principal
                TrySetParametro(rpt, "Agente", string.IsNullOrWhiteSpace(agente) ? "*" : agente);
                TrySetParametro(rpt, "Empresa", string.IsNullOrWhiteSpace(empresa) ? "*" : empresa);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);

                if (string.IsNullOrWhiteSpace(fechaInicio) || string.IsNullOrWhiteSpace(fechaFin))
                    throw new Exception("Fecha Inicio y Fecha Fin son obligatorias.");

                TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicio));
                TrySetParametro(rpt, "Fecha Fin", Convert.ToDateTime(fechaFin));

                return ExportarPdf(rpt, "Despachos_En_Ruta_Dia");
            }
            catch (Exception ex)
            {
                rpt.Close();
                rpt.Dispose();
                return ContenidoError(ex, "Despachos en ruta dia");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DESPACHOS EN RUTA — un action por empresa
        //  Parámetros del .rpt (verificados con DiagParametros):
        //    Agente | Empresa | Fecha Inicio | Fecha Fin |
        //    Cliente | Pedido (No. Factura) | Pm-Comando.ID_DOCUMENTO (No. Pedido)
        // ══════════════════════════════════════════════════════════════════════

        public ActionResult DespachosEnRutaBolik(
            string agente = "",
            string fechaInicio = "",
            string fechaFin = "",
            string cliente = "",
            string pedido = "",       // No. Factura → parámetro "Pedido" del .rpt
            string idDocumento = "")       // No. Pedido  → parámetro "Pm-Comando.ID_DOCUMENTO"
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Reporte despachos en ruta Bolik.rpt"));

                AplicarConexionHanaSoloMainConClaves(rpt,
                    "HANA_Server_APK66", "HANA_Database_APK66",
                    "HANA_User_APK66", "HANA_Password_APK66");

                // Subreporte SAP B1 — schema BOLIK
                AplicarConexionHanaB1Subreporte(rpt, "DatosFacturaHANA",
                    ResolverSchemaSapDesdeEmpresa("BOLIK"));

                if (string.IsNullOrWhiteSpace(fechaInicio) || string.IsNullOrWhiteSpace(fechaFin))
                    throw new Exception("Fecha Inicio y Fecha Fin son obligatorias.");

                TrySetParametro(rpt, "Agente", string.IsNullOrWhiteSpace(agente) ? "*" : agente);
                TrySetParametro(rpt, "Empresa", "BOLIK");
                TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicio));
                TrySetParametro(rpt, "Fecha Fin", Convert.ToDateTime(fechaFin));
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);
                // TrySetParametro(rpt, "Pm-Comando.ID_DOCUMENTO", string.IsNullOrWhiteSpace(idDocumento) ? "*" : idDocumento);

                return ExportarPdf(rpt, "Despachos_Ruta_Bolik");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Despachos en Ruta Bolik"); }
        }

        public ActionResult DespachosEnRutaFaes(
            string agente = "",
            string fechaInicio = "",
            string fechaFin = "",
            string cliente = "",
            string pedido = "",
            string idDocumento = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Reporte despachos en ruta Faes.rpt"));

                AplicarConexionHanaSoloMainConClaves(rpt,
                    "HANA_Server_APK66", "HANA_Database_APK66",
                    "HANA_User_APK66", "HANA_Password_APK66");

                // Subreporte SAP B1 — schema FAES (= SBOESCOCESA en este entorno)
                AplicarConexionHanaB1Subreporte(rpt, "DatosFacturaHANA",
                    ResolverSchemaSapDesdeEmpresa("FAES"));

                if (string.IsNullOrWhiteSpace(fechaInicio) || string.IsNullOrWhiteSpace(fechaFin))
                    throw new Exception("Fecha Inicio y Fecha Fin son obligatorias.");

                TrySetParametro(rpt, "Agente", string.IsNullOrWhiteSpace(agente) ? "*" : agente);
                TrySetParametro(rpt, "Empresa", "FAES");
                TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicio));
                TrySetParametro(rpt, "Fecha Fin", Convert.ToDateTime(fechaFin));
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);
                // TrySetParametro(rpt, "Pm-Comando.ID_DOCUMENTO", string.IsNullOrWhiteSpace(idDocumento) ? "*" : idDocumento);

                return ExportarPdf(rpt, "Despachos_Ruta_Faes");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Despachos en Ruta Faes"); }
        }

        public ActionResult DespachosEnRutaGraco(
            string agente = "",
            string fechaInicio = "",
            string fechaFin = "",
            string cliente = "",
            string pedido = "",
            string idDocumento = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Reporte despachos en ruta Graco.rpt"));

                AplicarConexionHanaSoloMainConClaves(rpt,
                    "HANA_Server_APK66", "HANA_Database_APK66",
                    "HANA_User_APK66", "HANA_Password_APK66");

                // Subreporte SAP B1 — schema GRACO
                AplicarConexionHanaB1Subreporte(rpt, "DatosFacturaHANA",
                    ResolverSchemaSapDesdeEmpresa("GRACO"));

                if (string.IsNullOrWhiteSpace(fechaInicio) || string.IsNullOrWhiteSpace(fechaFin))
                    throw new Exception("Fecha Inicio y Fecha Fin son obligatorias.");

                TrySetParametro(rpt, "Agente", string.IsNullOrWhiteSpace(agente) ? "*" : agente);
                TrySetParametro(rpt, "Empresa", "GRACO");
                TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicio));
                TrySetParametro(rpt, "Fecha Fin", Convert.ToDateTime(fechaFin));
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);
                // TrySetParametro(rpt, "Pm-Comando.ID_DOCUMENTO", string.IsNullOrWhiteSpace(idDocumento) ? "*" : idDocumento);

                return ExportarPdf(rpt, "Despachos_Ruta_Graco");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Despachos en Ruta Graco"); }
        }

        // ── PARÁMETRO: Agente ────────────────────────────────────────────────────

        public ActionResult BackorderAgenteBolik(string agente = "",
                                         string cliente = "",
                                         string producto = "",
                                         string pedido = "",
                                         string estadoStock = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Backorder Agentes Bolik.rpt"));

                // Siempre SBOBOLIK — este .rpt es exclusivo de Bolik
                AplicarConexionHana(rpt, "SBOBOLIK");

                // Si el front manda "*" o vacío → todos los agentes
                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*"
                    ? "*"
                    : agente;

                TrySetParametro(rpt, "Agente", agenteParam);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Producto", string.IsNullOrWhiteSpace(producto) ? "*" : producto);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);
                TrySetParametro(rpt, "EstadoStock", string.IsNullOrWhiteSpace(estadoStock) ? "*" : estadoStock);

                string sufijo = agenteParam == "*" ? "General" : agenteParam;
                return ExportarPdf(rpt, $"Backorder_Bolik_{sufijo}");
            }
            catch (Exception ex)
            {
                rpt.Close(); rpt.Dispose();
                return ContenidoError(ex, "Backorder Agentes Bolik");
            }
        }

        public ActionResult BackorderAgenteGraco(string agente = "",
                                          string cliente = "",
                                          string producto = "",
                                          string pedido = "",
                                          string estadoStock = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Backorder Agentes Graco.rpt"));

                // Siempre SBO_GRACO — este .rpt es exclusivo de Graco
                AplicarConexionHana(rpt, "SBO_GRACO");

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*"
                    ? "*"
                    : agente;

                TrySetParametro(rpt, "Agente", agenteParam);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Producto", string.IsNullOrWhiteSpace(producto) ? "*" : producto);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);
                TrySetParametro(rpt, "EstadoStock", string.IsNullOrWhiteSpace(estadoStock) ? "*" : estadoStock);

                string sufijo = agenteParam == "*" ? "General" : agenteParam;
                return ExportarPdf(rpt, $"Backorder_Graco_{sufijo}");
            }
            catch (Exception ex)
            {
                rpt.Close(); rpt.Dispose();
                return ContenidoError(ex, "Backorder Agentes Graco");
            }
        }

        public ActionResult BackorderAgenteFaes(string agente = "",
                                          string cliente = "",
                                          string producto = "",
                                          string pedido = "",
                                          string estadoStock = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Backorder Agentes Faes.rpt"));

                // Siempre SBOFAES — este .rpt es exclusivo de Faes
                AplicarConexionHana(rpt, "SBOESCOCESA");

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*"
                    ? "*"
                    : agente;

                TrySetParametro(rpt, "Agente", agenteParam);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Producto", string.IsNullOrWhiteSpace(producto) ? "*" : producto);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);
                TrySetParametro(rpt, "EstadoStock", string.IsNullOrWhiteSpace(estadoStock) ? "*" : estadoStock);

                string sufijo = agenteParam == "*" ? "General" : agenteParam;
                return ExportarPdf(rpt, $"Backorder_Faes_{sufijo}");
            }
            catch (Exception ex)
            {
                rpt.Close(); rpt.Dispose();
                return ContenidoError(ex, "Backorder Agentes Faes");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── PARÁMETROS: FInicial + FFinal + Cliente ──────────────────────────────
        //  ESTADO DE CUENTA — un action por empresa (.rpt ya tiene param Agente)
        // ⚠️ Nombres EXACTOS del .rpt: FInicial / FFinal / Cliente (no FechaInicial/CardCode)
        // ══════════════════════════════════════════════════════════════════════
        public ActionResult EstadoDeCuentaBolik(string fechaInicial = "",
                                                 string fechaFinal = "",
                                                 string cliente = "",
                                                 string agente = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado de Cuenta Bolik.rpt"));
                AplicarConexionHana(rpt, "SBOBOLIK");

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "FInicial", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "FFinal", Convert.ToDateTime(fechaFinal));
                }
                if (!string.IsNullOrWhiteSpace(cliente))
                    TrySetParametro(rpt, "Cliente", cliente);

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*" ? "*" : agente;
                TrySetParametro(rpt, "Agente", agenteParam);

                return ExportarPdf(rpt, $"Estado_Cuenta_Bolik_{(string.IsNullOrWhiteSpace(cliente) ? "General" : cliente)}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Estado de Cuenta Bolik"); }
        }

        public ActionResult EstadoDeCuentaFaes(string fechaInicial = "",
                                                string fechaFinal = "",
                                                string cliente = "",
                                                string agente = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado de Cuenta Faes.rpt"));
                AplicarConexionHana(rpt, "SBOESCOCESA");

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "FInicial", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "FFinal", Convert.ToDateTime(fechaFinal));
                }
                if (!string.IsNullOrWhiteSpace(cliente))
                    TrySetParametro(rpt, "Cliente", cliente);

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*" ? "*" : agente;
                TrySetParametro(rpt, "Agente", agenteParam);

                return ExportarPdf(rpt, $"Estado_Cuenta_Faes_{(string.IsNullOrWhiteSpace(cliente) ? "General" : cliente)}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Estado de Cuenta Faes"); }
        }

        public ActionResult EstadoDeCuentaGraco(string fechaInicial = "",
                                                 string fechaFinal = "",
                                                 string cliente = "",
                                                 string agente = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado de Cuenta Graco.rpt"));
                AplicarConexionHana(rpt, "SBO_GRACO");

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "FInicial", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "FFinal", Convert.ToDateTime(fechaFinal));
                }
                if (!string.IsNullOrWhiteSpace(cliente))
                    TrySetParametro(rpt, "Cliente", cliente);

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*" ? "*" : agente;
                TrySetParametro(rpt, "Agente", agenteParam);

                return ExportarPdf(rpt, $"Estado_Cuenta_Graco_{(string.IsNullOrWhiteSpace(cliente) ? "General" : cliente)}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Estado de Cuenta Graco"); }
        }


        // ── PARÁMETROS: Cliente + Pedido, INVENTARIO GRACO, INVENTARIO ESCOCESA, INVENTARIO BOLIK  ─────────────────────────────────────────
        public ActionResult InventarioBolik(string Codigo_Producto = "", string Producto_Name = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Inventario Bolik.rpt"));
                AplicarConexionHana(rpt);

                TrySetParametro(rpt, "Codigo_Producto", string.IsNullOrWhiteSpace(Codigo_Producto) ? "*" : Codigo_Producto);
                TrySetParametro(rpt, "Producto_Name", string.IsNullOrWhiteSpace(Producto_Name) ? "*" : Producto_Name);

                return ExportarPdf(rpt, "Inventario_Bolik");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Inventario Bolik"); }
        }

        public ActionResult InventarioGraco(string Codigo_Producto = "", string Producto_Name = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Inventario Graco.rpt"));
                AplicarConexionHana(rpt);

                if (!string.IsNullOrWhiteSpace(Codigo_Producto))
                    TrySetParametro(rpt, "Codigo_Producto", Codigo_Producto);
                if (!string.IsNullOrWhiteSpace(Producto_Name))
                    TrySetParametro(rpt, "Producto_Name", Producto_Name);

                return ExportarPdf(rpt, "Inventario_Graco");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Inventario Graco"); }
        }

        public ActionResult InventarioEscocesa(string Codigo_Producto = "", string Producto_Name = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Inventario Escocesa.rpt"));
                AplicarConexionHana(rpt);

                if (!string.IsNullOrWhiteSpace(Codigo_Producto))
                    TrySetParametro(rpt, "Codigo_Producto", Codigo_Producto);
                if (!string.IsNullOrWhiteSpace(Producto_Name))
                    TrySetParametro(rpt, "Producto_Name", Producto_Name);

                return ExportarPdf(rpt, "Inventario_Escocesa");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Inventario Escocesa"); }
        }


        // ── PARÁMETROS: 6 campos (REVISION DE RUTAS) ────────────────────────────
        // ⚠️ Nombres con espacios y mayúsculas: "FECHA INICIAL", "FECHA FINAL", etc.

        public ActionResult EstadoPedido(string fechaInicial = "", string fechaFinal = "",
                          string vehiculo = "", string noRuta = "",
                          string agente = "", string documento = "",
                          string empresa = "")   // ← parámetro nuevo
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado Pedido.rpt"));
                AplicarConexionSql(rpt, "GiveContext");

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "FECHA INICIAL", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "FECHA FINAL", Convert.ToDateTime(fechaFinal));
                }

                TrySetParametro(rpt, "VEHICULO", string.IsNullOrWhiteSpace(vehiculo) ? "*" : vehiculo);
                TrySetParametro(rpt, "NO RUTA", string.IsNullOrWhiteSpace(noRuta) ? "*" : noRuta);
                TrySetParametro(rpt, "AGENTE", string.IsNullOrWhiteSpace(agente) ? "*" : agente);
                TrySetParametro(rpt, "DOCUMENTO", string.IsNullOrWhiteSpace(documento) ? "*" : documento);

                // ⚠️ Verifica que el valor que espera el .rpt coincida con lo que envía el select
                // (ej: "INDUSTRIAS BOLIK, S.A." o "*" para todos)
                TrySetParametro(rpt, "Empresa", string.IsNullOrWhiteSpace(empresa) ? "*" : empresa);

                return ExportarPdf(rpt, "Estado_Pedido");
            }
            catch (Exception ex)
            {
                rpt.Close(); rpt.Dispose();
                return ContenidoError(ex, "Estado Pedido");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DETALLE FACTURAS — un action por empresa, mismo .rpt, distinto schema
        //  ⚠️ Si "Detalle de Facturas General.rpt" no declara el param "Agente", TrySetParametro lo ignora sin error. Agrégalo al .rpt para filtrar.
        // ── DETALLE FACTURAS — Empresa + Fechas + Cliente + Codigo + Producto ──────
        // ══════════════════════════════════════════════════════════════════════
        public ActionResult DetalleFacturas(string empresa = "SBOBOLIK",
                                            string fechaInicial = "",
                                            string fechaFinal = "",
                                            string cliente = "",
                                            string codigo = "",
                                            string producto = "")
        {
            // Whitelist de empresas permitidas
            var empresaDb = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "SBOBOLIK",    "SBOBOLIK"    },
                { "SBOESCOCESA", "SBOESCOCESA" },
                { "SBO_GRACO",   "SBO_GRACO"   }
            };

            string dbName = empresaDb.ContainsKey(empresa) ? empresaDb[empresa] : "SBOBOLIK";
            var rpt = new ReportDocument();

            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Detalle de Facturas General.rpt"));
                AplicarConexionHana(rpt, dbName);   // ← pasa el schema de la empresa elegida

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "Fecha Final", Convert.ToDateTime(fechaFinal));
                }
                if (!string.IsNullOrWhiteSpace(cliente)) TrySetParametro(rpt, "Cliente", cliente);
                if (!string.IsNullOrWhiteSpace(codigo)) TrySetParametro(rpt, "Codigo", codigo);
                if (!string.IsNullOrWhiteSpace(producto)) TrySetParametro(rpt, "Producto", producto);

                return ExportarPdf(rpt, $"Detalle_Facturas_{dbName}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Detalle Facturas"); }
        }
      
        public ActionResult DetalleFacturasBolik(string fechaInicial = "",
                                                  string fechaFinal = "",
                                                  string cliente = "",
                                                  string codigo = "",
                                                  string producto = "",
                                                  string agente = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Detalle de Facturas General.rpt"));
                AplicarConexionHana(rpt);

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "Fecha Final", Convert.ToDateTime(fechaFinal));
                    TrySetParametro(rpt, "Empresa", "BOLIK");
                }
                if (!string.IsNullOrWhiteSpace(cliente)) TrySetParametro(rpt, "Cliente", cliente);
                if (!string.IsNullOrWhiteSpace(codigo)) TrySetParametro(rpt, "Codigo", codigo);
                if (!string.IsNullOrWhiteSpace(producto)) TrySetParametro(rpt, "Producto", producto);

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*" ? "*" : agente;
                TrySetParametro(rpt, "Agente", agenteParam);

                return ExportarPdf(rpt, "Detalle_Facturas_Bolik");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Detalle Facturas Bolik"); }
        }

        public ActionResult DetalleFacturasFaes(string fechaInicial = "",
                                                 string fechaFinal = "",
                                                 string cliente = "",
                                                 string codigo = "",
                                                 string producto = "",
                                                 string agente = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Detalle de Facturas General.rpt"));
                AplicarConexionHana(rpt);

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "Fecha Final", Convert.ToDateTime(fechaFinal));
                    TrySetParametro(rpt, "Empresa", "FAES");
                }
                if (!string.IsNullOrWhiteSpace(cliente)) TrySetParametro(rpt, "Cliente", cliente);
                if (!string.IsNullOrWhiteSpace(codigo)) TrySetParametro(rpt, "Codigo", codigo);
                if (!string.IsNullOrWhiteSpace(producto)) TrySetParametro(rpt, "Producto", producto);

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*" ? "*" : agente;
                TrySetParametro(rpt, "Agente", agenteParam);

                return ExportarPdf(rpt, "Detalle_Facturas_Faes");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Detalle Facturas Faes"); }
        }

        public ActionResult DetalleFacturasGraco(string fechaInicial = "",
                                                  string fechaFinal = "",
                                                  string cliente = "",
                                                  string codigo = "",
                                                  string producto = "",
                                                  string agente = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Detalle de Facturas General.rpt"));
                AplicarConexionHana(rpt);

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "Fecha Inicio", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "Fecha Final", Convert.ToDateTime(fechaFinal));
                    TrySetParametro(rpt, "Empresa", "GRACO");
                }
                if (!string.IsNullOrWhiteSpace(cliente)) TrySetParametro(rpt, "Cliente", cliente);
                if (!string.IsNullOrWhiteSpace(codigo)) TrySetParametro(rpt, "Codigo", codigo);
                if (!string.IsNullOrWhiteSpace(producto)) TrySetParametro(rpt, "Producto", producto);

                string agenteParam = string.IsNullOrWhiteSpace(agente) || agente == "*" ? "*" : agente;
                TrySetParametro(rpt, "Agente", agenteParam);

                return ExportarPdf(rpt, "Detalle_Facturas_Graco");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Detalle Facturas Graco"); }
        }

        // ── Helpers internos ──────────────────────────────────────────────────

        /// <summary>
        /// Intenta setear un parámetro Crystal sin explotar si no existe.
        /// Útil porque no siempre sabemos qué Parameter Fields tiene el .rpt.
        /// </summary>
        private void TrySetParametro(ReportDocument rpt, string nombre, object valor)
        {
            try { rpt.SetParameterValue(nombre, valor); }
            catch { /* El .rpt no declara ese parámetro — se ignora */ }
        }

        /// <summary>
        /// Devuelve un ContentResult con el error en HTML legible.
        /// Solo para desarrollo; en producción conectar con tu logger.
        /// </summary>
        private ContentResult ContenidoError(Exception ex, string reporte)
        {
            return Content(
                $"<h3 style='color:red;font-family:sans-serif'>" +
                $"Error al generar: {reporte}</h3>" +
                $"<pre style='font-size:12px'>{ex}</pre>",
                "text/html");
        }


        // ════════════════════════════════════════════════════════════════════════
        //  DIAGNÓSTICOS REPORTS — HANA
        //  - (verifica Estados de Reporte, DiagParametros)
        //  - Quitar en producción — solo para diagnóstico
        // ════════════════════════════════════════════════════════════════════════
        public ActionResult TestHana()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<style>body{font-family:monospace;padding:20px}</style>");
            sb.Append("<h2>Diagnóstico HANA</h2><hr/>");

            string driver = ConfigurationManager.AppSettings["HANA_Driver"] ?? "(no definido)";
            string server = ConfigurationManager.AppSettings["HANA_Server"] ?? "(no definido)";
            string tenant = ConfigurationManager.AppSettings["HANA_TenantDB"] ?? "(no definido)";
            string schema = ConfigurationManager.AppSettings["HANA_Database"] ?? "(no definido)";
            string user = ConfigurationManager.AppSettings["HANA_User"] ?? "(no definido)";

            sb.Append("<h4>Configuración</h4><ul>");
            sb.Append($"<li><b>Driver:</b>   {driver}</li>");
            sb.Append($"<li><b>Server:</b>   {server}</li>");
            sb.Append($"<li><b>TenantDB:</b> {tenant}  ← HANA database name</li>");
            sb.Append($"<li><b>Schema:</b>   {schema}  ← SAP B1 schema (va en el SQL)</li>");
            sb.Append($"<li><b>User:</b>     {user}</li>");
            sb.Append("</ul>");
            sb.Append($"<p><b>Proceso IIS:</b> {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}</p>");

            // Test 1: Conectar
            string error;
            bool ok = DiamDev.Give.DAL.HanaHelper.ProbarConexion(out error);

            if (ok)
            {
                sb.Append("<h3 style='color:green'>✓ Conexión al tenant HANA OK</h3>");

                // Test 2: Leer una tabla SAP B1 con schema explícito
                try
                {
                    var dt = DiamDev.Give.DAL.HanaHelper.EjecutarConsulta(
                        $@"SELECT COUNT(*) AS TOTAL FROM ""{schema}"".""OITM"" WHERE ""Canceled"" = 'N'");

                    sb.Append($"<h3 style='color:green'>✓ Lectura schema '{schema}' OK — " +
                              $"{dt.Rows[0]["TOTAL"]} artículos activos</h3>");
                }
                catch (Exception ex2)
                {
                    sb.Append($"<h3 style='color:orange'>⚠ Conexión OK pero error leyendo schema</h3>" +
                              $"<pre>{ex2.Message}</pre>");
                }
            }
            else
            {
                sb.Append($"<h3 style='color:red'>✗ Fallo conexión HANA</h3><pre>{error}</pre>");
                sb.Append("<hr/><h4>Posibles causas:</h4><ul>");
                sb.Append($"<li>HANA_TenantDB '<b>{tenant}</b>' no existe → verifica el nombre real del tenant</li>");
                sb.Append($"<li>Puerto incorrecto en HANA_Server '<b>{server}</b>'</li>");
                sb.Append("<li>Firewall bloqueando el puerto HANA</li>");
                sb.Append("<li>Credenciales incorrectas</li>");
                sb.Append("</ul>");
            }

            return Content(sb.ToString(), "text/html");
        }

        public ActionResult DiagParametros()
        {
            string carpeta = Server.MapPath("~/Reports/Crystal/");
            var archivos = System.IO.Directory.GetFiles(carpeta, "*.rpt");

            var sb = new System.Text.StringBuilder();
            sb.Append(@"<style>
                        body { font-family: monospace; padding: 20px; }
                        table { border-collapse: collapse; margin-bottom: 30px; width: 100%; }
                        th { background: #2c3e50; color: white; padding: 8px 12px; text-align: left; }
                        td { border: 1px solid #ddd; padding: 6px 12px; }
                        tr:nth-child(even) { background: #f5f5f5; }
                        h3 { color: #2c3e50; border-bottom: 2px solid #2c3e50; padding-bottom: 5px; }
                        .none { color: #999; font-style: italic; }
                        .badge { background:#27ae60; color:white; padding:2px 8px; 
                                 border-radius:3px; font-size:11px; }
                    </style>");
            sb.Append("<h2>📋 Parámetros Crystal por Reporte</h2><hr/>");

            foreach (string archivo in archivos.OrderBy(f => f))
            {
                string nombre = System.IO.Path.GetFileName(archivo);
                sb.Append($"<h3>📄 {nombre}</h3>");

                var rpt = new ReportDocument();
                try
                {
                    rpt.Load(archivo);
                    var parametros = rpt.DataDefinition.ParameterFields;

                    if (parametros.Count == 0)
                    {
                        sb.Append("<p class='none'>— Sin parámetros definidos</p>");
                    }
                    else
                    {
                        sb.Append("<table>");
                        sb.Append("<tr><th>#</th><th>Nombre</th><th>Tipo</th><th>Requerido</th></tr>");
                        int i = 1;
                        foreach (ParameterFieldDefinition p in parametros)
                        {
                            string requerido = p.IsOptionalPrompt ? "No" : "<span class='badge'>Sí</span>";
                            sb.Append($"<tr><td>{i++}</td><td><b>{p.Name}</b></td>" +
                                      $"<td></td><td>{requerido}</td></tr>");
                        }
                        sb.Append("</table>");
                    }
                }
                catch (Exception ex)
                {
                    sb.Append($"<p style='color:red'>❌ Error al cargar: {ex.Message}</p>");
                }
                finally
                {
                    rpt.Close();
                    rpt.Dispose();
                }
            }

            return Content(sb.ToString(), "text/html");
        }

        public ActionResult DiagConexionRutas()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<style>body{font-family:monospace;padding:20px} table{border-collapse:collapse;width:100%} td,th{border:1px solid #ccc;padding:6px 10px} th{background:#2c3e50;color:white}</style>");
            sb.Append("<h2>Diagnóstico conexión: Estado Pedido.rpt</h2><hr/>");

            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado Pedido.rpt"));

                // ── ANTES de aplicar credenciales ──────────────────────────────
                sb.Append("<h3>Tablas del reporte principal (ANTES)</h3><table>");
                sb.Append("<tr><th>Tabla</th><th>ServerName</th><th>DatabaseName</th><th>UserID</th><th>Connection.Type</th></tr>");
                foreach (CrystalDecisions.CrystalReports.Engine.Table t in rpt.Database.Tables)
                {
                    var li = t.LogOnInfo;
                    sb.Append($"<tr><td>{t.Name}</td><td>{li.ConnectionInfo.ServerName}</td>" +
                              $"<td>{li.ConnectionInfo.DatabaseName}</td><td>{li.ConnectionInfo.UserID}</td>" +
                              $"<td>{li.ConnectionInfo.Type}</td></tr>");
                }
                sb.Append("</table>");

                // ── Subreportes ────────────────────────────────────────────────
                if (rpt.Subreports.Count > 0)
                {
                    sb.Append($"<h3>Subreportes encontrados: {rpt.Subreports.Count}</h3>");
                    foreach (ReportDocument sub in rpt.Subreports)
                    {
                        sb.Append($"<h4>Subreporte: {sub.Name}</h4><table>");
                        sb.Append("<tr><th>Tabla</th><th>ServerName</th><th>DatabaseName</th><th>UserID</th></tr>");
                        foreach (CrystalDecisions.CrystalReports.Engine.Table t in sub.Database.Tables)
                        {
                            var li = t.LogOnInfo;
                            sb.Append($"<tr><td>{t.Name}</td><td>{li.ConnectionInfo.ServerName}</td>" +
                                      $"<td>{li.ConnectionInfo.DatabaseName}</td><td>{li.ConnectionInfo.UserID}</td></tr>");
                        }
                        sb.Append("</table>");
                    }
                }
                else
                {
                    sb.Append("<p>Sin subreportes.</p>");
                }

                // ── Configuración en Web.config ────────────────────────────────
                sb.Append("<h3>Web.config HANA</h3><table>");
                sb.Append("<tr><th>Clave</th><th>Valor</th></tr>");
                foreach (string key in new[] { "HANA_Server", "HANA_Database", "HANA_User" })
                {
                    sb.Append($"<tr><td>{key}</td><td>{System.Configuration.ConfigurationManager.AppSettings[key]}</td></tr>");
                }
                sb.Append("</table>");

                rpt.Close(); rpt.Dispose();
            }
            catch (Exception ex)
            {
                sb.Append($"<h3 style='color:red'>Error al cargar el .rpt</h3><pre>{ex}</pre>");
            }

            return Content(sb.ToString(), "text/html");
        }

        public ActionResult DiagDetalleFacturas()
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Detalle de Facturas.rpt"));
                var sb = new System.Text.StringBuilder();
                sb.Append("<pre style='font-family:monospace;padding:20px'>");
                sb.Append("<h3>Tablas / Comandos</h3>");

                foreach (CrystalDecisions.CrystalReports.Engine.Table t in rpt.Database.Tables)
                {
                    sb.Append($"<b>Tabla:</b> {t.Name}<br/>");
                    sb.Append($"<b>Location:</b> {t.Location}<br/>");
                    var li = t.LogOnInfo;
                    sb.Append($"<b>ServerName:</b> {li.ConnectionInfo.ServerName}<br/>");
                    sb.Append($"<b>DatabaseName:</b> {li.ConnectionInfo.DatabaseName}<br/><hr/>");
                }

                // Si usa SQL Command embebido
                try
                {
                    var cmd = rpt.DataDefinition;
                    sb.Append($"<b>RecordSelectionFormula:</b><br/>{cmd.RecordSelectionFormula}<br/>");
                }
                catch { }

                sb.Append("</pre>");
                rpt.Close(); rpt.Dispose();
                return Content(sb.ToString(), "text/html");
            }
            catch (Exception ex)
            {
                rpt.Close(); rpt.Dispose();
                return Content($"<pre style='color:red'>{ex}</pre>", "text/html");
            }
        }

        public ActionResult DiagSql()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<style>body{font-family:monospace;padding:20px}</style>");
            sb.Append("<h2>Diagnóstico SQL Server — GiveContext</h2><hr/>");

            try
            {
                var cs = System.Configuration.ConfigurationManager
                             .ConnectionStrings["GiveContext"].ConnectionString;
                var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(cs);

                sb.Append("<h4>Configuración leída</h4><ul>");
                sb.Append($"<li><b>Server:</b>   {builder.DataSource}</li>");
                sb.Append($"<li><b>Database:</b> {builder.InitialCatalog}</li>");
                sb.Append($"<li><b>User:</b>     {builder.UserID}</li>");
                sb.Append("</ul>");

                using (var conn = new System.Data.SqlClient.SqlConnection(cs))
                {
                    conn.Open();
                    sb.Append("<h3 style='color:green'>✓ Conexión SQL Server OK</h3>");

                    // Verifica que el reporte puede leer alguna tabla clave
                    using (var cmd = new System.Data.SqlClient.SqlCommand(
                               "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES", conn))
                    {
                        int tablas = (int)cmd.ExecuteScalar();
                        sb.Append($"<p>Tablas en la BD: <b>{tablas}</b></p>");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.Append($"<h3 style='color:red'>✗ Fallo conexión SQL</h3><pre>{ex.Message}</pre>");
            }

            return Content(sb.ToString(), "text/html");
        }

        public ActionResult DiagDespachos()
        {
            var rpt = new ReportDocument();
            var sb = new System.Text.StringBuilder();
            sb.Append("<style>body{font-family:monospace;padding:20px} table{border-collapse:collapse;width:100%} td,th{border:1px solid #ccc;padding:6px 10px} th{background:#2c3e50;color:white}</style>");
            sb.Append("<h2>Diagnóstico: Despachos en ruta dia.rpt</h2><hr/>");
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Despachos en ruta dia.rpt"));

                sb.Append("<h3>Tablas / Conexión embebida</h3><table>");
                sb.Append("<tr><th>Tabla</th><th>ServerName</th><th>DatabaseName</th><th>UserID</th><th>Type</th></tr>");
                foreach (CrystalDecisions.CrystalReports.Engine.Table t in rpt.Database.Tables)
                {
                    var li = t.LogOnInfo;
                    sb.Append($"<tr><td>{t.Name}</td><td>{li.ConnectionInfo.ServerName}</td>" +
                              $"<td>{li.ConnectionInfo.DatabaseName}</td><td>{li.ConnectionInfo.UserID}</td>" +
                              $"<td>{li.ConnectionInfo.Type}</td></tr>");
                }
                sb.Append("</table>");

                sb.Append("<h3>Parámetros del reporte</h3><table>");
                sb.Append("<tr><th>Nombre</th><th>Tipo</th></tr>");
                foreach (ParameterFieldDefinition p in rpt.DataDefinition.ParameterFields)
                    sb.Append($"<tr><td>{p.Name}</td><td>{p.ParameterValueKind}</td></tr>");
                sb.Append("</table>");

                rpt.Close(); rpt.Dispose();
            }
            catch (Exception ex)
            {
                sb.Append($"<pre style='color:red'>{ex}</pre>");
            }
            return Content(sb.ToString(), "text/html");
        }

        public ActionResult DiagDespachosRutaMixto()
        {
            var rpt = new ReportDocument();
            var sb = new System.Text.StringBuilder();
            sb.Append(@"<style>
                            body  { font-family: monospace; padding: 20px; }
                            table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }
                            th    { background: #2c3e50; color: white; padding: 8px 12px; text-align: left; }
                            td    { border: 1px solid #ddd; padding: 6px 12px; }
                            tr:nth-child(even) { background: #f5f5f5; }
                            h3    { color: #2c3e50; border-bottom: 2px solid #3498db; }
                            .ok   { color: green; font-weight: bold; }
                            .warn { color: orange; font-weight: bold; }
                            .err  { color: red; font-weight: bold; }
                        </style>");
            sb.Append("<h2>Diagnóstico: Reporte despachos en ruta.rpt (SQL + HANA)</h2><hr/>");

            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Reporte despachos en ruta.rpt"));

                // ── Main report ──────────────────────────────────────────────────────
                sb.Append("<h3>📊 Reporte Principal — Tablas y conexión embebida</h3>");
                sb.Append("<table><tr><th>Tabla</th><th>ServerName</th><th>DatabaseName</th>" +
                          "<th>UserID</th><th>Type (Driver)</th></tr>");
                foreach (CrystalDecisions.CrystalReports.Engine.Table t in rpt.Database.Tables)
                {
                    var li = t.LogOnInfo;
                    sb.Append($"<tr><td><b>{t.Name}</b></td>" +
                              $"<td>{li.ConnectionInfo.ServerName}</td>" +
                              $"<td>{li.ConnectionInfo.DatabaseName}</td>" +
                              $"<td>{li.ConnectionInfo.UserID}</td>" +
                              $"<td>{li.ConnectionInfo.Type}</td></tr>");
                }
                sb.Append("</table>");

                // ── Parámetros del main ──────────────────────────────────────────────
                sb.Append("<h3>📋 Reporte Principal — Parámetros</h3>");
                sb.Append("<table><tr><th>#</th><th>Nombre</th><th>Tipo</th><th>Requerido</th></tr>");
                int idx = 1;
                foreach (ParameterFieldDefinition p in rpt.DataDefinition.ParameterFields)
                    sb.Append($"<tr><td>{idx++}</td><td><b>{p.Name}</b></td>" +
                              $"<td>{p.ParameterValueKind}</td>" +
                              $"<td>{(p.IsOptionalPrompt ? "No" : "<span class='ok'>Sí</span>")}</td></tr>");
                sb.Append("</table>");

                // ── Subreportes ──────────────────────────────────────────────────────
                int totalSubs = rpt.Subreports.Count;
                sb.Append($"<h3>📎 Subreportes encontrados: {totalSubs}</h3>");

                if (totalSubs == 0)
                {
                    sb.Append("<p class='warn'>⚠ No se encontraron subreportes. " +
                              "Verifica que el .rpt sea el correcto.</p>");
                }
                else
                {
                    foreach (ReportDocument sub in rpt.Subreports)
                    {
                        bool esDatosFacturaHana = string.Equals(
                            sub.Name, "DatosFacturaHANA", StringComparison.OrdinalIgnoreCase);

                        sb.Append($"<h4>Subreporte: <code>\"{sub.Name}\"</code> " +
                                  (esDatosFacturaHana
                                      ? "<span class='ok'>✓ Coincide con 'DatosFacturaHANA'</span>"
                                      : "<span class='warn'>⚠ Nombre diferente — actualiza el código</span>") +
                                  "</h4>");

                        sb.Append("<table><tr><th>Tabla</th><th>ServerName</th>" +
                                  "<th>DatabaseName</th><th>UserID</th><th>Type (Driver)</th></tr>");
                        foreach (CrystalDecisions.CrystalReports.Engine.Table t in sub.Database.Tables)
                        {
                            var li = t.LogOnInfo;
                            sb.Append($"<tr><td><b>{t.Name}</b></td>" +
                                      $"<td>{li.ConnectionInfo.ServerName}</td>" +
                                      $"<td>{li.ConnectionInfo.DatabaseName}</td>" +
                                      $"<td>{li.ConnectionInfo.UserID}</td>" +
                                      $"<td>{li.ConnectionInfo.Type}</td></tr>");
                        }
                        sb.Append("</table>");

                        if (sub.DataDefinition.ParameterFields.Count > 0)
                        {
                            sb.Append("<p><b>Parámetros del subreporte:</b></p>");
                            sb.Append("<table><tr><th>Nombre</th><th>Tipo</th></tr>");
                            foreach (ParameterFieldDefinition p in sub.DataDefinition.ParameterFields)
                                sb.Append($"<tr><td><b>{p.Name}</b></td><td>{p.ParameterValueKind}</td></tr>");
                            sb.Append("</table>");
                        }
                    }
                }

                // ── AppSettings relevantes ───────────────────────────────────────────
                sb.Append("<h3>⚙ AppSettings HANA APK66</h3>");
                sb.Append("<table><tr><th>Clave</th><th>Valor</th></tr>");
                foreach (var key in new[] { "HANA_Server_APK66", "HANA_Database_APK66", "HANA_User_APK66" })
                {
                    var val = System.Configuration.ConfigurationManager.AppSettings[key];
                    sb.Append($"<tr><td>{key}</td>" +
                              $"<td>{(string.IsNullOrEmpty(val) ? "<span class='err'>NO DEFINIDO</span>" : val)}</td></tr>");
                }
                sb.Append("</table>");

                rpt.Close();
                rpt.Dispose();
            }
            catch (Exception ex)
            {
                sb.Append($"<h3 class='err'>Error al cargar el .rpt</h3><pre>{ex}</pre>");
            }

            return Content(sb.ToString(), "text/html");
        }
        // ═══════════════════════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════════════════

        // GET: /Reporte/Index
        public ActionResult Index()
        {
            CustomHelper.setTitle("Reportes", "Listado");
            return View();
        }

        /// <summary>
        /// Endpoint JSON para que el frontend sepa qué empresas y agentes
        /// tiene disponibles el usuario loggeado.
        /// Se llama desde Index.cshtml al cargar la página.
        /// </summary>
        [HttpGet]
        public JsonResult GetUserEmpresaInfo()
        {
            // ⚠️ AJUSTE: si CustomHelper.getUserId() devuelve int, cambia a:
            // long usuarioId = (long)CustomHelper.getUserId();
            long usuarioId = CustomHelper.getUserId();

            var bl = new UsuarioEmpresaBL();
            var registros = bl.ObtenerPorUsuarioId(usuarioId);

            // Proyectamos cada registro a un objeto anónimo con los campos
            // que necesita el JavaScript en el frontend.
            var agentes = registros.Select(r =>
            {
                var parsed = bl.ParseCodigo(r.Codigo);
                return new
                {
                    // String para evitar pérdida de precisión en JSON con números grandes
                    EmpresaId = r.EmpresaId.ToString(),
                    EmpresaNombre = bl.GetEmpresaNombre(r.EmpresaId),
                    HanaDb = bl.GetHanaDb(r.EmpresaId),
                    SapId = parsed.SapId,
                    AgenteNombre = parsed.AgenteNombre,
                    Codigo = r.Codigo,
                    SerieSap = r.SERIE_SAP
                };
            }).ToList();

            return Json(new
            {
                TieneBolik = registros.Any(r => r.EmpresaId == UsuarioEmpresaBL.ID_BOLIK),
                TieneFaes = registros.Any(r => r.EmpresaId == UsuarioEmpresaBL.ID_FAES),
                TieneGraco = registros.Any(r => r.EmpresaId == UsuarioEmpresaBL.ID_GRACO),
                Agentes = agentes
            }, JsonRequestBehavior.AllowGet);
        }
    }


}