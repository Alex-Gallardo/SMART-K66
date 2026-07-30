using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Empresa_Bodega_Activa")]
    public class EmpresaBodegaActiva
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Bodega_Id")]
        public Guid BodegaId { get; set; }

        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Column("Warehouse_Id")]
        public string WarehouseId { get; set; }

        [Column("Location_Id")]
        public string LocationId { get; set; }
    }
}
