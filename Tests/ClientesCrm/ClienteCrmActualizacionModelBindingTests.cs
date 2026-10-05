using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web.Mvc;
using DiamDev.Give.Entities;
using DiamDev.Give.UI.Models;

namespace Tests.ClientesCrm
{
    internal static class ClienteCrmActualizacionModelBindingTests
    {
        public static int Main()
        {
            var datos = new NameValueCollection {
                { "TipoSolicitud", TiposSolicitudCliente.Actualizacion },
                { "OrigenClienteId", "42" },
                { "OrigenVersion", "3" },
                { "OrigenCodigoSap", "CL0042" },
                { "Empresa", "BOLIK" },
                { "CodigoOperador", "AGENTE01" },
                { "Ficha.RazonSocial", "Cliente actualizado" },
                { "Ficha.CambioRazonSocial", "true" },
                { "Ficha.Contactos[0].Nombre", "Ana" },
                { "Ficha.Contactos[0].TipoContacto", "Encargado de compras" },
                { "Ficha.Contactos[0].CanalComunicacion", "WHATSAPP" },
                { "Ficha.Contactos[1].Nombre", "Luis" },
                { "Ficha.Contactos[1].TipoContacto", "Responsable de pagos" },
                { "Ficha.Contactos[1].CanalComunicacion", "CORREO" }
            };
            var contexto = new ModelBindingContext {
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(
                    null, typeof(ClienteCrmEditorViewModel)),
                ModelName = "",
                ValueProvider = new NameValueCollectionValueProvider(datos, CultureInfo.InvariantCulture)
            };
            var modelo = (ClienteCrmEditorViewModel)new DefaultModelBinder()
                .BindModel(new ControllerContext(), contexto);
            if (modelo == null || modelo.TipoSolicitud != TiposSolicitudCliente.Actualizacion ||
                modelo.OrigenClienteId != 42 || modelo.OrigenVersion != 3 ||
                modelo.OrigenCodigoSap != "CL0042" || modelo.Empresa != "BOLIK" ||
                modelo.Ficha == null || !modelo.Ficha.CambioRazonSocial ||
                modelo.Ficha.Contactos == null || modelo.Ficha.Contactos.Count != 2 ||
                modelo.Ficha.Contactos[0].Nombre != "Ana" ||
                modelo.Ficha.Contactos[0].TipoContacto != "Encargado de compras" ||
                modelo.Ficha.Contactos[0].CanalComunicacion != "WHATSAPP" ||
                modelo.Ficha.Contactos[1].Nombre != "Luis" ||
                modelo.Ficha.Contactos[1].TipoContacto != "Responsable de pagos" ||
                modelo.Ficha.Contactos[1].CanalComunicacion != "CORREO")
            {
                Console.Error.WriteLine("FALLA: el formulario no conservó el origen o los contactos de actualización.");
                return 1;
            }
            Console.WriteLine("OK: MVC enlaza origen CRM, tipos de contacto libres y canales.");
            return 0;
        }
    }
}
