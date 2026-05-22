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

        // ── SIN PARÁMETROS — abren directo ──────────────────────────────────────

        public ActionResult DespachosEnRutaDia()
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Despachos en ruta dia.rpt"));
                AplicarConexionHana(rpt);
                return ExportarPdf(rpt, "Despachos_En_Ruta_Dia");
            }
            catch(Exception ex)
            {
                rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Despachos en ruta dia");
            }
        }

        // ── PARÁMETRO: Agente ────────────────────────────────────────────────────

        public ActionResult BackorderAgenteBolik(string cliente = "",
                                           string producto = "",
                                           string pedido = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Backorder Agentes Bolik.rpt"));
                AplicarConexionHana(rpt);

                string agente = CustomHelper.getUsuarioNombre();

                TrySetParametro(rpt, "Agente", agente);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Producto", string.IsNullOrWhiteSpace(producto) ? "*" : producto);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);

                return ExportarPdf(rpt, $"Backorder_Bolik_{agente}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Backorder Agentes Bolik"); }
        }

        public ActionResult BackorderAgenteGraco(string cliente = "",
                                                  string producto = "",
                                                  string pedido = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Backorder Agentes Graco.rpt"));
                AplicarConexionHana(rpt);

                string agente = CustomHelper.getUsuarioNombre();

                TrySetParametro(rpt, "Agente", agente);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Producto", string.IsNullOrWhiteSpace(producto) ? "*" : producto);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);

                return ExportarPdf(rpt, $"Backorder_Graco_{agente}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Backorder Agentes Graco"); }
        }

        public ActionResult BackorderAgenteFaes(string cliente = "",
                                                  string producto = "",
                                                  string pedido = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Backorder Agentes Faes.rpt"));
                AplicarConexionHana(rpt);

                string agente = CustomHelper.getUsuarioNombre();

                TrySetParametro(rpt, "Agente", agente);
                TrySetParametro(rpt, "Cliente", string.IsNullOrWhiteSpace(cliente) ? "*" : cliente);
                TrySetParametro(rpt, "Producto", string.IsNullOrWhiteSpace(producto) ? "*" : producto);
                TrySetParametro(rpt, "Pedido", string.IsNullOrWhiteSpace(pedido) ? "*" : pedido);

                return ExportarPdf(rpt, $"Backorder_Faes_{agente}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Backorder Agentes Faes"); }
        }

        // ── PARÁMETROS: FInicial + FFinal + Cliente ──────────────────────────────
        // ⚠️ Nombres EXACTOS del .rpt: FInicial / FFinal / Cliente (no FechaInicial/CardCode)

        public ActionResult EstadoDeCuenta(string fechaInicial = "",
                                            string fechaFinal = "",
                                            string cardCode = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado de Cuenta.rpt"));
                AplicarConexionHana(rpt);

                if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "FInicial", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "FFinal", Convert.ToDateTime(fechaFinal));
                }

                if (!string.IsNullOrWhiteSpace(cardCode))
                {
                    TrySetParametro(rpt, "Cliente", cardCode);  // ← "Cliente", no "CardCode"
                }

                string sufijo = string.IsNullOrWhiteSpace(cardCode) ? "General" : cardCode;
                return ExportarPdf(rpt, $"Estado_Cuenta_{sufijo}");
            }
            catch (Exception ex) { rpt.Close(); rpt.Dispose(); return ContenidoError(ex, "Estado de Cuenta"); }
        }

        // ── PARÁMETROS: Cliente + Pedido ─────────────────────────────────────────
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

        // ── INVENTARIO GRACO ────────────────────────────────────────────────────
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

        // ── INVENTARIO ESCOCESA ─────────────────────────────────────────────────
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

        // [Permiso("Control.Reporte.Inventario")]
        public ActionResult EstadoPedido(string fechaInicial = "", string fechaFinal = "",
                                  string vehiculo = "", string noRuta = "",
                                  string agente = "", string documento = "")
        {
            var rpt = new ReportDocument();
            try
            {
                rpt.Load(Server.MapPath("~/Reports/Crystal/Estado Pedido.rpt"));
                AplicarConexionSql(rpt, "GiveContext");

                if (!string.IsNullOrWhiteSpace(fechaInicial) &&
                    !string.IsNullOrWhiteSpace(fechaFinal))
                {
                    TrySetParametro(rpt, "FECHA INICIAL", Convert.ToDateTime(fechaInicial));
                    TrySetParametro(rpt, "FECHA FINAL", Convert.ToDateTime(fechaFinal));
                }

                TrySetParametro(rpt, "VEHICULO", string.IsNullOrWhiteSpace(vehiculo) ? "*" : vehiculo);
                TrySetParametro(rpt, "NO RUTA", string.IsNullOrWhiteSpace(noRuta) ? "*" : noRuta);
                TrySetParametro(rpt, "AGENTE", string.IsNullOrWhiteSpace(agente) ? "*" : agente);
                TrySetParametro(rpt, "DOCUMENTO", string.IsNullOrWhiteSpace(documento) ? "*" : documento);

                return ExportarPdf(rpt, "Estado_Pedido");
            }
            catch (Exception ex)
            {
                rpt.Close(); rpt.Dispose();
                return ContenidoError(ex, "Estado Pedido");
            }
        }

        // ── DETALLE FACTURAS — Empresa + Fechas + Cliente + Codigo + Producto ──────
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

        // ═══════════════════════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════════════════

        [Permiso("Control.Reporte.kpidel")]
        public ActionResult KpiDelivery()
        {
            CustomHelper.setTitle("KPI Entrega", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.cxchist")]
        public ActionResult CuentasPorCobrarHistorico()
        {
            CustomHelper.setTitle("Cuentas Por Cobrar Historico", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.ventasdesp")]
        public ActionResult VentasDespachadas()
        {
            CustomHelper.setTitle("Venta Despachada", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.recanula")]
        public ActionResult RecibosAnulados()
        {
            CustomHelper.setTitle("Recibos Anulados", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.CompVend")]
        public ActionResult VendedoresComparativa()
        {
            CustomHelper.setTitle("Comparativa Vendedores", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.TransRep")]
        public ActionResult TransporteConsolidado()
        {
            CustomHelper.setTitle("Transporte Consolidado", "Reporte");

            this.CargaControles(true);
            return View();
        }
        [Permiso("Control.Reporte.Ventacom")]
        public ActionResult ComparativaSucursal()
        {
            CustomHelper.setTitle("Comparativa Sucursal", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.ProdTop")]
        public ActionResult ProductosTop()
        {
            CustomHelper.setTitle("Ventas Por Producto", "Reporte");

            this.CargaControles(true);
            return View();
        }



        [Permiso("Control.Reporte.topcli")]
        public ActionResult TopClientes()
        {
            CustomHelper.setTitle("Top Clientes", "Reporte");

            
            return View();
        }


        [Permiso("Control.Reporte.Cierre")]
        public ActionResult Cierre()
        {
            CustomHelper.setTitle("Cierre del Día", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Cierre")]
        public ActionResult CierrexUsuario()
        {
            CustomHelper.setTitle("Cierre del Día x Usuario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Cierre")]
        public ActionResult CierrexUsuarioHora()
        {
            CustomHelper.setTitle("Cierre del Día x Usuario Hora", "Reporte");

            this.cargaUsuarios();
            return View();
        }

        [Permiso("Control.Reporte.Ingreso")]
        public ActionResult Ingreso()
        {
            CustomHelper.setTitle("Ingreso", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.IngresoxProveedor")]
        public ActionResult IngresoxProveedor()
        {
            CustomHelper.setTitle("Ingreso x Proveedor", "Reporte");

            this.CargaControles(true);
            this.CargaProveedores();
            return View();
        }

        [Permiso("Control.Reporte.IngresoxProducto")]
        public ActionResult IngresoxProducto()
        {
            CustomHelper.setTitle("Ingreso x Producto", "Reporte");

            this.CargaControles(true);
            this.CargaProductos();
            return View();
        }

        [Permiso("Control.Reporte.Egreso")]
        public ActionResult Egreso()
        {
            CustomHelper.setTitle("Egreso", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia()
        {
            CustomHelper.setTitle("Ganancia", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia_Detalle()
        {
            CustomHelper.setTitle("Ganancia", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia_Consolidada()
        {
            CustomHelper.setTitle("Ganancia Consolidada", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Ganancia")]
        public ActionResult Ganancia_Consolidada_x_Producto()
        {
            CustomHelper.setTitle("Ganancia Consolidada x Producto", "Reporte");

            this.CargaControles(true);
            this.CargaProductos();
            return View();
        }

        [Permiso("Control.Reporte.Diario")]
        public ActionResult Diario()
        {
            CustomHelper.setTitle("Libro Diario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.Mayor")]
        public ActionResult Mayor()
        {
            CustomHelper.setTitle("Libro Mayor", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.BalanceSaldo")]
        public ActionResult Balance_Saldo()
        {
            CustomHelper.setTitle("Balance de Saldos", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.VentaxTienda")]
        public ActionResult VentaxTienda()
        {
            CustomHelper.setTitle("Venta x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.VentaxTienda")]
        public ActionResult VentaxTiendaYMarca()
        {
            CustomHelper.setTitle("Venta x Tienda Y Marca", "Reporte");

            this.CargaMarcas();
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.TomaFisicaxTienda")]
        public ActionResult TomaFisicaxTienda()
        {
            CustomHelper.setTitle("Toma Fisica de Inventario x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.InventarioxTienda")]
        public ActionResult InventarioxTienda()
        {
            CustomHelper.setTitle("Inventario x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.InventarioxTienda")]
        public ActionResult InventarioxTiendaYMarca()
        {
            CustomHelper.setTitle("Inventario x Tienda Y Marca", "Reporte");

            this.CargaMarcas();
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.PedidoxTienda")]
        public ActionResult PedidoxTiendaYMarca()
        {
            CustomHelper.setTitle("Pedido x Tienda Y Marca", "Reporte");

            this.CargaMarcas();
            this.CargaControles(true);
            return View();
        }
        //se cambio el nombre en el Menu y en el encabezado 
        [Permiso("Control.Reporte.VentaResumenxTienda")]
        public ActionResult VentaResumenxTienda()
        {
            CustomHelper.setTitle("Resumen del Mes", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.CierreDiarioResumen")]
        public ActionResult CierreDiarioResumen()
        {
            CustomHelper.setTitle("Corte Diario", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.IngresoxTienda")]
        public ActionResult IngresoxTienda()
        {
            CustomHelper.setTitle("Ingreso x Tienda", "Reporte");
                       
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.SalidaxTienda")]
        public ActionResult SalidaxTienda()
        {
            CustomHelper.setTitle("Salida x Tienda", "Reporte");

            this.CargaControles(true);
            return View();
        }

        public ActionResult Horario()
        {
            CustomHelper.setTitle("Horario Personal", "Reporte");

            this.CargaControles(true, false);
            this.CargarPersonal();
            return View();
        }

        public ActionResult Horario_General()
        {
            CustomHelper.setTitle("Horario General", "Reporte");

            this.CargaControles(true, false);
            return View();
        }

        [Permiso("Control.Reporte.LibroVenta")]
        public ActionResult LibroVenta()
        {
            CustomHelper.setTitle("Libro de Venta", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.ProductoControlado")]
        public ActionResult Producto_Controlado()
        {
            CustomHelper.setTitle("Producto Controlado", "Reporte");
           
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.ProductoMinimoCategoria")]
        public ActionResult Producto_Minimo_Categoria()
        {
            CustomHelper.setTitle("Producto Minimo x Categoria", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.VentaComisionVendedor")]
        public ActionResult Venta_Comision_Vendedor()
        {
            CustomHelper.setTitle("Venta Comision x Vendedor", "Reporte");

            this.cargaVendedores();
            return View();
        }

        [Permiso("Control.Reporte.ProveedorProducto")]
        public ActionResult Proveedor_Producto()
        {
            CustomHelper.setTitle("Proveedor Producto", "Reporte");

            this.CargaProveedores();
            return View();
        }

        [Permiso("Control.Reporte.VentaTransporte")]
        public ActionResult Venta_Transporte()
        {
            CustomHelper.setTitle("Venta Transporte", "Reporte");

            this.cargaTransportes();
            return View();
        }

        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario_x_Tienda_Categoria()
        {
            CustomHelper.setTitle("Inventario x Tienda y Categoria", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.Inventario")]
        public ActionResult Inventario_IDs_x_Tienda_Producto()
        {
            CustomHelper.setTitle("Inventario IDs x Tienda y Producto", "Reporte");

            this.CargaControles(true);
            this.cargaProductosIDs();
            return View();
        }

        [Permiso("Control.Reporte.VentaTransporte")]
        public ActionResult Cierre_Transporte()
        {
            CustomHelper.setTitle("Cierre Transporte", "Reporte");

            this.cargaTransportes();
            return View();
        }

        [Permiso("Control.Reporte.ProductoReserva")]
        public ActionResult Producto_Reservado()
        {
            CustomHelper.setTitle("Producto Reservado", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            return View();
        }

        [Permiso("Control.Reporte.VentaxTipoCliente")]
        public ActionResult Venta_x_Tipo_Cliente()
        {
            CustomHelper.setTitle("Venta x Tipo de Cliente", "Reporte");

            this.CargaControles(true);
            this.cargaTiposDeClientes();
            return View();
        }

        [Permiso("Control.Reporte.VentaxTipoCliente")]
        public ActionResult Grafica_Venta_x_Tipo_Cliente()
        {
            CustomHelper.setTitle("Grafica Venta x Tipo de Cliente", "Reporte");

            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.VentaComisionVendedor")]
        public ActionResult Venta_Comision_x_Vendedor_Configurable()
        {
            CustomHelper.setTitle("Venta Comision x Vendedor Configurable", "Reporte");

            this.cargaVendedores();
            return View();
        }

        [Permiso("Control.Reporte.Reparacion_Pagos_Tecnicos")]
        public ActionResult Reparacion_Pagos_Tecnicos()
        {
            CustomHelper.setTitle("Reparación de Pagos Tecnicos", "Reporte");

            this.cargaTecnicos();
            return View();
        }

        [Permiso("Control.Reporte.Venta_x_Forma_Pago")]
        public ActionResult Venta_x_Forma_Pago()
        {
            CustomHelper.setTitle("Venta x Forma de Pago", "Reporte");

            this.cargaFormas();
            return View();
        }

        [Permiso("Control.Reporte.Producto_Reservado_x_Producto")]
        public ActionResult Producto_Reservado_x_Producto()
        {
            CustomHelper.setTitle("Producto Reservado x Producto", "Reporte");

            this.CargaControles(true);
            this.CargarProductoCategorias();
            this.cargaEstadosReserva();
            return View();
        }

        [Permiso("Control.Reporte.Producto_Reservado_Actual")]
        public ActionResult Producto_Reservado_Actual()
        {
            CustomHelper.setTitle("Producto Reservado Actual", "Reporte");

            this.CargaControles(true);           
            return View();
        }

        [Permiso("Control.Reporte.Egresos_Efectivo")]
        public ActionResult Egresos_Efectivo()
        {
            CustomHelper.setTitle("Egresos de Efectivo", "Reporte");

            this.CargaControles(true);
            this.cargaCategoriaGastos();
            return View();
        }

        [Permiso("Control.Reporte.Abono_x_Cliente")]
        public ActionResult Abono_x_Cliente()
        {
            CustomHelper.setTitle("Abonos x Cliente", "Reporte");
         
            return View();
        }

        [Permiso("Control.Reporte.VentaxProductoDiaVendedor")]
        public ActionResult Venta_x_Producto_Dia_Vendedor()
        {
            CustomHelper.setTitle("Venta x Producto x Dia x Vendedor", "Reporte");

            this.CargaControles(true);
            this.cargaVendedores();
            return View();
        }

        [Permiso("Control.Reporte.ProductoxLote")]
        public ActionResult Producto_x_Lote()
        {
            CustomHelper.setTitle("Productos x Lote", "Reporte");

            this.CargaControles(true);           
            return View();
        }

        [Permiso("Control.Reporte.HistorialVenta")]
        public ActionResult HistorialVenta()
        {
            CustomHelper.setTitle("Historial de Venta", "Reporte");
            this.CargaControles(true);
            return View();
        }

        [Permiso("Control.Reporte.HistorialEntrega")]
        public ActionResult HistorialEntrega()
        {
            CustomHelper.setTitle("Historial de Entrega", "Reporte");
            this.CargaControles(true);
            return View();
        }

        // GET: /Reporte/Index
        public ActionResult Index()
        {
            CustomHelper.setTitle("Reportes", "Listado");
            return View();
        }
    }


}