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
        private readonly HanaRepository _hana = new HanaRepository();

        public List<ClienteHana> BuscarClientesSap(string empresa, string agente, string filtro)
        {
            ValidarEmpresa(empresa);
            if (string.IsNullOrWhiteSpace(agente) || string.IsNullOrWhiteSpace(filtro) ||
                filtro.Trim().Length < 2 || filtro.Length > 100)
                throw new InvalidOperationException("Escriba al menos dos caracteres para buscar en SAP.");
            string texto = filtro.Trim();
            return _hana.BuscarClientes(empresa, agente)
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.CardCode) &&
                    (Contiene(c.CardCode, texto) || Contiene(c.CardName, texto) ||
                     Contiene(c.LicTradNum, texto)))
                .Take(30).ToList();
        }

        public ClienteHana ObtenerClienteSap(string empresa, string agente, string codigo)
        {
            ValidarEmpresa(empresa);
            if (string.IsNullOrWhiteSpace(agente) || string.IsNullOrWhiteSpace(codigo) ||
                codigo.Trim().Length > 50)
                throw new InvalidOperationException("Seleccione un cliente SAP válido.");
            string exacto = codigo.Trim();
            return _hana.BuscarClientes(empresa, agente)
                .FirstOrDefault(c => c != null && string.Equals(c.CardCode,
                    exacto, StringComparison.OrdinalIgnoreCase));
        }

        public ClienteSapDetalle ObtenerClienteSapDetalle(string empresa, string agente, string codigo)
        {
            // El SP limita los clientes visibles para el agente. Solo después se leen
            // las tablas maestras del código autorizado y del schema de esa empresa.
            var visible = ObtenerClienteSap(empresa, agente, codigo);
            return visible == null ? null : _hana.ObtenerClienteDetalle(empresa, visible.CardCode);
        }

        public static ClienteCrmFicha CrearFichaDesdeSap(ClienteSapDetalle sap)
        {
            if (sap == null) throw new ArgumentNullException("sap");
            var ficha = new ClienteCrmFicha {
                RazonSocial = sap.CardName,
                NombreComercial = PrimerDato(sap.SecondaryName, sap.ForeignName, sap.CardName),
                NitDpi = PrimerDato(sap.LicTradNum, sap.AlternateNit),
                DireccionFiscal = sap.Address,
                CorreoFactura = sap.Email, MonedaIndicadores = sap.Currency,
                TipoNegocioCodigo = sap.GroupCode, TipoNegocio = sap.GroupName,
                MetodoPago = MetodoPagoCrm(sap.SapPaymentMethod), CodigoSapOrigen = sap.CardCode
            };
            var facturacion = (sap.Direcciones ?? new List<ClienteSapDireccion>())
                .FirstOrDefault(d => d != null && d.Tipo == "B");
            if (facturacion != null && !string.IsNullOrWhiteSpace(FormatearDireccion(facturacion)))
                ficha.DireccionFiscal = FormatearDireccion(facturacion);

            foreach (var contacto in (sap.Contactos ?? new List<ClienteSapContacto>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Nombre)).Take(30))
            {
                ficha.Contactos.Add(new ClienteCrmContacto {
                    Nombre = contacto.Nombre, Puesto = contacto.Puesto,
                    Telefono = UnirTelefonos(contacto.Telefono, contacto.Celular),
                    Correo = contacto.Correo, TipoContacto = contacto.Profesion
                });
            }
            if (ficha.Contactos.Count == 0 && !string.IsNullOrWhiteSpace(sap.ContactPerson))
                ficha.Contactos.Add(new ClienteCrmContacto {
                    Nombre = sap.ContactPerson,
                    Telefono = UnirTelefonos(sap.Phone1, sap.Cellular),
                    Correo = sap.Email
                });

            foreach (var direccion in (sap.Direcciones ?? new List<ClienteSapDireccion>())
                .Where(d => d != null && d.Tipo == "S" &&
                    !string.IsNullOrWhiteSpace(FormatearDireccion(d))).Take(20))
            {
                ficha.Direcciones.Add(new ClienteCrmDireccion {
                    Nombre = direccion.Nombre, Direccion = FormatearDireccion(direccion),
                    Modalidad = "Despacho"
                });
            }
            if (ficha.Direcciones.Count == 0 && !string.IsNullOrWhiteSpace(sap.MailAddress))
                ficha.Direcciones.Add(new ClienteCrmDireccion {
                    Nombre = "Dirección de entrega SAP", Direccion = sap.MailAddress,
                    Modalidad = "Despacho"
                });
            if (sap.PaymentExtraMonths == 0 &&
                new[] { 8, 15, 30, 60, 90 }.Contains(sap.PaymentExtraDays.GetValueOrDefault()) &&
                (string.IsNullOrWhiteSpace(sap.PaymentDueMonth) || sap.PaymentDueMonth == "N"))
            {
                ficha.TemporadaPago = sap.PaymentExtraDays.Value.ToString();
                ficha.CondicionPago = "CREDITO";
            }
            return ficha;
        }

        private static string FormatearDireccion(ClienteSapDireccion d)
        {
            // Algunas sociedades guardan la dirección completa en Street.
            if (!string.IsNullOrWhiteSpace(d.Calle) && d.Calle.Length > 60 &&
                d.Calle.Contains(",")) return d.Calle.Trim();
            var partes = new[] { d.Calle, d.Numero, d.Colonia, d.Ciudad,
                d.Municipio, d.Departamento, d.Pais, d.CodigoPostal }
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", partes);
        }

        private static string PrimerDato(params string[] valores)
        {
            return valores.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static string UnirTelefonos(string fijo, string movil)
        {
            if (string.IsNullOrWhiteSpace(fijo)) return movil;
            if (string.IsNullOrWhiteSpace(movil) ||
                string.Equals(fijo.Trim(), movil.Trim(), StringComparison.OrdinalIgnoreCase))
                return fijo;
            return fijo.Trim() + " / " + movil.Trim();
        }

        private static string MetodoPagoCrm(string metodoSap)
        {
            var valor = (metodoSap ?? "").Trim().ToUpperInvariant();
            if (valor == "TRANSFERENCIA") return "Transferencia";
            if (valor == "DEPOSITO" || valor == "DEPÓSITO") return "Depósito";
            if (valor == "CHEQUE") return "Cheque";
            if (valor == "EFECTIVO") return "Efectivo";
            return null;
        }

        public List<ClienteGrupoHana> TiposNegocioSap(string empresa)
        {
            ValidarEmpresa(empresa);
            return _hana.ObtenerGruposClientes(empresa);
        }

        private void VincularTipoNegocio(string empresa, ClienteCrmFicha ficha)
        {
            if (ficha == null || !ficha.TipoNegocioCodigo.HasValue) return;
            var grupo = TiposNegocioSap(empresa)
                .FirstOrDefault(x => x.GroupCode == ficha.TipoNegocioCodigo.Value);
            if (grupo == null)
                throw new InvalidOperationException("Seleccione un tipo de negocio vigente en SAP para esta empresa.");
            ficha.TipoNegocio = grupo.GroupName;
        }

        private static bool Contiene(string valor, string filtro)
        {
            return !string.IsNullOrWhiteSpace(valor) &&
                valor.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0;
        }

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
            bool incluirInactivos, string usuario = null)
        {
            var lista = _da.ListarClientes(Limpio(empresa), Limitar(filtro, 100),
                incluirInactivos, Limpio(usuario));
            foreach (var c in lista) c.Ficha = LeerFicha(c.FichaJson);
            return lista;
        }

        public ClienteCrmCliente ObtenerCliente(long id, string empresa)
        {
            var c = _da.ObtenerCliente(id, empresa);
            if (c != null) c.Ficha = LeerFicha(c.FichaJson);
            return c;
        }

        public ClienteCrmCliente ObtenerClientePorCodigoSap(string empresa, string codigo)
        {
            ValidarEmpresa(empresa);
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            var cliente = _da.ObtenerClientePorCodigoSap(empresa, codigo.Trim());
            if (cliente != null) cliente.Ficha = LeerFicha(cliente.FichaJson);
            return cliente;
        }

        public bool ClientePropio(long id, string empresa, string usuario)
        {
            return _da.ClientePropio(id, empresa, usuario);
        }

        public long GuardarSolicitud(ClienteCrmSolicitud solicitud,
            IList<ClienteCrmArchivo> archivos, bool enviar, string usuario, string ip)
        {
            if (solicitud == null) throw new InvalidOperationException("No se recibió la solicitud.");
            ValidarEmpresa(solicitud.Empresa);
            solicitud.Empresa = solicitud.Empresa.Trim().ToUpperInvariant();
            solicitud.TipoSolicitud = string.IsNullOrWhiteSpace(solicitud.TipoSolicitud)
                ? TiposSolicitudCliente.Alta : solicitud.TipoSolicitud;
            if (solicitud.TipoSolicitud != TiposSolicitudCliente.Alta &&
                solicitud.TipoSolicitud != TiposSolicitudCliente.Actualizacion)
                throw new InvalidOperationException("El tipo de solicitud no es válido.");
            if (solicitud.Ficha == null)
                throw new InvalidOperationException("Complete la ficha de cliente.");
            if (solicitud.TipoSolicitud == TiposSolicitudCliente.Actualizacion &&
                !solicitud.OrigenClienteId.HasValue && string.IsNullOrWhiteSpace(solicitud.OrigenCodigoSap))
                throw new InvalidOperationException("Seleccione un cliente para actualizar.");
            if (solicitud.TipoSolicitud == TiposSolicitudCliente.Actualizacion &&
                solicitud.OrigenClienteId.HasValue != solicitud.OrigenVersion.HasValue)
                throw new InvalidOperationException("La ficha de origen perdió su versión. Seleccione nuevamente el cliente.");
            if (solicitud.TipoSolicitud == TiposSolicitudCliente.Alta)
            {
                solicitud.OrigenClienteId = null;
                solicitud.OrigenVersion = null;
                solicitud.OrigenCodigoSap = null;
                solicitud.Ficha.CodigoSapOrigen = null;
                solicitud.Ficha.CambioRazonSocial = false;
            }
            VincularTipoNegocio(solicitud.Empresa, solicitud.Ficha);
            if (enviar) ValidarPasos(solicitud.Ficha, 5, solicitud.TipoSolicitud == TiposSolicitudCliente.Actualizacion);
            ValidarFicha(solicitud.Ficha, enviar, false);
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
            VincularTipoNegocio(cliente.Empresa, cliente.Ficha);
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

        public List<ClienteCrmEvento> Eventos(string entidad, long id, string empresa = null)
        {
            return _da.Eventos(entidad, id, empresa);
        }

        public void RegistrarEvento(string entidad, long? id, string empresa, string usuario,
            string accion, string detalle, string ip)
        {
            _da.RegistrarEvento(entidad, id, empresa, usuario, accion, detalle, ip);
        }

        public static void ValidarFicha(ClienteCrmFicha f, bool completa, bool correoFacturaObligatorio = true)
        {
            if (f == null) throw new InvalidOperationException("Complete la ficha de cliente.");
            if (string.Equals(f.TipoOperacion, "Gobierno", StringComparison.OrdinalIgnoreCase))
                f.TipoOperacion = "Publica";
            f.RazonSocial = Limitar(f.RazonSocial, 200);
            f.NombreComercial = Limitar(f.NombreComercial, 200);
            f.NitDpi = Limitar(f.NitDpi, 50);
            f.CorreoFactura = Limitar(f.CorreoFactura, 150);
            f.CodigoSapOrigen = Limitar(f.CodigoSapOrigen, 50);
            f.CondicionPago = Limpio(f.CondicionPago);
            if (f.CondicionPago != null) f.CondicionPago = f.CondicionPago.ToUpperInvariant();
            f.MonedaIndicadores = Limpio(f.MonedaIndicadores);
            if (f.MonedaIndicadores != null) f.MonedaIndicadores = f.MonedaIndicadores.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(f.RazonSocial) || string.IsNullOrWhiteSpace(f.NitDpi))
                throw new InvalidOperationException("Razón social y NIT o DPI son obligatorios.");
            if (completa)
            {
                if (string.IsNullOrWhiteSpace(f.DireccionFiscal) ||
                    string.IsNullOrWhiteSpace(f.CondicionPago))
                    throw new InvalidOperationException("Complete dirección fiscal y condición de pago.");
                if (correoFacturaObligatorio && string.IsNullOrWhiteSpace(f.CorreoFactura))
                    throw new InvalidOperationException("Complete el correo de facturación.");
                if (f.Direcciones == null || !f.Direcciones.Any(x => x != null && !string.IsNullOrWhiteSpace(x.Direccion)))
                    throw new InvalidOperationException("Agregue al menos una dirección de entrega.");
            }
            if (!string.IsNullOrWhiteSpace(f.CorreoFactura) && !new EmailAddressAttribute().IsValid(f.CorreoFactura))
                throw new InvalidOperationException("El correo de facturación no es válido.");
            if (!string.IsNullOrWhiteSpace(f.CorreoPrincipal) && !new EmailAddressAttribute().IsValid(f.CorreoPrincipal))
                throw new InvalidOperationException("El correo del contacto principal no es válido.");
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

        public static void ValidarPasos(ClienteCrmFicha f, int hasta, bool esActualizacion = false)
        {
            if (f == null || hasta < 1 || hasta > 5)
                throw new InvalidOperationException("El paso de la solicitud no es válido.");
            if (hasta >= 1)
            {
                Requerir(f.RazonSocial, "Razón social");
                Requerir(f.NombreComercial, "Nombre comercial");
                Requerir(f.NitDpi, "NIT o DPI");
                if (!f.TipoNegocioCodigo.HasValue)
                    throw new InvalidOperationException("Seleccione un tipo de negocio de SAP.");
                Requerir(f.DireccionFiscal, "Dirección fiscal");
                Requerir(f.TipoOperacion, "Tipo de operación");
                Requerir(f.CondicionPago, "Condición de pago");
                Requerir(f.MetodoPago, "Método de pago");
                if (!new[] { "8", "15", "30", "60", "90" }.Contains(f.TemporadaPago))
                    throw new InvalidOperationException("Seleccione los días de crédito.");
                if (!esActualizacion) f.CambioRazonSocial = false;
                f.CambioRazonSocialRespuesta = f.CambioRazonSocial ? "SI" : "NO";
                if (!string.IsNullOrWhiteSpace(f.CorreoFactura) &&
                    !new EmailAddressAttribute().IsValid(f.CorreoFactura))
                    throw new InvalidOperationException("El correo de factura no es válido.");
            }
            if (hasta >= 2)
            {
                if (f.Contactos == null || f.Contactos.Count == 0 || f.Contactos.Count > 30)
                    throw new InvalidOperationException("Agregue entre uno y treinta contactos.");
                foreach (var c in f.Contactos)
                {
                    if (c == null) throw new InvalidOperationException("Complete cada contacto.");
                    Requerir(c.Area, "Área del contacto");
                    Requerir(c.Nombre, "Nombre del contacto");
                    Requerir(c.Puesto, "Puesto del contacto");
                    Requerir(c.Telefono, "Teléfono del contacto");
                    Requerir(c.TomadorDecision, "Tomador de decisiones");
                    Requerir(c.InfluenciadorTecnico, "Influenciador técnico / usuario");
                    Requerir(c.TipoContacto, "Tipo de contacto");
                    c.TipoContacto = Limitar(c.TipoContacto, 100);
                    if (!new[] { "WHATSAPP", "CELULAR", "FIJO", "CORREO", "PRESENCIAL" }.Contains(c.CanalComunicacion))
                        throw new InvalidOperationException("Seleccione el canal de comunicación de cada contacto.");
                    if (!string.IsNullOrWhiteSpace(c.Correo) &&
                        !new EmailAddressAttribute().IsValid(c.Correo))
                        throw new InvalidOperationException("Revise el correo de " + c.Nombre + ".");
                }
            }
            if (hasta >= 3)
            {
                if (f.Direcciones == null || f.Direcciones.Count == 0 || f.Direcciones.Count > 20)
                    throw new InvalidOperationException("Agregue entre una y veinte direcciones.");
                foreach (var d in f.Direcciones)
                {
                    if (d == null) throw new InvalidOperationException("Complete cada dirección.");
                    Requerir(d.Nombre, "Nombre de la ubicación");
                    Requerir(d.Modalidad, "Modalidad de entrega");
                    Requerir(d.Referencia, "Referencia de la dirección");
                    Requerir(d.Direccion, "Dirección completa");
                    Requerir(d.HorarioSemana, "Horario entre semana");
                    Requerir(d.HorarioFinSemana, "Horario fin de semana");
                    d.RequiereCitaRespuesta = d.RequiereCita ? "SI" : "NO";
                    d.ActivaRespuesta = d.Activa ? "SI" : "NO";
                }
            }
            if (hasta >= 4)
            {
                Requerir(f.CoberturaGeografica, "Cobertura geográfica");
                Requerir(f.RegionesMayorVenta, "Regiones de mayor venta");
                Requerir(f.SucursalesMayorVenta, "Sucursales / PDV");
                if (!f.VendedoresCampo.HasValue || !f.VendedoresTienda.HasValue)
                    throw new InvalidOperationException("Complete ambos números de vendedores.");
                f.Televentas = Respuesta(f.TeleventasRespuesta, "Televentas");
                f.VentaMostrador = Respuesta(f.VentaMostradorRespuesta, "Venta mostrador");
                f.VentaInstitucional = Respuesta(f.VentaInstitucionalRespuesta, "Venta institucional");
                f.Ecommerce = Respuesta(f.EcommerceRespuesta, "Comercio electrónico");
                Requerir(f.ClientesFinales, "Principales clientes finales");
                Requerir(f.TemporadasDemanda, "Temporadas de mayor demanda");
                Requerir(f.ProyectosEventos, "Proyectos / eventos");
                Requerir(f.NecesidadPrincipal, "Necesidad principal");
                Requerir(f.ValorProveedor, "Qué valora al elegir proveedor");
                Requerir(f.MotivoCompra, "Motivo de compra");
                Requerir(f.ObjecionPrincipal, "Objeción principal");
                Requerir(f.CompetidorPrincipal, "Competidor principal");
                Requerir(f.FortalezaCompetidor, "Fortaleza del competidor");
                Requerir(f.DebilidadCompetidor, "Debilidad u oportunidad");
                Requerir(f.InformacionMercado, "Información de mercado");
                Requerir(f.RiesgoComercial, "Riesgo comercial");
                Requerir(f.Satisfaccion, "Nivel de satisfacción");
            }
            if (hasta >= 5 && esActualizacion)
            {
                f.CompraActualmente = Respuesta(f.CompraActualmenteRespuesta,
                    "Compra actualmente");
                if (!new[] { "GTQ", "USD" }.Contains(f.MonedaIndicadores))
                    throw new InvalidOperationException("Seleccione la moneda de los indicadores.");
                if (!f.Venta12Meses.HasValue || !f.PotencialAnual.HasValue ||
                    !f.Objetivo12Meses.HasValue)
                    throw new InvalidOperationException("Complete todos los indicadores comerciales.");
                Requerir(f.OportunidadCrecimiento, "Oportunidad de crecimiento");
                Requerir(f.CompetidorNegocio, "Competidor en este negocio");
                Requerir(f.Forecast, "Forecast / expectativa de compra");
                Requerir(f.ProximoPaso, "Próximo paso comercial");
            }
        }

        private static void Requerir(string valor, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException("Complete " + campo + ".");
        }

        private static bool Respuesta(string valor, string campo)
        {
            if (valor != "SI" && valor != "NO")
                throw new InvalidOperationException("Seleccione Sí o No en " + campo + ".");
            return valor == "SI";
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
