using System;
using DiamDev.Give.Entities;

namespace Tests.ClientesCrm
{
    internal static class ClienteCrmTextoSapTests
    {
        public static int Main()
        {
            var ficha = new ClienteCrmFicha {
                RazonSocial = "  Compañía Águila, S.A.  ",
                NombreComercial = "Niños & Cía.",
                NitDpi = "123456-7",
                CorreoFactura = " Facturas+crm@ejemplo.com ",
                CodigoSapOrigen = "CL-001",
                TipoNegocio = "Comercio / distribución",
                TipoOperacion = "Pública",
                MetodoPago = "Depósito",
                TemporadaPago = "30",
                ObservacionesComerciales = "¡Próximo paso: llamar!",
                CoberturaGeografica = "Ñiquén",
                MonedaIndicadores = "gtq"
            };
            ficha.Contactos.Add(new ClienteCrmContacto {
                Area = "Créditos", Nombre = "José Pérez", Puesto = "Jefe / ventas",
                Telefono = "+502 5555-0101", Correo = "jose.perez@ejemplo.com",
                TipoContacto = "Influenciador técnico", CanalComunicacion = "WhatsApp"
            });
            ficha.Direcciones.Add(new ClienteCrmDireccion {
                Nombre = "Bodega #1", Direccion = "5ª avenida, zona 3",
                Modalidad = "Transporte de envío", HorarioSemana = "08:00–17:00"
            });

            ClienteCrmTextoSap.NormalizarFicha(ficha);
            Verificar(ficha.RazonSocial == "COMPANIA AGUILA S A", "razón social");
            Verificar(ficha.NombreComercial == "NINOS CIA", "nombre comercial");
            Verificar(ficha.TipoNegocio == "COMERCIO DISTRIBUCION", "grupo de SAP");
            Verificar(ficha.TipoOperacion == "PUBLICA" && ficha.MetodoPago == "DEPOSITO",
                "catálogos");
            Verificar(ficha.ObservacionesComerciales == "PROXIMO PASO LLAMAR", "observaciones");
            Verificar(ficha.CoberturaGeografica == "NIQUEN" && ficha.MonedaIndicadores == "GTQ",
                "acentos y moneda");
            Verificar(ficha.Contactos[0].Nombre == "JOSE PEREZ" &&
                ficha.Contactos[0].TipoContacto == "INFLUENCIADOR TECNICO", "contacto");
            Verificar(ficha.Direcciones[0].Direccion == "5 AVENIDA ZONA 3" &&
                ficha.Direcciones[0].Modalidad == "TRANSPORTE DE ENVIO", "dirección");
            Verificar(ficha.NitDpi == "123456-7" && ficha.CodigoSapOrigen == "CL-001" &&
                ficha.Contactos[0].Telefono == "+502 5555-0101" &&
                ficha.Contactos[0].Correo == "jose.perez@ejemplo.com" &&
                ficha.CorreoFactura == "Facturas+crm@ejemplo.com" &&
                ficha.TemporadaPago == "30", "identificadores y contacto estructurado");
            ClienteCrmTextoSap.NormalizarFicha(ficha);
            Verificar(ficha.RazonSocial == "COMPANIA AGUILA S A" &&
                ficha.Contactos[0].TipoContacto == "INFLUENCIADOR TECNICO", "idempotencia");
            Verificar(ClienteCrmTextoSap.Normalizar("!!!") == null, "texto sin caracteres válidos");
            Console.WriteLine("OK: normalización de ficha, catálogos, listas, datos estructurados e idempotencia.");
            return 0;
        }

        private static void Verificar(bool condicion, string caso)
        {
            if (!condicion) throw new Exception("Falló: " + caso);
        }
    }
}
