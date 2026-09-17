using System;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public sealed class PilotoRutaBL
    {
        public static bool PruebasSoloLectura { get { return PilotoRutaDA.PruebasSoloLectura; } }
        public static bool PermiteUsuarioPrueba(string login) { return PilotoRutaDA.PermiteUsuarioPrueba(login); }
        public PilotoLista Listar(string login, DateTime desde, DateTime hasta, int pagina)
        { return new PilotoRutaDA().Listar(login, desde, hasta, pagina); }
        public PilotoRuta Detalle(string login, string id)
        { return new PilotoRutaDA().Detalle(login, id); }
        public void Cerrar(string login, PilotoCierre cierre)
        { new PilotoRutaDA().Cerrar(login, cierre); }
    }
}
