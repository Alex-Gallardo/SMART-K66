using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web.Mvc;
using DiamDev.Give.UI.Models;

namespace Tests.Cotizaciones
{
    /// <summary>
    /// Prueba el contrato real que usa jQuery al enviar las líneas. En MVC 5
    /// cada propiedad debe llegar como Detalles[índice].Propiedad para que el
    /// DefaultModelBinder reconstruya correctamente la colección.
    /// </summary>
    internal static class CotizacionModelBindingTests
    {
        private static int _fallas;

        private static void Verdad(bool condicion, string caso)
        {
            if (condicion) return;
            Console.Error.WriteLine("FALLA " + caso);
            _fallas++;
        }

        public static int Main()
        {
            var datos = new NameValueCollection
            {
                { "IdEmpresa", "GRACO" },
                { "Fecha", "2026-08-26" },
                { "ValidaHasta", "2026-09-10" },
                { "IdCliente", "C0001" },
                { "NombreCliente", "Cliente editado" },
                { "Nit", "CF-EDITADO" },
                { "Direccion", "Dirección editada" },
                { "Correo", "editado@example.com" },
                { "CodigoOperador", "12-AGENTE DEMO" },
                { "Moneda", "GTQ" },
                { "CondicionesPago", "Crédito 30 días" },
                { "TiempoEntrega", "5 días hábiles" },
                { "Observaciones", "Entregar en bodega central" },
                { "Detalles[0].ItemCode", "ITEM-01" },
                { "Detalles[0].Descripcion", "Producto uno" },
                { "Detalles[0].Cantidad", "2.5" },
                { "Detalles[0].PrecioUnitario", "100.25" },
                { "Detalles[0].DescuentoPorcentaje", "5" },
                { "Detalles[0].ImpuestoPorcentaje", "12" },
                { "Detalles[1].ItemCode", "ITEM-02" },
                { "Detalles[1].Descripcion", "Producto dos" },
                { "Detalles[1].Cantidad", "1" },
                { "Detalles[1].PrecioUnitario", "40" },
                { "Detalles[1].DescuentoPorcentaje", "0" },
                { "Detalles[1].ImpuestoPorcentaje", "0" }
            };

            var contexto = new ModelBindingContext
            {
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(
                    null, typeof(GuardarCotizacionRequest)),
                ModelName = "",
                ValueProvider = new NameValueCollectionValueProvider(
                    datos, CultureInfo.InvariantCulture)
            };

            var modelo = (GuardarCotizacionRequest)new DefaultModelBinder()
                .BindModel(new ControllerContext(), contexto);

            Verdad(modelo != null, "el encabezado se enlaza");
            Verdad(modelo != null && modelo.IdEmpresa == "GRACO",
                "empresa conserva su valor");
            Verdad(modelo != null && modelo.NombreCliente == "Cliente editado" &&
                    modelo.Nit == "CF-EDITADO" &&
                    modelo.Direccion == "Dirección editada" &&
                    modelo.Correo == "editado@example.com",
                "datos comerciales editables conservan sus valores");
            Verdad(modelo != null &&
                    modelo.CondicionesPago == "Crédito 30 días",
                "condiciones de pago conservan su valor");
            Verdad(modelo != null &&
                    modelo.TiempoEntrega == "5 días hábiles",
                "tiempo de entrega conserva su valor");
            Verdad(modelo != null &&
                    modelo.Observaciones == "Entregar en bodega central",
                "observaciones conservan su valor");
            Verdad(modelo != null && modelo.Detalles != null &&
                    modelo.Detalles.Count == 2,
                "se reconstruyen dos líneas");
            Verdad(modelo != null && modelo.Detalles.Count > 0 &&
                    modelo.Detalles[0].ItemCode == "ITEM-01",
                "primera línea conserva el código");
            Verdad(modelo != null && modelo.Detalles.Count > 0 &&
                    modelo.Detalles[0].Cantidad == 2.5m,
                "cantidad decimal usa cultura invariante");
            Verdad(modelo != null && modelo.Detalles.Count > 1 &&
                    modelo.Detalles[1].ImpuestoPorcentaje == 0m,
                "segunda línea conserva impuesto exento");

            Console.WriteLine(_fallas == 0
                ? "OK: 10 aserciones de model binding de cotizaciones."
                : "FALLAS: " + _fallas);
            return _fallas == 0 ? 0 : 1;
        }
    }
}
