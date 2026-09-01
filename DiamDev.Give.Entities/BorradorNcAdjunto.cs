using System;

namespace DiamDev.Give.Entities
{
    public static class TiposAdjuntoBorradorNc
    {
        public const string Archivo = "ARCHIVO";
        public const string Enlace = "ENLACE";
    }

    /// <summary>
    /// Documento de respaldo asociado a un borrador. El contenido se carga
    /// únicamente al descargar; los detalles normales transportan metadatos.
    /// </summary>
    public class BorradorNcAdjunto
    {
        public long AdjuntoId { get; set; }
        public string IdBorrador { get; set; }
        public string IdEmpresa { get; set; }
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public string Extension { get; set; }
        public string ContentType { get; set; }
        public long Tamano { get; set; }
        public byte[] Contenido { get; set; }
        public string Url { get; set; }
        public byte[] HashSha256 { get; set; }
        public short Orden { get; set; }
        public string IdUsr { get; set; }
        public DateTime? Registro { get; set; }

        public bool EsArchivo
        {
            get { return string.Equals(Tipo, TiposAdjuntoBorradorNc.Archivo,
                                       StringComparison.OrdinalIgnoreCase); }
        }

        public bool EsEnlace
        {
            get { return string.Equals(Tipo, TiposAdjuntoBorradorNc.Enlace,
                                       StringComparison.OrdinalIgnoreCase); }
        }
    }
}
