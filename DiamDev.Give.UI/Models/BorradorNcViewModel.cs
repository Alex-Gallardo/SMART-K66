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
        public BorradorNcEmpresaViewModel()
        {
            Operadores = new List<BorradorNcOperadorViewModel>();
        }

        public long EmpresaId { get; set; }
        public string Nombre { get; set; }
        public List<BorradorNcOperadorViewModel> Operadores { get; set; }
    }

    public class BorradorNcOperadorViewModel
    {
        public string Codigo { get; set; }
        public string Agente { get; set; }
        public string Depto { get; set; }
    }

    // El total no forma parte de este contrato. Siempre lo calcula el BLL.
    public class GuardarBorradorNcRequest
    {
        public GuardarBorradorNcRequest()
        {
            Detalles = new List<BorradorNcDetalleRequest>();
            Enlaces = new List<BorradorNcEnlaceRequest>();
        }

        public string IdEmpresa { get; set; }
        public string Fecha { get; set; } // yyyy-MM-dd
        public string IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string CodigoOperador { get; set; }
        public string Moneda { get; set; }
        public List<BorradorNcDetalleRequest> Detalles { get; set; }
        public List<BorradorNcEnlaceRequest> Enlaces { get; set; }
    }

    public class BorradorNcEnlaceRequest
    {
        public string Url { get; set; }
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

    /// <summary>
    /// Factura asociada al borrador con sus renglones originales de SAP.
    /// Mantiene explícita la jerarquía Factura -> Productos/servicios para que
    /// Seguimiento y Autorizaciones consuman exactamente el mismo contrato.
    /// </summary>
    public class BorradorNcFacturaContenidoViewModel
    {
        public BorradorNcFacturaContenidoViewModel()
        {
            Productos = new List<BorradorNcProductoFacturaViewModel>();
        }

        public string Documento { get; set; }
        public string FechaDoc { get; set; }
        public string SerieFel { get; set; }
        public string NumeroFel { get; set; }
        public string Moneda { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal Pagado { get; set; }
        public decimal ImporteSolicitado { get; set; }
        public string Concepto { get; set; }
        public string DescripcionSolicitud { get; set; }
        public List<BorradorNcProductoFacturaViewModel> Productos { get; set; }
    }

    /// <summary>Producto o servicio facturado en un renglón de SAP INV1.</summary>
    public class BorradorNcProductoFacturaViewModel
    {
        public int NumeroLinea { get; set; }
        public string Sku { get; set; }
        public bool EsServicio { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public decimal Subtotal { get; set; }
        public string CodigoImpuesto { get; set; }
        public decimal ImpuestoPorcentaje { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public string Bodega { get; set; }
    }

    /// <summary>
    /// Consulta independiente y de solo lectura de una factura disponible en SAP.
    /// Se usa desde el selector de facturas antes de que exista un borrador.
    /// </summary>
    public class BorradorNcFacturaConsultaViewModel
    {
        public BorradorNcFacturaConsultaViewModel()
        {
            Productos = new List<BorradorNcProductoFacturaViewModel>();
        }

        public string Empresa { get; set; }
        public string Documento { get; set; }
        public string FechaDoc { get; set; }
        public string ClienteId { get; set; }
        public string ClienteNombre { get; set; }
        public string Agente { get; set; }
        public string Moneda { get; set; }
        public string SerieFel { get; set; }
        public string NumeroFel { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal Pagado { get; set; }
        public decimal SaldoSap { get; set; }
        public decimal Acumulado { get; set; }
        public decimal NcPreviaSap { get; set; }
        public decimal Disponible { get; set; }
        public decimal DisponibleNeto { get; set; }
        public List<BorradorNcProductoFacturaViewModel> Productos { get; set; }
    }

    public class BorradorNcDocumentoViewModel : BorradorNcListaItemViewModel
    {
        public BorradorNcDocumentoViewModel()
        {
            Detalles = new List<BorradorNcDetalleViewModel>();
            Adjuntos = new List<BorradorNcAdjuntoViewModel>();
        }

        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string Depto { get; set; }
        public string CodigoOperador { get; set; }
        public List<BorradorNcDetalleViewModel> Detalles { get; set; }
        public List<BorradorNcAdjuntoViewModel> Adjuntos { get; set; }
    }

    public class BorradorNcAdjuntoViewModel
    {
        public long AdjuntoId { get; set; }
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public string Extension { get; set; }
        public string ContentType { get; set; }
        public long Tamano { get; set; }
        public string Url { get; set; }
        public short Orden { get; set; }
        public string IdUsr { get; set; }
        public string Registro { get; set; }
        public bool EsVisualizable { get; set; }
    }
}
