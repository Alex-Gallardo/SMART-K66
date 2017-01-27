using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamDev.Give.Entities
{
    [Table("Traslado")]
    public class Traslado
    {
        [Key]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
        [Column("Traslado_Id")]
        public long TrasladoId { get; set; }

        [Column("Agencia_Origen_Id")]
        public long AgenciaOrigenId { get; set; }

        [ForeignKey("AgenciaOrigenId")]
        public Agencia AgenciaOrigen { get; set; }

        [Column("Agencia_Destino_Id")]
        public long AgenciaDestinoId { get; set; }

        [ForeignKey("AgenciaDestinoId")]
        public Agencia AgenciaDestino { get; set; }

        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; }

        [Column("Usr_Inicial")]
        public long UsrInicial { get; set; }

        [ForeignKey("UsrInicial")]
        public Usuario UsuarioInicial { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        public List<TrasladoDetalle> Detalles { get; set; }
    }
}
