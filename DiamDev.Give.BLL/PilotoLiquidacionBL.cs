using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public sealed class PilotoLiquidacionBL
    {
        public PilotoLiquidacionLista Listar(string actor,PilotoLiquidacionFiltro filtro) {return new PilotoLiquidacionDA().Listar(actor,filtro);}
        public PilotoLiquidacionRuta Detalle(string actor,string id) {return new PilotoLiquidacionDA().Detalle(actor,id);}
        public int LiquidarDocumentos(string actor,string id,int? rowId) {return new PilotoLiquidacionDA().LiquidarDocumentos(actor,id,rowId);}
        public void LiquidarRuta(string actor,string id) {new PilotoLiquidacionDA().LiquidarRuta(actor,id);}
        public PilotoLiquidacionReporte Reporte(string actor,string[] ids) {return new PilotoLiquidacionDA().Reporte(actor,ids);}
    }
}
