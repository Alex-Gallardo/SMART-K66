using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Cierre")]
    public class Cierre
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Cierre_Id")]
        public long CierreId { get; set; }

        [Column("Agencia_Id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("Cajero_Id")]
        public long CajeroId { get; set; }

        [ForeignKey("CajeroId")]
        public Usuario Cajero { get; set; }        

        public bool Recibido { get; set; }

        [Column("Fecha_Hora")]
        public DateTime FechaHora { get; set; }

        [Column("Fecha_Hora_Recibido")]
        public DateTime? FechaHoraRecibido { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<CierreDetalle> Detalles { get; set; }

        [NotMapped]
        public decimal Faltante { get; set; }

        [NotMapped]
        public decimal Sobrante { get; set; }
    }
}
