using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using Lsa.Data;
using Lsa.Vmfg.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiamDev.Give.Sincronizador
{
    class Program
    {

        // ── Control de cadencia para la sincronización de RECIBOS ──
        // El loop principal gira cada 2 seg (por los pedidos), pero los recibos
        // solo queremos revisarlos cada 5 min. Esta variable recuerda la última corrida.
        private static DateTime _ultimoSyncRecibos = DateTime.MinValue;
        private static readonly TimeSpan INTERVALO_RECIBOS = TimeSpan.FromMinutes(5);


        static void Main(string[] args)
        {
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

                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Revisa los recibos contra SAP en dos direcciones:
        ///  - PENDIENTE -> OPERADO  (créditos ya aplicó el pago).
        ///  - OPERADO   -> PENDIENTE (se anuló en SAP) o re-apunta DocEntry (anuló+rehízo).
        /// Llama al BLL (que tiene la lógica); aquí solo orquestamos y logueamos.
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

                // Detalle de errores al log (no a la consola, para no ensuciarla)
                foreach (string err in resultado.Errores)
                    LogFile.Error("Recibo sync: " + err);

                Console.WriteLine("----------------------------------------------");
            }
            catch (Exception ex)
            {
                // Blindaje total: si algo explota, lo logueamos pero NO tumbamos
                // el loop (los pedidos deben seguir sincronizando).
                Console.WriteLine("ERROR GENERAL EN SYNC DE RECIBOS: {0}", ex.Message);
                LogFile.Error("ERROR GENERAL en SincronizarRecibos", ex);
            }
        }


        private static string GuardarPedido(long id, string nombreInstancia)
        {
            string PedidoERPId = string.Empty;
            bool Conexion = false;

            //PEDIDO K66
            ERPPedidoEncabezadoK66 PedidoActual = new ERPPedidoEncabezadoK66();
            List<ERPPedidoDetalleK66> DetalleActual = new List<ERPPedidoDetalleK66>();

            try
            {
                PedidoActual = new Pedidok66BL().ObtenerPendientexId(id);
            }
            catch (Exception)
            { }

            if (PedidoActual == null)
            {
                Console.WriteLine("NO SE ENCUENTRA EL PEDIDO REGISTRADO EN EL SISTEMA");
                Console.WriteLine("----------------------------------------------");
                return PedidoERPId;
            }

            try
            {
                DetalleActual = new Pedidok66BL().ObtenerPendienteDetallexId(id);
            }
            catch (Exception)
            { }

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
            catch (Exception)
            { }

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

                if (OrderId.Length == 0)
                {
                    OrderId = "<1>";
                }

                Console.WriteLine("INICIA ENCABEZADO DEL #PEDIDO: {0}", id);
                Console.WriteLine("----------------------------------------------");

                drEncabezado = CustomerOrder.NewOrderRow(OrderId);
                drEncabezado["CUSTOMER_ID"] = PedidoActual.CUSTOMER_ID;
                drEncabezado["SITE_ID"] = PedidoActual.SITEID;
                drEncabezado["ENTERED_BY"] = PedidoActual.Extra1;
                drEncabezado["TERMS_ID"] = PedidoActual.Extra2;

                if (PedidoActual.SHIP_TO_ADDR_NO > 0)
                {
                    drEncabezado["SHIP_TO_ADDR_NO"] = PedidoActual.SHIP_TO_ADDR_NO;
                }
                if (PedidoActual.SHIPTO_ID != "0")
                {
                    drEncabezado["SHIPTO_ID"] = PedidoActual.SHIPTO_ID;
                }

                if (PedidoActual.CUSTOMER_PO_REF != "NA")
                {
                    drEncabezado["CUSTOMER_PO_REF"] = PedidoActual.CUSTOMER_PO_REF;
                }

                drEncabezado["DESIRED_SHIP_DATE"] = PedidoActual.DESIRED_SHIP_DATE.ToString();
                drEncabezado["STATUS"] = PedidoActual.STATUS;

                if (PedidoActual.USER_1 != "NA")
                {
                    drEncabezado["USER_1"] = PedidoActual.USER_1;
                }

                drEncabezado["USER_2"] = PedidoActual.USER_2;

                if (PedidoActual.USER_3 != "0")
                {
                    drEncabezado["USER_3"] = PedidoActual.USER_3;
                }

                if (PedidoActual.USER_4 != "0")
                {
                    drEncabezado["USER_4"] = PedidoActual.USER_4;
                }

                if (PedidoActual.USER_5 != "0")
                {
                    drEncabezado["USER_5"] = PedidoActual.USER_5;
                }

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

                        drDetalle["SITE_ID"] = Detalle.SITEID; //BOLIK

                        if (Detalle.TRADE_DISC_PERCENT == 0)
                        {
                            drDetalle["UNIT_PRICE"] = Detalle.UNIT_PRICE; //PRECIO
                        }
                        else
                        {
                            drDetalle["UNIT_PRICE"] = Detalle.UNIT_PRICE_ORIGINAL; //PRECIO
                        }

                        drDetalle["USER_ORDER_QTY"] = Detalle.USER_ORDER_QTY; //CANTIDAD
                        drDetalle["TRADE_DISC_PERCENT"] = Detalle.TRADE_DISC_PERCENT; //DESCUENTO CUANDO SE APLICA PORCENTAJE DE DESCUENTO WEB. AGREGAR CAMPO
                        drDetalle["PART_ID"] = Detalle.PART_ID;
                        drDetalle["SELLING_UM"] = UnidadOriginal;
                        drDetalle["VAT_CODE"] = Detalle.VAT_CODE;  // VAT_CODE DE TABLA CUSTOMER
                        drDetalle["ENTERED_BY"] = PedidoActual.Extra1;
                        i++;
                    }

                    // Add some specs to the order.
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