using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Paquete_Empresa")]
    public class PaqueteEmpresa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Paquete_Empresa_Id")]
        public long PaqueteEmpresaId { get; set; }
        
        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Column("Paquete_Id")]
        public long PaqueteId { get; set; }

        [ForeignKey("PaqueteId")]
        public Paquete Paquete { get; set; }

        [Column("Forma_Pago_Id")]
        public long FormaPagoId { get; set; }

        [ForeignKey("FormaPagoId")]
        public FormaPago FormaPago { get; set; }       

        [Column("Saldo_Factura")]
        public int SaldoFactura { get; set; }

        [Column("Fecha_Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        public decimal Costo { get; set; }

        public decimal Precio { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
