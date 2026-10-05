using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Web.Mvc;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Controllers;
using DiamDev.Give.UI.Models;

namespace Tests.ClientesCrm
{
    internal static class ClienteCrmOrigenVacioTests
    {
        public static int Main()
        {
            var datos = new NameValueCollection {
                { "TipoSolicitud", TiposSolicitudCliente.Alta },
                { "OrigenCodigoSap", "" }
            };
            var contexto = new ModelBindingContext {
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(
                    null, typeof(ClienteCrmEditorViewModel)),
                ModelName = "",
                ValueProvider = new NameValueCollectionValueProvider(datos, CultureInfo.InvariantCulture)
            };
            var modelo = (ClienteCrmEditorViewModel)new DefaultModelBinder()
                .BindModel(new ControllerContext(), contexto);
            var comparar = typeof(ClientesCrmController).GetMethod("TieneMismoOrigen",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (comparar == null) return Fallar("No se encontró la validación del origen.");

            var existente = new ClienteCrmSolicitud {
                TipoSolicitud = TiposSolicitudCliente.Alta,
                OrigenCodigoSap = Convert.ToString(DBNull.Value)
            };
            if (modelo == null || modelo.OrigenCodigoSap != null ||
                !MismoOrigen(comparar, existente, modelo))
                return Fallar("Un alta con origen SQL vacío no puede continuar al paso 02.");

            modelo.OrigenCodigoSap = "CL999";
            if (MismoOrigen(comparar, existente, modelo))
                return Fallar("Se aceptó un código SAP de origen agregado al alta.");

            existente.TipoSolicitud = TiposSolicitudCliente.Actualizacion;
            existente.OrigenClienteId = 42;
            existente.OrigenVersion = 3;
            existente.OrigenCodigoSap = "CL0042";
            modelo.TipoSolicitud = TiposSolicitudCliente.Actualizacion;
            modelo.OrigenClienteId = 42;
            modelo.OrigenVersion = 3;
            modelo.OrigenCodigoSap = "cl0042";
            if (!MismoOrigen(comparar, existente, modelo))
                return Fallar("Una actualización con el mismo origen fue rechazada.");
            modelo.OrigenCodigoSap = "CL999";
            if (MismoOrigen(comparar, existente, modelo))
                return Fallar("Se aceptó cambiar el código SAP de origen de una actualización.");
            modelo.OrigenCodigoSap = "CL0042";
            modelo.OrigenClienteId = 43;
            if (MismoOrigen(comparar, existente, modelo))
                return Fallar("Se aceptó cambiar la ficha CRM de origen.");

            Console.WriteLine("OK: el origen vacío permite avanzar y los cambios reales siguen bloqueados.");
            return 0;
        }

        private static bool MismoOrigen(MethodInfo metodo, ClienteCrmSolicitud existente,
            ClienteCrmEditorViewModel modelo)
        {
            return (bool)metodo.Invoke(null, new object[] { existente, modelo });
        }

        private static int Fallar(string detalle)
        {
            Console.Error.WriteLine("FALLA: " + detalle);
            return 1;
        }
    }
}
