using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;
using Newtonsoft.Json;

namespace DiamDev.Give.BLL
{
    public class ClienteCrmBLL
    {
        private readonly ClienteCrmDA _da = new ClienteCrmDA();

        public List<ClienteCrmSolicitud> ListarSolicitudes(string empresa, string estado,
            string filtro, string creador)
        {
            var lista = _da.ListarSolicitudes(Limpio(empresa), Limpio(estado),
                Limitar(filtro, 100), Limpio(creador));
            foreach (var s in lista) s.Ficha = LeerFicha(s.FichaJson);
            return lista;
        }

        public ClienteCrmSolicitud ObtenerSolicitud(long id)
        {
            var s = _da.ObtenerSolicitud(id);
            if (s != null)
            {
                s.Ficha = LeerFicha(s.FichaJson);
                s.Archivos = _da.Archivos(id, true);
            }
            return s;
        }

        public List<ClienteCrmCliente> ListarClientes(string empresa, string filtro,
            bool incluirInactivos)
        {
            var lista = _da.ListarClientes(Limpio(empresa), Limitar(filtro, 100), incluirInactivos);
            foreach (var c in lista) c.Ficha = LeerFicha(c.FichaJson);
            return lista;
        }

        public ClienteCrmCliente ObtenerCliente(long id, string empresa)
        {
            var c = _da.ObtenerCliente(id, empresa);
            if (c != null) c.Ficha = LeerFicha(c.FichaJson);
            return c;
        }

        public long GuardarSolicitud(ClienteCrmSolicitud solicitud,
            IList<ClienteCrmArchivo> archivos, bool enviar, string usuario, string ip)
        {
            if (solicitud == null) throw new InvalidOperationException("No se recibió la solicitud.");
            ValidarEmpresa(solicitud.Empresa);
            solicitud.Empresa = solicitud.Empresa.Trim().ToUpperInvariant();
            ValidarFicha(solicitud.Ficha, enviar);
            if (string.IsNullOrWhiteSpace(solicitud.CodigoOperador) ||
                string.IsNullOrWhiteSpace(solicitud.Agente))
                throw new InvalidOperationException("Seleccione un agente asignado a la empresa.");
            if (solicitud.CodigoOperador.Length > 100 || solicitud.Agente.Length > 150)
                throw new InvalidOperationException("El código o nombre del agente excede el límite permitido.");
            ValidarArchivos(archivos);
            solicitud.FichaJson = JsonConvert.SerializeObject(solicitud.Ficha);
            return _da.GuardarSolicitud(solicitud, archivos ?? new List<ClienteCrmArchivo>(),
                enviar, usuario, ip);
        }

