using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Corte_Caja")]
    public class CorteCaja
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Corte_Id")]
        public long CorteId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cajero_Id")]
        public long CajeroId { get; set; }

        [NotMapped]
        public Usuario Cajero { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [NotMapped]
        public Usuario Responsable { get; set; }

        [Column("Opero_Id")]
        public long OperoId { get; set; }

        [ForeignKey("OperoId")]
        public Usuario Opero { get; set; }

        public decimal Monto { get; set; }

        public decimal Gasto { get; set; }

        public bool Recibido { get; set; }
        
        [Column("Fecha_Hora")]
        public DateTime FechaHora { get; set; }

        [Column("Fecha_Hora_Recibido")]
        public DateTime? FechaHoraRecibido { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }
    }
}
