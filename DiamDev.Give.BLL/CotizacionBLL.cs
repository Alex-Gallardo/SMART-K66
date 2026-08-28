using System;
using System.Collections.Generic;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    /// <summary>Reglas de negocio del módulo Cotizaciones.</summary>
    public class CotizacionBLL
    {
        private readonly CotizacionDA _da = new CotizacionDA();
        private readonly HanaRepository _hana = new HanaRepository();

        public List<ClienteHana> BuscarClientes(string empresa, string agente, string filtro)
        {
            var clientes = _hana.BuscarClientes(empresa, agente);
            if (string.IsNullOrWhiteSpace(filtro)) return clientes.Take(50).ToList();

            string f = filtro.Trim();
            return clientes.Where(c => Contiene(c.CardCode, f) ||
                                       Contiene(c.CardName, f) ||
                                       Contiene(c.LicTradNum, f))
                           .Take(50)
                           .ToList();
        }

        public PaginaProductosCotizacionHana BuscarProductos(
            string empresa, string agente, string clienteId, string filtro,
            int pagina, int tamano)
        {
            ObtenerClienteAsignado(empresa, agente, clienteId);
            var paginaResultado = _hana.BuscarProductosCotizacion(
                empresa, clienteId, filtro, pagina, tamano);
            NormalizarPreciosSap(paginaResultado.Items);
            return paginaResultado;
        }

        public ProductoCotizacionHana ObtenerPrecioProducto(
            string empresa, string agente, string clienteId,
            string itemCode, decimal cantidad)
        {
            ObtenerClienteAsignado(empresa, agente, clienteId);
            if (string.IsNullOrWhiteSpace(itemCode) || cantidad <= 0m ||
                cantidad > 999999999m)
                throw new InvalidOperationException(
                    "Indique un producto y una cantidad válida mayor que cero.");

            var cantidades = new Dictionary<string, decimal>(
                StringComparer.OrdinalIgnoreCase);
            cantidades[itemCode.Trim()] = cantidad;
            var productos = _hana.ObtenerProductosCotizacion(
                empresa, clienteId, cantidades);
            NormalizarPreciosSap(productos);
            return productos.FirstOrDefault();
        }

        /// <summary>
        /// Valida los snapshots contra SAP, recalcula absolutamente todos los
        /// importes y persiste. No acepta totales procedentes del navegador.
        /// </summary>
        public ResultadoCotizacion Guardar(CotizacionEncabezado enc, string usuario)
        {
            try
            {
                string error = ValidarEncabezado(enc);
                if (error != null) return ResultadoCotizacion.Error(error);

                var cliente = ObtenerClienteAsignado(
                    enc.IdEmpresa, enc.Agente, enc.IdCliente);
                enc.IdCliente = Limpiar(cliente.CardCode);
                enc.NombreCliente = Limpiar(cliente.CardName);
                enc.Nit = Limpiar(cliente.LicTradNum);
                enc.Direccion = Limpiar(cliente.Address);
                enc.Correo = Limpiar(cliente.Email);
                enc.Moneda = ResolverMoneda(enc.Moneda, cliente.Currency);
                enc.IdUsr = Limpiar(usuario);
                enc.Estado = EstadosCotizacion.Vigente;

                var codigos = enc.Detalles.Select(d => Limpiar(d.ItemCode)).ToList();
                if (codigos.Any(x => x.Length == 0))
                    return ResultadoCotizacion.Error("Todas las líneas deben tener un código de producto.");
                if (codigos.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codigos.Count)
                    return ResultadoCotizacion.Error(
                        "Un producto aparece más de una vez. Edite su cantidad en la línea existente.");

                var cantidades = enc.Detalles
                    .GroupBy(x => Limpiar(x.ItemCode),
                             StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First().Cantidad,
                                  StringComparer.OrdinalIgnoreCase);
                var productos = _hana.ObtenerProductosCotizacion(
                    enc.IdEmpresa, enc.IdCliente, cantidades);
                NormalizarPreciosSap(productos);
                var porCodigo = productos
                    .GroupBy(x => x.ItemCode, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First(),
                                  StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < enc.Detalles.Count; i++)
                {
                    var d = enc.Detalles[i];
                    ProductoCotizacionHana producto;
                    if (!porCodigo.TryGetValue(codigos[i], out producto))
                        return ResultadoCotizacion.Error(
                            "El producto '" + codigos[i] +
                            "' ya no está disponible para venta en SAP.");

                    string monedaProducto = NormalizarMoneda(producto.Moneda);
                    if (monedaProducto == "##") monedaProducto = "";
                    if (!string.IsNullOrWhiteSpace(monedaProducto) &&
                        !string.Equals(monedaProducto, enc.Moneda,
                                       StringComparison.OrdinalIgnoreCase))
                    {
                        return ResultadoCotizacion.Error(string.Format(
                            "El producto {0} tiene precio en {1}, pero la cotización está en {2}. " +
                            "Seleccione productos de la misma moneda.",
                            producto.ItemCode, monedaProducto, enc.Moneda));
                    }

                    d.Linea = i + 1;
                    d.ItemCode = producto.ItemCode;
                    d.ItemName = Limpiar(producto.ItemName);
                    d.Grupo = Limpiar(producto.Grupo);
                    d.Unidad = Limpiar(producto.Unidad);
                    d.ListaPrecio = producto.ListaPrecio;
                    d.Existencia = producto.Existencia;
                    d.Disponible = producto.Disponible;
                    d.PrecioLista = producto.Precio;
                    d.GrupoImpuesto = producto.GrupoImpuesto;
                    d.ImpuestoPorcentaje = producto.ImpuestoPorcentaje;
                    d.Descripcion = string.IsNullOrWhiteSpace(d.Descripcion)
                        ? d.ItemName : d.Descripcion.Trim();

                    error = ValidarLinea(d);
                    if (error != null)
                        return ResultadoCotizacion.Error(
                            "Línea " + d.Linea + " (" + d.ItemCode + "): " + error);

                    CalcularLinea(d);
                }

                enc.ImporteBruto = Redondear(enc.Detalles.Sum(x => x.ImporteBruto));
                enc.DescuentoTotal = Redondear(enc.Detalles.Sum(x => x.DescuentoMonto));
                enc.Subtotal = Redondear(enc.Detalles.Sum(x => x.Subtotal));
                enc.ImpuestoTotal = Redondear(enc.Detalles.Sum(x => x.ImpuestoMonto));
                enc.Total = Redondear(enc.Detalles.Sum(x => x.Total));

                if (enc.Total < 0m)
                    return ResultadoCotizacion.Error("El total de la cotización no puede ser negativo.");
                if (!_da.ExisteSerie(enc.IdEmpresa))
                    return ResultadoCotizacion.Error(
                        "No hay una serie activa de cotizaciones para " + enc.IdEmpresa + ".");

                _da.GuardarCompleta(enc);
                return ResultadoCotizacion.Ok(enc.IdCotizacion);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ResultadoCotizacion.Error("No se pudo guardar la cotización: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoCotizacion.Error("No se pudo guardar la cotización: " + ex.Message);
            }
            catch (Exception)
            {
                return ResultadoCotizacion.Error(
                    "No se pudo guardar la cotización por un error interno. " +
                    "Intente nuevamente o contacte a Sistemas.");
            }
        }

        /// <summary>Fórmula oficial, compartida por todos los guardados.</summary>
        public static void CalcularLinea(CotizacionDetalle d)
        {
            if (d == null) throw new ArgumentNullException("d");
            d.ImporteBruto = Redondear(d.Cantidad * d.PrecioUnitario);
            d.DescuentoMonto = Redondear(
                d.ImporteBruto * d.DescuentoPorcentaje / 100m);
            d.Subtotal = Redondear(d.ImporteBruto - d.DescuentoMonto);
            d.ImpuestoMonto = Redondear(
                d.Subtotal * d.ImpuestoPorcentaje / 100m);
            d.Total = Redondear(d.Subtotal + d.ImpuestoMonto);
        }

        /// <summary>
        /// HANA_02 confirmó que las fuentes de precio de las tres compañías son
        /// brutas. La aplicación calcula líneas netas y agrega IVA después, por
        /// lo que debe retirar el impuesto sin perder la precisión de SAP.
        /// </summary>
        public static decimal PrecioNetoDesdeBruto(
            decimal precioBruto, decimal impuestoPorcentaje)
        {
            if (precioBruto <= 0m || impuestoPorcentaje <= 0m)
                return Math.Round(precioBruto, 6,
                                  MidpointRounding.AwayFromZero);
            return Math.Round(
                precioBruto / (1m + impuestoPorcentaje / 100m), 6,
                MidpointRounding.AwayFromZero);
        }

        public List<CotizacionEncabezado> Listar(
            string empresa, string estado, string idUsr, string agente,
            DateTime? desde, DateTime? hasta, string filtro)
        {
            return _da.Listar(empresa, estado, idUsr, agente, desde, hasta, filtro);
        }

        public CotizacionEncabezado ObtenerPorId(string empresa, string idCotizacion)
        {
            return _da.ObtenerPorId(empresa, idCotizacion);
        }

        public ResultadoCotizacion Anular(
            string empresa, string idCotizacion, string usuario, string motivo)
        {
            motivo = Limpiar(motivo);
            if (motivo.Length < 5)
                return ResultadoCotizacion.Error(
                    "Indique un motivo de anulación de al menos 5 caracteres.");
            if (motivo.Length > 1000)
                return ResultadoCotizacion.Error(
                    "El motivo de anulación no puede exceder 1000 caracteres.");

            int filas = _da.Anular(empresa, idCotizacion, usuario, motivo);
            return filas == 1
                ? new ResultadoCotizacion
                  {
                      Exito = true,
                      IdCotizacion = idCotizacion,
                      Mensaje = "Cotización anulada correctamente."
                  }
                : ResultadoCotizacion.Error(
                    "La cotización no existe o ya había sido anulada.");
        }

        private ClienteHana ObtenerClienteAsignado(
            string empresa, string agente, string clienteId)
        {
            if (string.IsNullOrWhiteSpace(clienteId))
                throw new InvalidOperationException("Seleccione un cliente.");

            var cliente = _hana.BuscarClientes(empresa, agente).FirstOrDefault(c =>
                string.Equals(c.CardCode, clienteId.Trim(),
                              StringComparison.OrdinalIgnoreCase));
            if (cliente == null)
                throw new UnauthorizedAccessException(
                    "El cliente no pertenece al agente seleccionado o ya no está disponible en SAP.");
            return cliente;
        }

        private static string ValidarEncabezado(CotizacionEncabezado enc)
        {
            if (enc == null) return "No se recibió información de la cotización.";
            if (string.IsNullOrWhiteSpace(enc.IdEmpresa)) return "Seleccione una empresa.";
            if (string.IsNullOrWhiteSpace(enc.CodigoOperador)) return "Seleccione un agente.";
            if (string.IsNullOrWhiteSpace(enc.Agente)) return "El agente seleccionado no es válido.";
            if (string.IsNullOrWhiteSpace(enc.IdCliente)) return "Seleccione un cliente.";
            if (enc.ValidaHasta.Date < enc.Fecha.Date)
                return "La fecha de validez no puede ser anterior a la fecha de emisión.";
            if (enc.ValidaHasta.Date > enc.Fecha.Date.AddYears(1))
                return "La vigencia no puede exceder un año.";
            if (enc.Detalles == null || enc.Detalles.Count == 0)
                return "Agregue al menos un producto.";
            if (enc.Detalles.Count > 100)
                return "Una cotización no puede tener más de 100 productos.";
            if (Longitud(enc.CondicionesPago) > 250) return "Las condiciones de pago exceden 250 caracteres.";
            if (Longitud(enc.TiempoEntrega) > 250) return "El tiempo de entrega excede 250 caracteres.";
            if (Longitud(enc.Observaciones) > 1500) return "Las observaciones exceden 1500 caracteres.";
            return null;
        }

        private static string ValidarLinea(CotizacionDetalle d)
        {
            if (d.Cantidad <= 0m) return "la cantidad debe ser mayor que cero.";
            if (d.Cantidad > 999999999m) return "la cantidad es demasiado grande.";
            if (d.PrecioUnitario <= 0m)
                return "el precio neto debe ser mayor que cero.";
            if (d.PrecioUnitario > 999999999999m) return "el precio es demasiado grande.";
            if (d.DescuentoPorcentaje < 0m || d.DescuentoPorcentaje > 100m)
                return "el descuento debe estar entre 0 y 100%.";
            if (d.ImpuestoPorcentaje < 0m || d.ImpuestoPorcentaje > 100m)
                return "el impuesto debe estar entre 0 y 100%.";
            if (Longitud(d.Descripcion) > 500)
                return "la descripción excede 500 caracteres.";
            return null;
        }

        private static string ResolverMoneda(string solicitada, string monedaCliente)
        {
            string sap = NormalizarMoneda(monedaCliente);
            string app = NormalizarMoneda(solicitada);
            if (sap == "##" || string.IsNullOrWhiteSpace(sap))
                sap = app;
            if (sap != "GTQ" && sap != "USD" && sap != "EUR")
                throw new InvalidOperationException(
                    "La moneda del cliente en SAP no está soportada: '" + sap + "'.");
            if (!string.IsNullOrWhiteSpace(app) && app != sap)
                throw new InvalidOperationException(
                    "La moneda enviada no coincide con la moneda del cliente en SAP.");
            return sap;
        }

        private static void NormalizarPreciosSap(
            IEnumerable<ProductoCotizacionHana> productos)
        {
            foreach (var producto in productos ??
                     Enumerable.Empty<ProductoCotizacionHana>())
            {
                producto.Precio = producto.PrecioEsBruto
                    ? PrecioNetoDesdeBruto(
                        producto.PrecioBruto, producto.ImpuestoPorcentaje)
                    : producto.PrecioBruto;
            }
        }

        private static string NormalizarMoneda(string valor)
        {
            string moneda = Limpiar(valor).ToUpperInvariant();
            return moneda == "QTZ" || moneda == "Q" ? "GTQ" : moneda;
        }

        private static decimal Redondear(decimal valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }

        private static bool Contiene(string valor, string filtro)
        {
            return (valor ?? "").IndexOf(filtro ?? "",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int Longitud(string valor)
        {
            return valor == null ? 0 : valor.Length;
        }

        private static string Limpiar(string valor)
        {
            return (valor ?? "").Trim();
        }
    }
}
