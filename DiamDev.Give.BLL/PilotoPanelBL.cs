using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public sealed class PilotoPanelBL
    {
        public PilotoPanelModelo Leer(string login,PilotoPanelFiltro filtro,string id=null) { return new PilotoPanelDA().Leer(login,filtro,id); }
        public PilotoImagen Imagen(string login,string id,int row,bool cliente) { return new PilotoPanelDA().Imagen(login,id,row,cliente); }
    }
}
