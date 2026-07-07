using DiamDev.Give.BLL;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Lsa.Data;
using Lsa.Vmfg.Sales;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiamDev.Give.Sincronizador
{
    class Program
    {
        // ── Control de cadencia para la sincronización de RECIBOS ──
        private static DateTime _ultimoSyncRecibos = DateTime.MinValue;

        // =========== CAMBIAR A 5 MIN (PRODUCCION) ============================================
        private static readonly TimeSpan INTERVALO_RECIBOS = TimeSpan.FromMinutes(5);
        // private static readonly TimeSpan INTERVALO_RECIBOS = TimeSpan.FromSeconds(30); // DEMO: volver a FromMinutes(5) después

        static void Main(string[] args)
        {
            // ════════════════════════════════════════════════════════════
            //  MODO DIAGNÓSTICO:  Sincronizador.exe --diag  [ID_RECIBO] [EMPRESA]
            //  Corre los chequeos y SALE (no entra al loop). En TS sería como
            //  un `if (process.argv.includes('--diag')) { runDiag(); return; }`
            // ════════════════════════════════════════════════════════════
            if (args != null && args.Length > 0 &&
                args[0].Equals("--diag", StringComparison.OrdinalIgnoreCase))
            {
                string idRecibo = args.Length > 1 ? args[1] : null;
                string empresa = args.Length > 2 ? args[2] : "GRACO";
                EjecutarDiagnostico(idRecibo, empresa);

                Console.WriteLine();
                Console.WriteLine("Diagnóstico terminado. Presioná una tecla para salir...");
                Console.ReadKey();
                return; // NO entra al loop de sincronización
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("SINCRONIZADOR DE PEDIDOS K66");
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("ULTIMA SINCRONIZACION: {0}", DateTime.Now.ToString());
                Console.WriteLine("----------------------------------------------");

                Console.WriteLine("CARGANDO PEDIDOS PENDIENTES DE SINCRONIZAR");
                Console.WriteLine("----------------------------------------------");
                List<PedidoPendienteK66> PedidosPendientes = new Pedidok66BL().ObtenerPendientesSincronizar();

                if (PedidosPendientes != null && PedidosPendientes.Count() > 0)
                {
                    Console.WriteLine("CANTIDAD DE PEDIDOS A SINCRONIZAR: {0}", PedidosPendientes.Count());
                    Console.WriteLine("----------------------------------------------");

                    PedidosPendientes.ForEach(p =>
                    {
                        try
                        {
                            Console.WriteLine("INICIAR SINCRONIZACION DEL #PEDIDO: {0}", p.PedidoId);
                            Console.WriteLine("----------------------------------------------");

                            string CustomerOrderId = GuardarPedido(p.PedidoId, p.Etiqueta);

                            if (!string.IsNullOrWhiteSpace(CustomerOrderId))
                            {
                                Console.WriteLine("EL PEDIDO: {0} FUE CREADO EXITOSAMENTE EN EL ERP CON ORDER ID: {1}", p.PedidoId, CustomerOrderId);
                                Console.WriteLine("----------------------------------------------");

                                string Mensaje = new Pedidok66BL().GuardarCustomerOrder(p.PedidoId, CustomerOrderId);
                                if (Mensaje == "OK")
                                {
                                    Console.WriteLine("EL #PEDIDO: {0}, ACTUALIZO SU ESTADO A SINCRONIZADO", p.PedidoId);
                                    Console.WriteLine("----------------------------------------------");
                                }
                                else
                                {
                                    Console.WriteLine("EL #PEDIDO: {0}, NO ACTUALIZO SU ESTADO A SINCRONIZADO", p.PedidoId);
                                    Console.WriteLine("----------------------------------------------");
                                }
                            }
                            else
                            {
                                Console.WriteLine("EL #PEDIDO: {0}, NO FUE SINCRONIZADO", p.PedidoId);
                                Console.WriteLine("----------------------------------------------");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("ERROR EN LA SINCRONIZACION DEL PEDIDO: {0}", p.PedidoId);
                            Console.WriteLine("----------------------------------------------");
                        }
                    });
                }
                else
                {
                    Console.WriteLine("NO HAY PEDIDOS PENDIENTES DE SINCRONIZAR");
                    Console.WriteLine("----------------------------------------------");
                }

                // ════════════════════════════════════════════════════════════
                //  SINCRONIZACIÓN DE RECIBOS DE CAJA (cada 5 min)
                // ════════════════════════════════════════════════════════════
                if (DateTime.Now - _ultimoSyncRecibos >= INTERVALO_RECIBOS)
                {
                    SincronizarRecibos();
                    _ultimoSyncRecibos = DateTime.Now;
                }

                Thread.Sleep(2000);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  DIAGNÓSTICO (--diag)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Semáforo de las 3 patas del sync: SQL (RecibosContext), SAP (HANA/ODBC)
        /// y match de datos para un ID concreto. No modifica nada; solo lee y reporta.
        /// </summary>
        private static void EjecutarDiagnostico(string idRecibo, string empresa)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine(" DIAGNÓSTICO DEL SINCRONIZADOR DE RECIBOS");
            Console.WriteLine(" Fecha: {0}", DateTime.Now);
            Console.WriteLine("==============================================");
            Console.WriteLine();

            // ── PATA 1: SQL (RecibosContext) ─────────────────────────────
            Console.WriteLine("[1/3] SQL  -> conexión RecibosContext");
            string sqlServer = "(desconocido)", sqlDb = "(desconocido)";
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["RecibosContext"];
                if (cs == null)
                {
                    Console.WriteLine("   ✗ NO existe la connection string 'RecibosContext' en App.config.");
                }
                else
                {
                    var b = new SqlConnectionStringBuilder(cs.ConnectionString);
                    sqlServer = b.DataSource;
                    sqlDb = b.InitialCatalog;
                    Console.WriteLine("   → Server : {0}", sqlServer);
                    Console.WriteLine("   → BD     : {0}", sqlDb);

                    using (var cn = new SqlConnection(cs.ConnectionString))
                    {
                        cn.Open();
                        using (var cmd = new SqlCommand("SELECT DB_NAME(), @@SERVERNAME", cn))
                        using (var rd = cmd.ExecuteReader())
                            if (rd.Read())
                                Console.WriteLine("   ✓ Conectado. BD real: {0} | Server real: {1}",
                                    rd[0], rd[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ✗ ERROR SQL: {0}", ex.Message);
            }
            Console.WriteLine();

            // ── PATA 2: SAP (HANA / ODBC) ────────────────────────────────
            Console.WriteLine("[2/3] SAP  -> conexión HANA (HanaHelper)");
            try
            {
                string err;
                bool ok = HanaHelper.ProbarConexion(out err);
                if (ok) Console.WriteLine("   ✓ HANA conecta correctamente.");
                else Console.WriteLine("   ✗ HANA NO conecta: {0}", err);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ✗ ERROR HANA: {0}", ex.Message);
            }
            Console.WriteLine();

            // ── PATA 3: MATCH de datos para un ID concreto ───────────────
            Console.WriteLine("[3/3] MATCH -> comparar SQL vs SAP para un recibo");
            if (string.IsNullOrWhiteSpace(idRecibo))
            {
                Console.WriteLine("   (omitido) No pasaste ID. Uso: --diag RG12-07519 GRACO");
            }
            else
            {
                Console.WriteLine("   ID: {0}  |  Empresa: {1}", idRecibo, empresa);
                Console.WriteLine("   ------------------------------------------");

                // 3a. Lo que ve SQL
                try
                {
                    var cs = ConfigurationManager.ConnectionStrings["RecibosContext"].ConnectionString;
                    using (var cn = new SqlConnection(cs))
                    {
                        cn.Open();
                        using (var cmd = new SqlCommand(
                            @"SELECT SYNC_ESTADO, SAP_DOCENTRY, SAP_DOCNUM, STATUS, SYNC_OBSERVACION
                              FROM dbo.REC_CAJA_ENC
                              WHERE ID_RECIBO = @id AND ID_EMPRESA = @emp", cn))
                        {
                            cmd.Parameters.AddWithValue("@id", idRecibo);
                            cmd.Parameters.AddWithValue("@emp", empresa);
                            using (var rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                {
                                    Console.WriteLine("   SQL  → SYNC_ESTADO : {0}",
                                        rd["SYNC_ESTADO"] == DBNull.Value ? "(NULL)" : rd["SYNC_ESTADO"]);
                                    Console.WriteLine("          SAP_DOCENTRY: {0} | SAP_DOCNUM: {1} | STATUS: {2}",
                                        rd["SAP_DOCENTRY"], rd["SAP_DOCNUM"], rd["STATUS"]);
                                    Console.WriteLine("          OBSERVACION : {0}",
                                        rd["SYNC_OBSERVACION"] == DBNull.Value ? "(NULL)" : rd["SYNC_OBSERVACION"]);
                                }
                                else
                                {
                                    Console.WriteLine("   SQL  → ✗ El recibo NO existe en esta BD ({0}).", "REC_CAJA_ENC");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   SQL  → ✗ ERROR: {0}", ex.Message);
                }

                // 3b. Lo que ve SAP (usa el MISMO método que el sync real)
                try
                {
                    var hana = new HanaRepository();
                    var operados = hana.ObtenerCobrosOperados(empresa, new List<string> { idRecibo });
                    if (operados.Count > 0)
                    {
                        var s = operados[0];
                        Console.WriteLine("   SAP  → ✓ ACTIVO (Canceled='N'). DocEntry: {0} | DocNum: {1}",
                            s.SapDocEntry, s.SapDocNum);
                        Console.WriteLine("          (el sync lo marcaría OPERADO si está PENDIENTE)");
                    }
                    else
                    {
                        Console.WriteLine("   SAP  → ✗ NO aparece como activo.");
                        Console.WriteLine("          Causa típica: Canceled='Y' (anulado) o el ID no está en ORCT.");
                        Console.WriteLine("          Si en SQL está OPERADO, el sync lo regresaría a PENDIENTE.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   SAP  → ✗ ERROR: {0}", ex.Message);
                }

                // 3c. Veredicto legible
                Console.WriteLine("   ------------------------------------------");
                Console.WriteLine("   VEREDICTO: revisá arriba. Recordá la regla:");
                Console.WriteLine("     • El sync SOLO toca recibos en SYNC_ESTADO 'PENDIENTE' u 'OPERADO'.");
                Console.WriteLine("     • Si SYNC_ESTADO es NULL u otro valor → es INVISIBLE para el sync.");
            }

            Console.WriteLine();
            Console.WriteLine("==============================================");
        }

        /// <summary>
        /// Revisa los recibos contra SAP en dos direcciones:
        ///  - PENDIENTE -> OPERADO  (créditos ya aplicó el pago).
        ///  - OPERADO   -> PENDIENTE (se anuló en SAP) o re-apunta DocEntry.
        /// </summary>
        private static void SincronizarRecibos()
        {
            try
            {
                Console.WriteLine("==============================================");
                Console.WriteLine("SINCRONIZACION DE RECIBOS DE CAJA: {0}", DateTime.Now);
                Console.WriteLine("==============================================");
                LogFile.Info("Inicia sincronización de recibos.");

                var resultado = new ReciboCajaSyncBL().Ejecutar();

                string resumen = string.Format(
                "Pendientes revisados: {0} | Operados nuevos: {1} | Operados revisados: {2} | " +
                "Anulados: {3} | Reapuntados: {4} | Conciliados: {5} | Descuadrados: {6} | Errores: {7}",
                resultado.Revisados,
                resultado.Operados,
                resultado.OperadosRevisados,
                resultado.Anulados,
                resultado.Reapuntados,
                resultado.Conciliados,
                resultado.Descuadrados,
                resultado.Errores.Count);

                Console.WriteLine(resumen);
                LogFile.Info(resumen);

                foreach (string err in resultado.Errores)
                    LogFile.Error("Recibo sync: " + err);

                Console.WriteLine("----------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR GENERAL EN SYNC DE RECIBOS: {0}", ex.Message);
                LogFile.Error("ERROR GENERAL en SincronizarRecibos", ex);
            }
        }

        private static string GuardarPedido(long id, string nombreInstancia)
        {
            string PedidoERPId = string.Empty;
            bool Conexion = false;

            ERPPedidoEncabezadoK66 PedidoActual = new ERPPedidoEncabezadoK66();
            List<ERPPedidoDetalleK66> DetalleActual = new List<ERPPedidoDetalleK66>();

            try { PedidoActual = new Pedidok66BL().ObtenerPendientexId(id); }
            catch (Exception) { }

            if (PedidoActual == null)
            {
                Console.WriteLine("NO SE ENCUENTRA EL PEDIDO REGISTRADO EN EL SISTEMA");
                Console.WriteLine("----------------------------------------------");
                return PedidoERPId;
            }

            try { DetalleActual = new Pedidok66BL().ObtenerPendienteDetallexId(id); }
            catch (Exception) { }

            if (DetalleActual == null || DetalleActual.Count() == 0)
            {
                Console.WriteLine("NO SE ENCUENTRA EL PEDIDO REGISTRADO EN EL SISTEMA");
                Console.WriteLine("----------------------------------------------");
                return PedidoERPId;
            }

            try
            {
                Dbms.Close(nombreInstancia);
                Conexion = Dbms.OpenLocal(nombreInstancia, "SYSADM", "Kilob2020");
            }
            catch (Exception) { }

            if (!Conexion)
            {
                Console.WriteLine("NO HAY CONEXION AL ERP");
                Console.WriteLine("----------------------------------------------");
                return PedidoERPId;
            }

            try
            {
                CustomerOrder CustomerOrder = new CustomerOrder(nombreInstancia);
                CustomerOrder.Load("");

                Lsa.Data.DataRow drEncabezado;
                Lsa.Data.DataRow drDetalle;

                String OrderId = string.Empty;
                if (OrderId.Length == 0) OrderId = "<1>";

                Console.WriteLine("INICIA ENCABEZADO DEL #PEDIDO: {0}", id);
                Console.WriteLine("----------------------------------------------");

                drEncabezado = CustomerOrder.NewOrderRow(OrderId);
                drEncabezado["CUSTOMER_ID"] = PedidoActual.CUSTOMER_ID;
                drEncabezado["SITE_ID"] = PedidoActual.SITEID;
                drEncabezado["ENTERED_BY"] = PedidoActual.Extra1;
                drEncabezado["TERMS_ID"] = PedidoActual.Extra2;

                if (PedidoActual.SHIP_TO_ADDR_NO > 0)
                    drEncabezado["SHIP_TO_ADDR_NO"] = PedidoActual.SHIP_TO_ADDR_NO;
                if (PedidoActual.SHIPTO_ID != "0")
                    drEncabezado["SHIPTO_ID"] = PedidoActual.SHIPTO_ID;
                if (PedidoActual.CUSTOMER_PO_REF != "NA")
                    drEncabezado["CUSTOMER_PO_REF"] = PedidoActual.CUSTOMER_PO_REF;

                drEncabezado["DESIRED_SHIP_DATE"] = PedidoActual.DESIRED_SHIP_DATE.ToString();
                drEncabezado["STATUS"] = PedidoActual.STATUS;

                if (PedidoActual.USER_1 != "NA") drEncabezado["USER_1"] = PedidoActual.USER_1;
                drEncabezado["USER_2"] = PedidoActual.USER_2;
                if (PedidoActual.USER_3 != "0") drEncabezado["USER_3"] = PedidoActual.USER_3;
                if (PedidoActual.USER_4 != "0") drEncabezado["USER_4"] = PedidoActual.USER_4;
                if (PedidoActual.USER_5 != "0") drEncabezado["USER_5"] = PedidoActual.USER_5;

                Console.WriteLine("FINALIZA ENCABEZADO DEL #PEDIDO: {0}", id);
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("INICIA DETALLE DEL #PEDIDO: {0}", id);
                Console.WriteLine("----------------------------------------------");

                if (DetalleActual != null && DetalleActual.Count() > 0)
                {
                    int i = 1;
                    foreach (ERPPedidoDetalleK66 Detalle in DetalleActual)
                    {
                        Console.WriteLine("#LINEA: {0}", i);
                        Console.WriteLine("----------------------------------------------");

                        string UnidadOriginal = Detalle.SELLING_UM;
                        if (UnidadOriginal.Contains("-"))
                        {
                            UnidadOriginal = UnidadOriginal.Replace(" ", "").Trim();
                            UnidadOriginal = UnidadOriginal.Substring(UnidadOriginal.IndexOf("-") + 1);
                        }

                        drDetalle = CustomerOrder.NewOrderLineRow(OrderId, i);
                        drDetalle["SITE_ID"] = Detalle.SITEID;

                        if (Detalle.TRADE_DISC_PERCENT == 0)
                            drDetalle["UNIT_PRICE"] = Detalle.UNIT_PRICE;
                        else
                            drDetalle["UNIT_PRICE"] = Detalle.UNIT_PRICE_ORIGINAL;

                        drDetalle["USER_ORDER_QTY"] = Detalle.USER_ORDER_QTY;
                        drDetalle["TRADE_DISC_PERCENT"] = Detalle.TRADE_DISC_PERCENT;
                        drDetalle["PART_ID"] = Detalle.PART_ID;
                        drDetalle["SELLING_UM"] = UnidadOriginal;
                        drDetalle["VAT_CODE"] = Detalle.VAT_CODE;
                        drDetalle["ENTERED_BY"] = PedidoActual.Extra1;
                        i++;
                    }

                    Lsa.Data.DataRow specsRow = CustomerOrder.NewCustOrderBinaryRow(OrderId, "D");
                    specsRow["BITS"] = Encoding.Unicode.GetBytes(PedidoActual.Observaciones);

                    CustomerOrder.Save();
                    PedidoERPId = drEncabezado.ToString("ID");
                }

                Console.WriteLine("FINALIZA DETALLE DEL #PEDIDO: {0}", id);
                Console.WriteLine("----------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine("EL #PEDIDO: {0}, NO SE REGISTRO EN EL ERP", id);
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("ERROR: {0}", ex.Message);
                Console.WriteLine("----------------------------------------------");
            }

            return PedidoERPId;
        }
    }
}