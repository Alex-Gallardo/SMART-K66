using System;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public sealed class PilotoRutaBL
    {
        public static bool PruebasSoloLectura { get { return PilotoRutaDA.PruebasSoloLectura; } }
        public static bool Habilitado { get { return PilotoRutaDA.Habilitado; } }
        public static bool ConsultaRutaFija { get { return PilotoRutaDA.ConsultaRutaFija; } }
        public static void ValidarConfiguracionPrueba() { PilotoRutaDA.ValidarConfiguracionPrueba(); }
        public static bool PermiteUsuarioPrueba(string login) { return PilotoRutaDA.PermiteUsuarioPrueba(login); }
        public PilotoLista Listar(string login, DateTime desde, DateTime hasta, int pagina, string vista = null)
        { return new PilotoRutaDA().Listar(login, desde, hasta, pagina,vista); }
        public PilotoRuta Detalle(string login, string id, int pagina = 1)
        { return new PilotoRutaDA().Detalle(login, id, pagina); }
        public void Cerrar(string login, PilotoCierre cierre)
        { new PilotoRutaDA().Cerrar(login, cierre); }
        public PilotoImagen ObtenerImagen(string login, string rutaId, int rowId)
        { return new PilotoRutaDA().ObtenerImagen(login, rutaId, rowId); }
        public void GuardarImagen(string login, PilotoImagen imagen)
        { new PilotoRutaDA().GuardarImagen(login, imagen); }
    }
}
