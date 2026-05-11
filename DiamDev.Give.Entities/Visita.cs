using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiamDev.Give.Entities
{
    [Table("Visita")]
    public class Visita
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Visita_Id")]
        public long VisitaId { get; set; }

        [Column("Empresa_Id")]
        public long EmpresaId { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa Empresa { get; set; }

        [Column("Tipo_Visita_Id")]
        public long TipoVisitaId { get; set; }

        [ForeignKey("TipoVisitaId")]
        public VisitaTipo TipoVisita { get; set; }

        [Column("ID_K66")]
        public string IDK66 { get; set; }

        public string Nit { get; set; }

        public string Nombre { get; set; }

        public string Direccion { get; set; }

        public string Observaciones { get; set; }

        public bool Bolik { get; set; }

        public bool Empaques { get; set; }

        public bool Faes { get; set; }

        public bool Graco { get; set; }

        public string Latitud { get; set; }

        public string Longitud { get; set; }

        [Column("Responsable_Id")]
        public long ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario Responsable { get; set; }

        public DateTime Fecha { get; set; }

        public int Correlativo { get; set; }

        [NotMapped]
        public List<Empresa> Empresas { get; set; }
    }
}
