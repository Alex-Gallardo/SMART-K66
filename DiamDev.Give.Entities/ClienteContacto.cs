using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Cliente_Contacto")]
    public class ClienteContacto
    {
        [Key, Column(name: "Contacto_Id", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ContactoId { get; set; }

        [Key, Column(name: "Cliente_Id", Order = 1)]
        public long ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [Column("Departamento_Id")]
        public long DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public Departamento Departamento { get; set; }

        public string Nombre { get; set; }

        public string Telefono { get; set; }

        public string Celular { get; set; }

        public string Correo { get; set; }

        public string Notas { get; set; }
    }
}
