using System.Collections.Generic;

namespace DiamDev.Give.UI.Models
{
    public class BorradorNcIndexViewModel
    {
        public BorradorNcIndexViewModel()
        {
            Empresas = new List<BorradorNcEmpresaViewModel>();
            Conceptos = new List<string>();
        }

        public string UsuarioActual { get; set; }
        public bool EsAgente { get; set; }
        public bool PuedeVerTodos { get; set; }
        public bool PuedeAutorizar { get; set; }
        public bool PuedeAnular { get; set; }
        public List<BorradorNcEmpresaViewModel> Empresas { get; set; }
        public List<string> Conceptos { get; set; }
    }

    public class BorradorNcEmpresaViewModel
    {
        public long EmpresaId { get; set; }
        public string Nombre { get; set; }
        public string CodigoOperador { get; set; }
        public string Agente { get; set; }
        public string Depto { get; set; }
    }

    // El total no forma parte de este contrato. Siempre lo calcula el BLL.
    public class GuardarBorradorNcRequest
    {
        public GuardarBorradorNcRequest()
        {
            Detalles = new List<BorradorNcDetalleRequest>();
        }

        public string IdEmpresa { get; set; }
        public string Fecha { get; set; } // yyyy-MM-dd
        public string IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string Agente { get; set; }
        public string Moneda { get; set; }
        public List<BorradorNcDetalleRequest> Detalles { get; set; }
    }

    public class BorradorNcDetalleRequest
    {
        public string Concepto { get; set; }
        public string Documento { get; set; }
        public string FechaDoc { get; set; } // yyyy-MM-dd
        public string SerieFel { get; set; }
        public string NumeroFel { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal Pagado { get; set; }
        public decimal NcPreviaSap { get; set; }
        public string Moneda { get; set; }
        public string Descripcion { get; set; }
        public decimal Importe { get; set; }
    }

    public class ResolverBorradorNcRequest
    {
        public string Empresa { get; set; }
        public string IdBorrador { get; set; }
        public string Accion { get; set; }
        public string Motivo { get; set; }
    }

    public class AnularBorradorNcRequest
    {
        public string Empresa { get; set; }
        public string IdBorrador { get; set; }
        public string Motivo { get; set; }
    }

    public class BorradorNcListaItemViewModel
    {
        public string IdBorrador { get; set; }
        public string IdEmpresa { get; set; }
        public string Fecha { get; set; }
        public string IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Agente { get; set; }
        public string Moneda { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public string IdUsr { get; set; }
        public string Registro { get; set; }
        public string ResueltoPor { get; set; }
        public string FechaResolucion { get; set; }
        public string MotivoResolucion { get; set; }
        public bool TieneNcPrevia { get; set; }
    }

    public class BorradorNcDetalleViewModel
    {
        public string Concepto { get; set; }
        public string Documento { get; set; }
        public string FechaDoc { get; set; }
        public string SerieFel { get; set; }
        public string NumeroFel { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal Pagado { get; set; }
        public decimal NcPreviaSap { get; set; }
        public string Moneda { get; set; }
        public string Descripcion { get; set; }
        public decimal Importe { get; set; }
    }

    public class BorradorNcDocumentoViewModel : BorradorNcListaItemViewModel
    {
        public BorradorNcDocumentoViewModel()
        {
            Detalles = new List<BorradorNcDetalleViewModel>();
        }

        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string Depto { get; set; }
        public string CodigoOperador { get; set; }
        public List<BorradorNcDetalleViewModel> Detalles { get; set; }
    }
}
