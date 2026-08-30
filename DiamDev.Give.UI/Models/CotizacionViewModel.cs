using System.Collections.Generic;

namespace DiamDev.Give.UI.Models
{
    public class CotizacionIndexViewModel
    {
        public CotizacionIndexViewModel()
        {
            Empresas = new List<CotizacionEmpresaViewModel>();
        }

        public string UsuarioActual { get; set; }
        public bool PuedeVerTodos { get; set; }
        public bool PuedeAnular { get; set; }
        public List<CotizacionEmpresaViewModel> Empresas { get; set; }
    }

    public class CotizacionEmpresaViewModel
    {
        public CotizacionEmpresaViewModel()
        {
            Operadores = new List<CotizacionOperadorViewModel>();
        }

        public long EmpresaId { get; set; }
        public string Nombre { get; set; }
        public List<CotizacionOperadorViewModel> Operadores { get; set; }
    }

    public class CotizacionOperadorViewModel
    {
        public string Codigo { get; set; }
        public string Agente { get; set; }
    }

    public class GuardarCotizacionRequest
    {
        public GuardarCotizacionRequest()
        {
            Detalles = new List<CotizacionDetalleRequest>();
        }

        public string IdEmpresa { get; set; }
        public string Fecha { get; set; }
        public string ValidaHasta { get; set; }
        public string IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string CodigoOperador { get; set; }
        public string Moneda { get; set; }
        public string CondicionesPago { get; set; }
        public string TiempoEntrega { get; set; }
        public string Observaciones { get; set; }
        public List<CotizacionDetalleRequest> Detalles { get; set; }
    }

    public class CotizacionDetalleRequest
    {
        public string ItemCode { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
    }

    public class AnularCotizacionRequest
    {
        public string Empresa { get; set; }
        public string IdCotizacion { get; set; }
        public string Motivo { get; set; }
    }

    public class CotizacionListaItemViewModel
    {
        public string IdCotizacion { get; set; }
        public string IdEmpresa { get; set; }
        public string Fecha { get; set; }
        public string ValidaHasta { get; set; }
        public string IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string Agente { get; set; }
        public string Moneda { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ImpuestoTotal { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string IdUsr { get; set; }
        public string Registro { get; set; }
    }

    public class CotizacionDetalleViewModel
    {
        public int Linea { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Descripcion { get; set; }
        public string Grupo { get; set; }
        public string Unidad { get; set; }
        public decimal Existencia { get; set; }
        public decimal Disponible { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioLista { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public string GrupoImpuesto { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ImpuestoMonto { get; set; }
        public decimal Total { get; set; }
    }

    public class CotizacionDocumentoViewModel : CotizacionListaItemViewModel
    {
        public CotizacionDocumentoViewModel()
        {
            Detalles = new List<CotizacionDetalleViewModel>();
        }

        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string CodigoOperador { get; set; }
        public string CondicionesPago { get; set; }
        public string TiempoEntrega { get; set; }
        public string Observaciones { get; set; }
        public decimal ImporteBruto { get; set; }
        public decimal DescuentoTotal { get; set; }
        public string AnuladoPor { get; set; }
        public string FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; }
        public List<CotizacionDetalleViewModel> Detalles { get; set; }
    }
}
