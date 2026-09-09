using System;
using System.Collections.Generic;
using System.Configuration;
using DiamDev.Give.DAL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.BLL
{
    public class BorradorNcDashboardBLL
    {
        private readonly BorradorNcDashboardDA _da = new BorradorNcDashboardDA();

        public int MaximoImpresion => ConfigEntero("BorradorNC.DashboardImpresionMax", 50, 1, 100);
        public int MaximoExportacion => ConfigEntero("BorradorNC.DashboardExportacionMax", 10000, 100, 50000);

        public BorradorNcDashboardPagina Consultar(
            BorradorNcDashboardFiltro filtro, BorradorNcDashboardAlcance alcance)
        {
            Normalizar(filtro, 100);
            return _da.Consultar(filtro, alcance,
                ConfigEntero("BorradorNC.DashboardDiasVencido", 5, 1, 365));
        }

        public BorradorNcDashboardPagina ConsultarParaExportar(
            BorradorNcDashboardFiltro filtro, BorradorNcDashboardAlcance alcance)
        {
            Normalizar(filtro, MaximoExportacion);
            filtro.Pagina = 1;
            filtro.TamanoPagina = MaximoExportacion;
            return _da.Consultar(filtro, alcance,
                ConfigEntero("BorradorNC.DashboardDiasVencido", 5, 1, 365));
        }

        public List<BorradorNcDashboardFactura> ConsultarFacturas(
            BorradorNcDashboardFiltro filtro, BorradorNcDashboardAlcance alcance)
        {
            return _da.ConsultarFacturas(filtro, alcance, MaximoExportacion);
        }

        public void RegistrarEvento(string empresa, string idBorrador,
                                    string evento, string usuario,
                                    string detalle, string ip)
        {
            _da.RegistrarEvento(empresa, idBorrador, evento, usuario, detalle, ip);
        }

        public List<BorradorNcBitacora> ConsultarBitacora(string empresa, string idBorrador)
        {
            return _da.ConsultarBitacora(empresa, idBorrador);
        }

        private static void Normalizar(BorradorNcDashboardFiltro filtro, int maximoPagina)
        {
            if (filtro == null) throw new ArgumentNullException("filtro");
            filtro.Pagina = Math.Max(1, filtro.Pagina);
            if (filtro.TamanoPagina <= 0) filtro.TamanoPagina = 25;
            filtro.TamanoPagina = Math.Min(maximoPagina, filtro.TamanoPagina);
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
                throw new InvalidOperationException("La fecha inicial no puede ser posterior a la final.");
        }

        private static int ConfigEntero(string clave, int defecto, int minimo, int maximo)
        {
            int valor;
            if (!int.TryParse(ConfigurationManager.AppSettings[clave], out valor)) valor = defecto;
            return Math.Max(minimo, Math.Min(maximo, valor));
        }
    }
}