        public long ResolverSolicitud(long id, int version, bool aprobar, string motivo,
            string usuario, string ip)
        {
            if (!aprobar && string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("Escriba el motivo del rechazo.");
            if (!aprobar && motivo.Trim().Length > 1000)
                throw new InvalidOperationException("El motivo no puede exceder 1000 caracteres.");
            return _da.ResolverSolicitud(id, version, aprobar,
                aprobar ? null : motivo.Trim(), usuario, ip);
        }

        public long GuardarCliente(ClienteCrmCliente cliente, IList<ClienteCrmArchivo> archivos,
            string usuario, string ip)
        {
            if (cliente == null) throw new InvalidOperationException("No se recibió la ficha.");
            ValidarEmpresa(cliente.Empresa);
            cliente.Empresa = cliente.Empresa.Trim().ToUpperInvariant();
            ValidarFicha(cliente.Ficha, true);
            if (!string.IsNullOrWhiteSpace(cliente.CodigoSap) && cliente.CodigoSap.Trim().Length > 50)
                throw new InvalidOperationException("El código SAP no puede exceder 50 caracteres.");
            cliente.FichaJson = JsonConvert.SerializeObject(cliente.Ficha);
            ValidarArchivos(archivos);
            try
            {
                return _da.GuardarCliente(cliente, archivos ?? new List<ClienteCrmArchivo>(), usuario, ip);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    throw new InvalidOperationException("El NIT o el código SAP ya está vinculado a otra ficha.");
                throw;
            }
        }

        public void CambiarActivo(long id, string empresa, int version, bool activo,
            string usuario, string ip)
        {
            ValidarEmpresa(empresa);
            _da.CambiarActivo(id, empresa, version, activo, usuario, ip);
        }

        public List<ClienteCrmArchivo> Archivos(long id, bool esSolicitud, string empresa = null)
        {
            return _da.Archivos(id, esSolicitud, empresa);
        }

        public ClienteCrmArchivo ObtenerArchivo(long id) { return _da.ObtenerArchivo(id); }

        public bool ArchivoPertenece(long archivoId, long entidadId, bool esSolicitud, string empresa = null)
        {
            return _da.ArchivoPertenece(archivoId, entidadId, esSolicitud, empresa);
        }

        public List<ClienteCrmEvento> Eventos(string entidad, long id)
        {
            return _da.Eventos(entidad, id);
        }

        public void RegistrarEvento(string entidad, long? id, string empresa, string usuario,
            string accion, string detalle, string ip)
        {
            _da.RegistrarEvento(entidad, id, empresa, usuario, accion, detalle, ip);
        }

        public static void ValidarFicha(ClienteCrmFicha f, bool completa)
        {
            if (f == null) throw new InvalidOperationException("Complete la ficha de cliente.");
            f.RazonSocial = Limitar(f.RazonSocial, 200);
            f.NombreComercial = Limitar(f.NombreComercial, 200);
            f.NitDpi = Limitar(f.NitDpi, 50);
            f.CorreoFactura = Limitar(f.CorreoFactura, 150);
            f.CondicionPago = Limpio(f.CondicionPago);
            if (f.CondicionPago != null) f.CondicionPago = f.CondicionPago.ToUpperInvariant();
            f.MonedaIndicadores = Limpio(f.MonedaIndicadores);
            if (f.MonedaIndicadores != null) f.MonedaIndicadores = f.MonedaIndicadores.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(f.RazonSocial) || string.IsNullOrWhiteSpace(f.NitDpi))
                throw new InvalidOperationException("Razón social y NIT o DPI son obligatorios.");
            if (completa)
            {
                if (string.IsNullOrWhiteSpace(f.DireccionFiscal) ||
                    string.IsNullOrWhiteSpace(f.CondicionPago) ||
                    string.IsNullOrWhiteSpace(f.CorreoFactura))
                    throw new InvalidOperationException("Complete dirección fiscal, condición de pago y correo de facturación.");
                if (f.Direcciones == null || !f.Direcciones.Any(x => !string.IsNullOrWhiteSpace(x.Direccion)))
                    throw new InvalidOperationException("Agregue al menos una dirección de entrega.");
            }
            if (!string.IsNullOrWhiteSpace(f.CorreoFactura) && !new EmailAddressAttribute().IsValid(f.CorreoFactura))
                throw new InvalidOperationException("El correo de facturación no es válido.");
            if (f.CondicionPago != null && !new[] { "ANTICIPADO", "CONTADO", "CREDITO", "TEMPORADA" }
                .Contains(f.CondicionPago, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("La condición de pago no es válida.");
            if (f.Venta12Meses < 0 || f.PotencialAnual < 0 || f.Objetivo12Meses < 0 ||
                f.VendedoresCampo < 0 || f.VendedoresTienda < 0)
                throw new InvalidOperationException("Los indicadores numéricos no pueden ser negativos.");
            if (f.Venta12Meses.HasValue || f.PotencialAnual.HasValue || f.Objetivo12Meses.HasValue)
                if (!new[] { "GTQ", "USD" }.Contains(f.MonedaIndicadores,
                    StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Seleccione la moneda de los indicadores comerciales.");
            f.Contactos = (f.Contactos ?? new List<ClienteCrmContacto>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Nombre)).Take(30).ToList();
            f.Direcciones = (f.Direcciones ?? new List<ClienteCrmDireccion>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Direccion)).Take(20).ToList();
            foreach (var contacto in f.Contactos)
                if (!string.IsNullOrWhiteSpace(contacto.Correo) &&
                    !new EmailAddressAttribute().IsValid(contacto.Correo))
                    throw new InvalidOperationException("Revise el correo de " + contacto.Nombre + ".");
        }

        private static void ValidarEmpresa(string empresa)
        {
            if (!new[] { "BOLIK", "FAES", "GRACO" }.Contains(empresa,
                StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Seleccione una empresa válida.");
        }

        private static void ValidarArchivos(IList<ClienteCrmArchivo> archivos)
        {
            if (archivos == null) return;
            foreach (var a in archivos)
            {
                if (a == null || a.Contenido == null || a.Contenido.Length == 0 ||
                    a.Contenido.Length > 5242880)
                    throw new InvalidOperationException("Cada documento debe tener entre 1 byte y 5 MB.");
                if (!new[] { "RTU", "DPI", "NOMBRAMIENTO", "PATENTE_COMERCIO", "PATENTE_SOCIEDAD" }
                    .Contains(a.Tipo, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Tipo de documento no permitido.");
                if (!new[] { "application/pdf", "image/png", "image/jpeg" }
                    .Contains(a.ContentType, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Adjunte documentos PDF, PNG o JPG.");
            }
        }

        private static ClienteCrmFicha LeerFicha(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new ClienteCrmFicha() :
                JsonConvert.DeserializeObject<ClienteCrmFicha>(json) ?? new ClienteCrmFicha();
        }

        private static string Limpio(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static string Limitar(string valor, int max)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            valor = valor.Trim();
            if (valor.Length > max) throw new InvalidOperationException("Un campo excede la longitud permitida.");
            return valor;
        }
    }
}
