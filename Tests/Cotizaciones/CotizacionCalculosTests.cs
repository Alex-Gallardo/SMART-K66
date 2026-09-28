using System;
using System.Reflection;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;

namespace Tests.Cotizaciones
{
    /// <summary>
    /// Pruebas sin framework externo para que puedan ejecutarse aun en el
    /// proyecto .NET Framework legado. Código de salida distinto de cero = falla.
    /// </summary>
    internal static class CotizacionCalculosTests
    {
        private static int _fallas;

        private static void Igual(decimal esperado, decimal real, string caso)
        {
            if (esperado == real) return;
            Console.Error.WriteLine(
                "FALLA " + caso + ": esperado " + esperado + ", real " + real);
            _fallas++;
        }

        private static void Igual(string esperado, string real, string caso)
        {
            if (string.Equals(esperado, real, StringComparison.Ordinal)) return;
            Console.Error.WriteLine(
                "FALLA " + caso + ": esperado '" + esperado +
                "', real '" + real + "'");
            _fallas++;
        }

        public static int Main()
        {
            var normal = new CotizacionDetalle
            {
                Cantidad = 2m,
                PrecioUnitario = 100m,
                DescuentoPorcentaje = 10m,
                ImpuestoPorcentaje = 12m
            };
            CotizacionBLL.CalcularLinea(normal);
            Igual(200m, normal.ImporteBruto, "bruto");
            Igual(20m, normal.DescuentoMonto, "descuento");
            Igual(180m, normal.Subtotal, "subtotal");
            Igual(0m, normal.ImpuestoMonto, "IVA ya incluido");
            Igual(180m, normal.Total, "total final sin agregar IVA");

            var precioConIva = new CotizacionDetalle
            {
                Cantidad = 1m,
                PrecioUnitario = 112m,
                DescuentoPorcentaje = 0m,
                ImpuestoPorcentaje = 12m
            };
            CotizacionBLL.CalcularLinea(precioConIva);
            Igual(112m, precioConIva.Total,
                "precio ingresado con IVA no debe convertirse en 125.44");
            Igual(0m, precioConIva.ImpuestoMonto,
                "no calcular IVA monetario adicional");

            decimal totalCasoReportado = 0m;
            foreach (decimal precio in new[]
            {
                666.285714m, 171.883929m, 475.919643m,
                400.142857m, 404.758929m
            })
            {
                var linea = new CotizacionDetalle
                {
                    Cantidad = 1m,
                    PrecioUnitario = precio,
                    DescuentoPorcentaje = 0m,
                    ImpuestoPorcentaje = 12m
                };
                CotizacionBLL.CalcularLinea(linea);
                totalCasoReportado += linea.Total;
                Igual(0m, linea.ImpuestoMonto,
                    "COT-GR-00000007 sin IVA adicional");
            }
            Igual(2118.99m, totalCasoReportado,
                "COT-GR-00000007 debe conservar los precios ingresados");

            var midpoint = new CotizacionDetalle
            {
                Cantidad = 1m,
                PrecioUnitario = 0.005m,
                DescuentoPorcentaje = 0m,
                ImpuestoPorcentaje = 0m
            };
            CotizacionBLL.CalcularLinea(midpoint);
            Igual(0.01m, midpoint.Total, "redondeo AwayFromZero");

            var sinIva = new CotizacionDetalle
            {
                Cantidad = 3.5m,
                PrecioUnitario = 8m,
                DescuentoPorcentaje = 0m,
                ImpuestoPorcentaje = 0m
            };
            CotizacionBLL.CalcularLinea(sinIva);
            Igual(28m, sinIva.Subtotal, "cantidad decimal");
            Igual(28m, sinIva.Total, "línea exenta");

            Igual("UN QUETZAL CON 00/100",
                CotizacionBLL.TotalEnLetras(1m, "GTQ"),
                "total singular GTQ");
            Igual("VEINTIÚN QUETZALES CON 25/100",
                CotizacionBLL.TotalEnLetras(21.25m, "QTZ"),
                "total plural y alias QTZ");
            Igual("UN DÓLAR ESTADOUNIDENSE CON 50/100",
                CotizacionBLL.TotalEnLetras(1.50m, "USD"),
                "total singular USD");
            Igual("DOS EUROS CON 05/100",
                CotizacionBLL.TotalEnLetras(2.05m, "EUR"),
                "total plural EUR");

            var validarLinea = typeof(CotizacionBLL).GetMethod(
                "ValidarLinea", BindingFlags.NonPublic | BindingFlags.Static);
            var errorPrecioCero = validarLinea == null
                ? null
                : validarLinea.Invoke(null, new object[] { new CotizacionDetalle
                {
                    Cantidad = 1m,
                    PrecioUnitario = 0m,
                    DescuentoPorcentaje = 0m,
                    ImpuestoPorcentaje = 12m
                } }) as string;
            if (string.IsNullOrWhiteSpace(errorPrecioCero) ||
                errorPrecioCero.IndexOf("mayor que cero",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                Console.Error.WriteLine(
                    "FALLA validación: el servidor aceptó un precio final cero.");
                _fallas++;
            }

            Console.WriteLine(_fallas == 0
                ? "OK: precios con IVA incluido, validación y total en letras verificados."
                : "FALLAS: " + _fallas);
            return _fallas == 0 ? 0 : 1;
        }
    }
}
