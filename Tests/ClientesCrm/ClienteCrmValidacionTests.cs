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
                InfluenciadorTecnico = "Luis", TipoContacto = " Encargado de compras ",
                CanalComunicacion = "WHATSAPP"
            });
            return ficha;
        }

        public static int Main()
        {
            var alta = Ficha();
            ClienteCrmBLL.ValidarPasos(alta, 2, false);
            if (alta.CambioRazonSocial) return Fallar("Un alta conserva cambio de razón social.");
            if (alta.Contactos[0].TipoContacto != "Encargado de compras")
                return Fallar("El tipo de contacto escrito no se normalizó correctamente.");

            var actualizacion = Ficha();
            ClienteCrmBLL.ValidarPasos(actualizacion, 2, true);
            if (!actualizacion.CambioRazonSocial)
                return Fallar("La actualización perdió cambio de razón social.");
            var restaurada = JsonConvert.DeserializeObject<ClienteCrmFicha>(
                JsonConvert.SerializeObject(actualizacion));
            if (restaurada.Contactos.Count != 1 ||
                restaurada.Contactos[0].TipoContacto != "Encargado de compras" ||
                restaurada.Contactos[0].CanalComunicacion != "WHATSAPP")
                return Fallar("El JSON de la ficha perdió los nuevos datos del contacto.");

            var anterior = Ficha();
            anterior.Contactos[0].TipoContacto = "COMPRAS";
            ClienteCrmBLL.ValidarPasos(anterior, 2, true);

            var sinTipo = Ficha();
            sinTipo.Contactos[0].TipoContacto = "  ";
            if (!Rechaza(sinTipo)) return Fallar("Se aceptó un tipo de contacto vacío.");
            var tipoExtenso = Ficha();
            tipoExtenso.Contactos[0].TipoContacto = new string('X', 101);
            if (!Rechaza(tipoExtenso)) return Fallar("Se aceptó un tipo de contacto mayor a 100 caracteres.");
            var sinCanal = Ficha();
            sinCanal.Contactos[0].CanalComunicacion = null;
            if (!Rechaza(sinCanal)) return Fallar("Se aceptó un contacto sin canal de comunicación.");

            Console.WriteLine("OK: tipo de contacto libre, valores anteriores y validación de contactos.");
            return 0;
        }

        private static bool Rechaza(ClienteCrmFicha ficha)
        {
            try { ClienteCrmBLL.ValidarPasos(ficha, 2, true); }
            catch (InvalidOperationException) { return true; }
            return false;
        }

        private static int Fallar(string detalle)
        {
            Console.Error.WriteLine("FALLA: " + detalle);
            return 1;
        }
    }
}
