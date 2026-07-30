using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("ServicioCliente")]
    public class ServicioCliente
    {
        [Key, Column(name: "id")]
        public long ID { get; set; }

        [Column("correlativo")]
        public int Correlativo { get; set; }

        [Column("tipo")]
        public int Tipo { get; set; }

        [ForeignKey("Tipo")]
        public ServicioClienteTipo TipoServicio { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("atendidopor")]
        [StringLength(300)]
        public string Atentido { get; set; }

        [Column("estado")]
        public int Estado { get; set; }

        [Column("hora_entrada")]
        public DateTime HoraEntrada { get; set; }

        [Column("hora_atendido")]
        public DateTime? HoraAtendido { get; set; }

        [Column("hora_entrega")]
        public DateTime? HoraEntrega { get; set; }

        [Column("agencia_id")]
        public long AgenciaId { get; set; }

        [ForeignKey("AgenciaId")]
        public Agencia Agencia { get; set; }

        [Column("factura_id")]
        public long? FacturaId { get; set; }

        [ForeignKey("FacturaId")]
        public Factura Factura { get; set; }
    }
}
