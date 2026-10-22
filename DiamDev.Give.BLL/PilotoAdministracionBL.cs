using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public sealed class PilotoAdministracionBL
    {
        public PilotoAdminLista Listar(string actor, string buscar)
        { return new PilotoAdministracionDA().Listar(actor, buscar); }

        public PilotoAdminEdicion Obtener(string actor, long usuarioId)
        { return new PilotoAdministracionDA().Obtener(actor, usuarioId); }

        public void Guardar(string actor, PilotoAdminGuardar modelo)
        { new PilotoAdministracionDA().Guardar(actor, modelo); }

        public PilotoAdminRutaEdicion ObtenerRuta(string actor,string id)
        { return new PilotoAdministracionDA().ObtenerRuta(actor,id); }

        public void GuardarRuta(string actor,PilotoAdminRutaGuardar modelo)
        { new PilotoAdministracionDA().GuardarRuta(actor,modelo); }

        public void GuardarConductor(string actor,PilotoConductorGuardar modelo)
        { new PilotoAdministracionDA().GuardarConductor(actor,modelo); }
    }
}
