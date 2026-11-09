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

        public PilotoAdminRutaEdicion ObtenerRuta(string actor,string id,int paginaDocumento=1,string buscarDocumento=null)
        { return new PilotoAdministracionDA().ObtenerRuta(actor,id,paginaDocumento,buscarDocumento); }

        public void GuardarRuta(string actor,PilotoAdminRutaGuardar modelo)
        { new PilotoAdministracionDA().GuardarRuta(actor,modelo); }

        public PilotoAdminDocumentoEdicion ObtenerDocumento(string actor,string rutaId,int rowId)
        { return new PilotoAdministracionDA().ObtenerDocumento(actor,rutaId,rowId); }

        public int GuardarDocumento(string actor,PilotoAdminDocumentoGuardar modelo)
        { return new PilotoAdministracionDA().GuardarDocumento(actor,modelo); }

        public void EliminarDocumento(string actor,string rutaId,int rowId,string version,string motivo)
        { new PilotoAdministracionDA().EliminarDocumento(actor,rutaId,rowId,version,motivo); }

        public void GuardarConductor(string actor,PilotoConductorGuardar modelo)
        { new PilotoAdministracionDA().GuardarConductor(actor,modelo); }
    }
}
