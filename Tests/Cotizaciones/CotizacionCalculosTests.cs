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
            Igual(21.60m, normal.ImpuestoMonto, "impuesto");
            Igual(201.60m, normal.Total, "total");

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

            Igual(100m, CotizacionBLL.PrecioNetoDesdeBruto(112m, 12m),
                "precio bruto SAP a neto");
            Igual(112m, CotizacionBLL.PrecioNetoDesdeBruto(112m, 0m),
                "precio exento conserva bruto");
            Igual(226.410714m,
                CotizacionBLL.PrecioNetoDesdeBruto(253.58m, 12m),
                "precio especial conserva seis decimales");

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
                    "FALLA validación: el servidor aceptó un precio neto cero.");
                _fallas++;
            }

            Console.WriteLine(_fallas == 0
                ? "OK: cálculos, validación y total en letras verificados."
                : "FALLAS: " + _fallas);
            return _fallas == 0 ? 0 : 1;
        }
    }
}
