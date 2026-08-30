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
                // SAP conserva la autoridad sobre la identidad y asignación del
                // cliente. Los datos comerciales son la fotografía confirmada
                // por el usuario para esta cotización y pueden ajustarse antes
                // de guardar sin modificar el maestro en SAP.
                enc.NombreCliente = Limpiar(enc.NombreCliente);
                enc.Nit = Limpiar(enc.Nit);
                enc.Direccion = Limpiar(enc.Direccion);
                enc.Correo = Limpiar(enc.Correo);
                enc.Moneda = ResolverMoneda(enc.Moneda);
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

        /// <summary>
        /// Expresa el total monetario con dos decimales para el documento
        /// impreso. Numalet se configura por instancia para evitar que la
        /// denominación quede fija en quetzales.
        /// </summary>
        public static string TotalEnLetras(decimal total, string moneda)
        {
            decimal importe = Redondear(total);
            if (importe < 0m)
                throw new ArgumentOutOfRangeException(
                    "total", "El total no puede ser negativo.");

            string codigo = NormalizarMoneda(moneda);
            string singular;
            string plural;
            switch (codigo)
            {
                case "GTQ":
                    singular = "quetzal";
                    plural = "quetzales";
                    break;
                case "USD":
                    singular = "dólar estadounidense";
                    plural = "dólares estadounidenses";
                    break;
                case "EUR":
                    singular = "euro";
                    plural = "euros";
                    break;
                default:
                    singular = string.IsNullOrWhiteSpace(codigo)
                        ? "unidad monetaria" : codigo;
                    plural = string.IsNullOrWhiteSpace(codigo)
                        ? "unidades monetarias" : codigo;
                    break;
            }

            string denominacion = decimal.Truncate(importe) == 1m
                ? singular : plural;
            var conversor = new Numalet
            {
                MascaraSalidaDecimal = "00'/100'",
                SeparadorDecimalSalida = denominacion + " con",
                ConvertirDecimales = false,
                LetraCapital = false
            };

            try
            {
                return conversor.ToCustomCardinal(Convert.ToDouble(importe))
                    .Trim()
                    .ToUpperInvariant();
            }
            catch (ArgumentException)
            {
                // Una cotización histórica fuera del rango de Numalet debe
                // seguir siendo imprimible en lugar de romper el documento.
                return (importe.ToString("N2") + " " + codigo)
                    .Trim()
                    .ToUpperInvariant();
            }
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
            if (string.IsNullOrWhiteSpace(enc.NombreCliente)) return "Ingrese el nombre del cliente.";
            if (Longitud(enc.NombreCliente) > 200) return "El nombre del cliente excede 200 caracteres.";
            if (Longitud(enc.Nit) > 50) return "El NIT excede 50 caracteres.";
            if (Longitud(enc.Direccion) > 300) return "La dirección excede 300 caracteres.";
            if (Longitud(enc.Correo) > 150) return "El correo excede 150 caracteres.";
            if (string.IsNullOrWhiteSpace(enc.Moneda)) return "Seleccione una moneda.";
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

        private static string ResolverMoneda(string solicitada)
        {
            string app = NormalizarMoneda(solicitada);
            if (app != "GTQ" && app != "USD" && app != "EUR")
                throw new InvalidOperationException(
                    "La moneda seleccionada no está soportada: '" + app + "'.");
            return app;
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
