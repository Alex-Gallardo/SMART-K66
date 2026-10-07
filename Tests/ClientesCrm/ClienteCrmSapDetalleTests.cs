using System;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;

namespace Tests.ClientesCrm
{
    internal static class ClienteCrmSapDetalleTests
    {
        public static int Main()
        {
            var sap = new ClienteSapDetalle {
                CardCode = "CL123", CardName = "Cliente", LicTradNum = "1234-5",
                Address = "Dirección genérica", MailAddress = "Entrega genérica",
                Email = "factura@example.com", Currency = "GTQ",
                SecondaryName = "Marca cliente", AlternateNit = "otro NIT",
                SapPaymentMethod = "TRANSFERENCIA",
                GroupCode = 7, GroupName = "Distribuidores", PaymentExtraDays = 8,
                PaymentExtraMonths = 0, PaymentDueMonth = "N"
            };
            sap.Contactos.Add(new ClienteSapContacto {
                Nombre = "Ana", Puesto = "Compras", Telefono = "1111",
                Celular = "2222", Correo = "ana@example.com", Profesion = "Compradora"
            });
            sap.Direcciones.Add(new ClienteSapDireccion {
                Nombre = "Fiscal", Tipo = "B", Calle = "Calle fiscal", Ciudad = "Ciudad"
            });
            sap.Direcciones.Add(new ClienteSapDireccion {
                Nombre = "Bodega", Tipo = "S", Calle = "Calle entrega", Numero = "3",
                Ciudad = "Ciudad"
            });
            var ficha = ClienteCrmBLL.CrearFichaDesdeSap(sap);
            if (ficha.RazonSocial != "Cliente" || ficha.NombreComercial != "Marca cliente" ||
                ficha.NitDpi != "1234-5" || ficha.MetodoPago != "Transferencia" ||
                ficha.CodigoSapOrigen != "CL123" || ficha.TipoNegocioCodigo != 7 ||
                ficha.DireccionFiscal != "Calle fiscal, Ciudad" ||
                ficha.TemporadaPago != "8" || ficha.CondicionPago != "CREDITO")
                return Fallar("Identidad, facturación o plazo SAP mal mapeados.");
            if (ficha.Contactos.Count != 1 || ficha.Contactos[0].Nombre != "Ana" ||
                ficha.Contactos[0].Puesto != "Compras" || ficha.Contactos[0].Telefono != "1111 / 2222" ||
                ficha.Contactos[0].Correo != "ana@example.com" ||
                ficha.Contactos[0].TomadorDecision != null)
                return Fallar("El contacto SAP perdió datos o inventó datos CRM.");
            if (ficha.Direcciones.Count != 1 || ficha.Direcciones[0].Nombre != "Bodega" ||
                ficha.Direcciones[0].Direccion != "Calle entrega, 3, Ciudad" ||
                ficha.Direcciones[0].Activa || ficha.Direcciones[0].HorarioSemana != null)
                return Fallar("La dirección de entrega SAP no se prellenó correctamente.");

            var minimo = ClienteCrmBLL.CrearFichaDesdeSap(new ClienteSapDetalle {
                CardCode = "CL124", CardName = "Otro", ContactPerson = "Luis",
                Phone1 = "3333", MailAddress = "Envío", PaymentExtraDays = 30,
                PaymentExtraMonths = 1
            });
            if (minimo.Contactos.Count != 1 || minimo.Contactos[0].Nombre != "Luis" ||
                minimo.Direcciones.Count != 1 || minimo.Direcciones[0].Direccion != "Envío" ||
                minimo.TemporadaPago != null)
                return Fallar("El respaldo maestro o las condiciones de pago no son correctos.");

            Console.WriteLine("OK: datos maestros, contactos, direcciones y campos no disponibles en SAP.");
            return 0;
        }

        private static int Fallar(string mensaje)
        {
            Console.Error.WriteLine("FALLA: " + mensaje);
            return 1;
        }
    }
}
