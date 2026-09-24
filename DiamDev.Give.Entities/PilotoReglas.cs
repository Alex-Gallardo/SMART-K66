using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DiamDev.Give.Entities
{
    public static class PilotoReglas
    {
        // Mantiene el formulario completo por debajo del limite de claves de ASP.NET.
        public const int MaxDocumentos = 200;
        public const int MaxObservacion = 500;
        public const int MaxImagenBytes = 10 * 1024 * 1024;
        public static readonly string[] MotivosNoEntrega = new[] {
            "TIEMPO CLIENTE", "TIEMPO RUTA", "CLIENTE CERRADO", "FALTA ESPACIO",
            "PEDIDO INCORRECTO", "CLIENTE RECHAZO PEDIDO", "PRODUCTO NO SOLICITADO", "OTRO"
        };
        private static readonly HashSet<string> MotivosPermitidos = new HashSet<string>(MotivosNoEntrega, StringComparer.Ordinal);
        public static string ValidarImagen(string nombre, byte[] contenido)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 255 || nombre.Any(char.IsControl))
                throw new PilotoException(400, "El nombre de la imagen no es válido.");
            if (contenido == null || contenido.Length == 0 || contenido.Length > MaxImagenBytes)
                throw new PilotoException(400, "La imagen debe pesar entre 1 byte y 10 MB.");
            var extension = Path.GetExtension(nombre).ToLowerInvariant();
            if ((extension == ".jpg" || extension == ".jpeg") && EmpiezaCon(contenido, 0xFF, 0xD8, 0xFF)) return "image/jpeg";
            if (extension == ".png" && EmpiezaCon(contenido, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return "image/png";
            if (extension == ".webp" && contenido.Length >= 12 && Encoding.ASCII.GetString(contenido, 0, 4) == "RIFF" &&
                Encoding.ASCII.GetString(contenido, 8, 4) == "WEBP") return "image/webp";
            throw new PilotoException(400, "Usa una imagen JPG, PNG o WebP válida.");
        }

        private static bool EmpiezaCon(byte[] contenido, params byte[] firma)
        {
            if (contenido.Length < firma.Length) return false;
            for (var i = 0; i < firma.Length; i++) if (contenido[i] != firma[i]) return false;
            return true;
        }
        public static string VistaRutas(string vista)
        {
            if (string.IsNullOrWhiteSpace(vista)) return "activas";
            if (vista == "activas" || vista == "historial") return vista;
            throw new PilotoException(400,"Selecciona Activas o Historial para consultar tus rutas.");
        }
        public static void PrepararConsulta(PilotoRuta ruta, int pagina)
        {
            if (pagina < 1 || pagina > 100000) throw new PilotoException(400,"Selecciona una página de documentos válida.");
            ruta.PuedeCerrar = ruta.PuedeCerrar && ruta.TotalDocumentos <= MaxDocumentos;
            ruta.TamanoPaginaDocumentos = ruta.PuedeCerrar ? MaxDocumentos : 25;
            ruta.PaginaDocumentos = ruta.PuedeCerrar ? 1 : pagina;
            if (ruta.PaginaDocumentos > 1 && (long)(ruta.PaginaDocumentos-1)*ruta.TamanoPaginaDocumentos >= ruta.TotalDocumentos)
                throw new PilotoException(404,"La página de documentos no está disponible. Vuelve al inicio de la ruta.");
        }
        public static void ValidarCierre(PilotoRuta ruta, PilotoCierre cierre)
        {
            NormalizarCierre(cierre);
            if (cierre == null || cierre.Solicitud == Guid.Empty || cierre.RutaId != ruta.Id)
                throw new PilotoException(400, "La solicitud de cierre no es valida.");
            if (ruta.Estado != "E")
                throw new PilotoException(409, "Solo se puede completar una ruta en estado En ruta.");
            if (string.IsNullOrEmpty(cierre.Version) || cierre.Version != ruta.Version)
                throw new PilotoException(409, "La ruta cambio. Recarga sus detalles antes de completar.");
            if (ruta.Documentos.Count == 0 || cierre.Documentos == null ||
                cierre.Documentos.Count != ruta.Documentos.Count || cierre.Documentos.Count > MaxDocumentos)
                throw new PilotoException(400, "Debes indicar el resultado de todos los documentos.");
            var esperados = new HashSet<int>(ruta.Documentos.Select(d => d.RowId));
            foreach (var d in cierre.Documentos)
            {
                if (d == null || !esperados.Remove(d.RowId))
                    throw new PilotoException(400, "Hay documentos repetidos o ajenos a la ruta.");
                ValidarResultado(d);
            }
        }

        public static void ValidarResultado(PilotoResultado d)
        {
            if (d == null || !d.Visito.HasValue || (d.Entrega != "ENTREGADO" && d.Entrega != "NO ENTREGADO" && d.Entrega != "INCIDENCIA"))
                throw new PilotoException(400, "Selecciona visita y resultado para cada documento.");
            if (d.Entrega == "ENTREGADO" && !d.Visito.Value)
                throw new PilotoException(400, "Un documento entregado debe estar marcado como visitado.");
            if (d.Entrega != "ENTREGADO" && (!MotivosPermitidos.Contains(d.Motivo ?? "") ||
                string.IsNullOrWhiteSpace(d.Observaciones) || d.Observaciones.Length > MaxObservacion))
                throw new PilotoException(400, "No entregado e incidencia requieren un motivo válido y una observación de hasta 500 caracteres.");
        }

        public static void NormalizarCierre(PilotoCierre cierre)
        {
            if (cierre == null || cierre.Documentos == null) return;
            foreach (var d in cierre.Documentos)
            {
                if (d == null) continue;
                d.Entrega = Limpiar(d.Entrega);
                if (d.Entrega != null) d.Entrega = d.Entrega.ToUpperInvariant();
                d.Motivo = Limpiar(d.Motivo);
                if (d.Motivo != null) d.Motivo = d.Motivo.ToUpperInvariant();
                d.Observaciones = Limpiar(d.Observaciones);
                if (d.Entrega == "ENTREGADO")
                {
                    d.Motivo = null;
                    d.Observaciones = null;
                }
            }
        }

        public static string Version(PilotoRuta ruta)
        {
            return Hash(delegate(BinaryWriter w)
            {
                Texto(w, ruta.Id); w.Write(ruta.Fecha.Ticks); Texto(w, ruta.Estado);
                Texto(w, ruta.Placa); Texto(w, ruta.Piloto); Texto(w, ruta.Centro);
                foreach (var d in ruta.Documentos.OrderBy(d => d.RowId))
                {
                    w.Write(d.RowId); Texto(w, d.Tipo); Texto(w, d.Empresa); Texto(w, d.Documento);
                    Texto(w, d.Cliente); Texto(w, d.Direccion); w.Write(d.Bultos);
                    w.Write(d.Visito.HasValue); if (d.Visito.HasValue) w.Write(d.Visito.Value);
                    Texto(w, d.Entrega); Texto(w, d.Motivo); Texto(w, d.Observaciones);
                    w.Write(d.Entrada.HasValue); if (d.Entrada.HasValue) w.Write(d.Entrada.Value.Ticks);
                    w.Write(d.Salida.HasValue); if (d.Salida.HasValue) w.Write(d.Salida.Value.Ticks);
                }
            });
        }

        public static string HuellaSolicitud(PilotoCierre cierre)
        {
            NormalizarCierre(cierre);
            if (cierre == null || cierre.Documentos == null || cierre.Documentos.Any(d => d == null) || cierre.Documentos.Count > MaxDocumentos)
                throw new PilotoException(400, "La solicitud de cierre no es valida.");
            return Hash(delegate(BinaryWriter w)
            {
                Texto(w, cierre.RutaId); Texto(w, cierre.Version);
                foreach (var d in cierre.Documentos.OrderBy(d => d.RowId))
                {
                    w.Write(d.RowId); w.Write(d.Visito.HasValue); if (d.Visito.HasValue) w.Write(d.Visito.Value);
                    Texto(w, d.Entrega); Texto(w, d.Motivo); Texto(w, d.Observaciones);
                }
            });
        }

        private static string Limpiar(string value) { return string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }
        private static void Texto(BinaryWriter w, string value) { w.Write(value != null); if (value != null) w.Write(value); }
        private static string Hash(Action<BinaryWriter> escribir)
        {
            using (var buffer = new MemoryStream())
            {
                using (var w = new BinaryWriter(buffer, Encoding.UTF8, true)) escribir(w);
                using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(buffer.ToArray()));
            }
        }
    }
}
