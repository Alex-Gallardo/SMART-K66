using System;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;

namespace Tests.ClientesCrm
{
    internal static class ClienteCrmValidacionTests
    {
        private static ClienteCrmFicha Ficha()
        {
            var ficha = new ClienteCrmFicha {
                RazonSocial = "Cliente", NombreComercial = "Cliente comercial", NitDpi = "123456-7",
                TipoNegocioCodigo = 1, DireccionFiscal = "Dirección", TipoOperacion = "Privada",
                CorreoFactura = "facturas@example.com", CondicionPago = "CREDITO",
                MetodoPago = "Transferencia", TramiteContrasena = "Correo", TemporadaPago = "30",
                CambioRazonSocial = true
            };
            ficha.Contactos.Add(new ClienteCrmContacto {
                Area = "Compras", Nombre = "Ana", Puesto = "Gerente", Telefono = "5555-0101",
                Correo = "ana@example.com", TomadorDecision = "Ana",
                InfluenciadorTecnico = "Luis", TipoContacto = "COMPRAS",
                CanalComunicacion = "WHATSAPP"
            });
            return ficha;
        }

        public static int Main()
        {
            var alta = Ficha();
            ClienteCrmBLL.ValidarPasos(alta, 2, false);
            if (alta.CambioRazonSocial) return Fallar("Un alta conserva cambio de razón social.");

            var actualizacion = Ficha();
            ClienteCrmBLL.ValidarPasos(actualizacion, 2, true);
            if (!actualizacion.CambioRazonSocial)
                return Fallar("La actualización perdió cambio de razón social.");
            var restaurada = JsonConvert.DeserializeObject<ClienteCrmFicha>(
                JsonConvert.SerializeObject(actualizacion));
            if (restaurada.Contactos.Count != 1 ||
                restaurada.Contactos[0].TipoContacto != "COMPRAS" ||
                restaurada.Contactos[0].CanalComunicacion != "WHATSAPP")
                return Fallar("El JSON de la ficha perdió los nuevos datos del contacto.");

            actualizacion.Contactos[0].CanalComunicacion = null;
            try { ClienteCrmBLL.ValidarPasos(actualizacion, 2, true); }
            catch (InvalidOperationException) {
                Console.WriteLine("OK: contactos requieren tipo/canal y el cambio de razón social solo aplica a actualizaciones.");
                return 0;
            }
            return Fallar("Se aceptó un contacto sin canal de comunicación.");
        }

        private static int Fallar(string detalle)
        {
            Console.Error.WriteLine("FALLA: " + detalle);
            return 1;
        }
    }
}
