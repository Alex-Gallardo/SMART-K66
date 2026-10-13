using System;
using System.Globalization;
using System.Text;

namespace DiamDev.Give.Entities
{
    /// <summary>Normaliza el texto libre de las fichas para captura posterior en SAP.</summary>
    public static class ClienteCrmTextoSap
    {
        public static string Normalizar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var descompuesto = valor.Trim().Normalize(NormalizationForm.FormD);
            var resultado = new StringBuilder(descompuesto.Length);
            bool espacio = false;
            foreach (char caracter in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
                    continue;
                char letra = char.ToUpperInvariant(caracter);
                if ((letra >= 'A' && letra <= 'Z') || (letra >= '0' && letra <= '9'))
                {
                    if (espacio && resultado.Length > 0) resultado.Append(' ');
                    resultado.Append(letra);
                    espacio = false;
                }
                else
                {
                    // Separar palabras evita que "S.A." se convierta en "SA" o que
                    // dos fragmentos alrededor de un símbolo queden unidos.
                    espacio = true;
                }
            }
            return resultado.Length == 0 ? null : resultado.ToString();
        }

        public static void NormalizarFicha(ClienteCrmFicha f)
        {
            if (f == null) return;
            f.RazonSocial = Normalizar(f.RazonSocial);
            f.NombreComercial = Normalizar(f.NombreComercial);
            f.TipoNegocio = Normalizar(f.TipoNegocio);
            f.DireccionFiscal = Normalizar(f.DireccionFiscal);
            f.TipoOperacion = Normalizar(f.TipoOperacion);
            f.CondicionPago = Normalizar(f.CondicionPago);
            f.MetodoPago = Normalizar(f.MetodoPago);
            f.TramiteContrasena = Normalizar(f.TramiteContrasena);
            f.CambioRazonSocialRespuesta = Normalizar(f.CambioRazonSocialRespuesta);
            f.ContactoPrincipal = Normalizar(f.ContactoPrincipal);
            f.CargoPrincipal = Normalizar(f.CargoPrincipal);
            f.TomadorDecision = Normalizar(f.TomadorDecision);
            f.InfluenciadorTecnico = Normalizar(f.InfluenciadorTecnico);
            f.ResponsableCompras = Normalizar(f.ResponsableCompras);
            f.ContactoPagos = Normalizar(f.ContactoPagos);
            f.CanalPreferido = Normalizar(f.CanalPreferido);
            f.ObservacionesRelacion = Normalizar(f.ObservacionesRelacion);
            f.CoberturaGeografica = Normalizar(f.CoberturaGeografica);
            f.RegionesMayorVenta = Normalizar(f.RegionesMayorVenta);
            f.SucursalesMayorVenta = Normalizar(f.SucursalesMayorVenta);
            f.TeleventasRespuesta = Normalizar(f.TeleventasRespuesta);
            f.VentaMostradorRespuesta = Normalizar(f.VentaMostradorRespuesta);
            f.VentaInstitucionalRespuesta = Normalizar(f.VentaInstitucionalRespuesta);
            f.EcommerceRespuesta = Normalizar(f.EcommerceRespuesta);
            f.ClientesFinales = Normalizar(f.ClientesFinales);
            f.TemporadasDemanda = Normalizar(f.TemporadasDemanda);
            f.ProyectosEventos = Normalizar(f.ProyectosEventos);
            f.NecesidadPrincipal = Normalizar(f.NecesidadPrincipal);
            f.ValorProveedor = Normalizar(f.ValorProveedor);
            f.MotivoCompra = Normalizar(f.MotivoCompra);
            f.ObjecionPrincipal = Normalizar(f.ObjecionPrincipal);
            f.CompetidorPrincipal = Normalizar(f.CompetidorPrincipal);
            f.FortalezaCompetidor = Normalizar(f.FortalezaCompetidor);
            f.DebilidadCompetidor = Normalizar(f.DebilidadCompetidor);
            f.InformacionMercado = Normalizar(f.InformacionMercado);
            f.RiesgoComercial = Normalizar(f.RiesgoComercial);
            f.Satisfaccion = Normalizar(f.Satisfaccion);
            f.OportunidadCrecimiento = Normalizar(f.OportunidadCrecimiento);
            f.CompetidorNegocio = Normalizar(f.CompetidorNegocio);
            f.Forecast = Normalizar(f.Forecast);
            f.ProximoPaso = Normalizar(f.ProximoPaso);
            f.ObservacionesComerciales = Normalizar(f.ObservacionesComerciales);
            f.CompraActualmenteRespuesta = Normalizar(f.CompraActualmenteRespuesta);

            foreach (var contacto in f.Contactos ?? new System.Collections.Generic.List<ClienteCrmContacto>())
            {
                if (contacto == null) continue;
                contacto.Area = Normalizar(contacto.Area);
                contacto.Nombre = Normalizar(contacto.Nombre);
                contacto.Puesto = Normalizar(contacto.Puesto);
                contacto.TomadorDecision = Normalizar(contacto.TomadorDecision);
                contacto.InfluenciadorTecnico = Normalizar(contacto.InfluenciadorTecnico);
                contacto.TipoContacto = Normalizar(contacto.TipoContacto);
                contacto.CanalComunicacion = Normalizar(contacto.CanalComunicacion);
                contacto.Telefono = LimpiarEstructurado(contacto.Telefono);
                contacto.Correo = LimpiarEstructurado(contacto.Correo);
            }
            foreach (var direccion in f.Direcciones ?? new System.Collections.Generic.List<ClienteCrmDireccion>())
            {
                if (direccion == null) continue;
                direccion.Nombre = Normalizar(direccion.Nombre);
                direccion.Direccion = Normalizar(direccion.Direccion);
                direccion.Referencia = Normalizar(direccion.Referencia);
                direccion.Modalidad = Normalizar(direccion.Modalidad);
                direccion.HorarioSemana = Normalizar(direccion.HorarioSemana);
                direccion.HorarioFinSemana = Normalizar(direccion.HorarioFinSemana);
                direccion.RequiereCitaRespuesta = Normalizar(direccion.RequiereCitaRespuesta);
                direccion.ActivaRespuesta = Normalizar(direccion.ActivaRespuesta);
            }

            // Estos datos tienen sintaxis propia o son identificadores externos. Cambiar
            // sus signos puede impedir búsquedas, contacto o vinculación con SAP.
            f.NitDpi = LimpiarEstructurado(f.NitDpi);
            f.CorreoFactura = LimpiarEstructurado(f.CorreoFactura);
            f.CorreoPrincipal = LimpiarEstructurado(f.CorreoPrincipal);
            f.TelefonoPrincipal = LimpiarEstructurado(f.TelefonoPrincipal);
            f.CodigoSapOrigen = LimpiarEstructurado(f.CodigoSapOrigen);
            f.TemporadaPago = LimpiarEstructurado(f.TemporadaPago);
            f.MonedaIndicadores = Normalizar(f.MonedaIndicadores);
        }

        private static string LimpiarEstructurado(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
