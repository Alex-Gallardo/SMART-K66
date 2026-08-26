using System;
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

            Console.WriteLine(_fallas == 0
                ? "OK: 8 aserciones de cálculos de cotización."
                : "FALLAS: " + _fallas);
            return _fallas == 0 ? 0 : 1;
        }
    }
}
