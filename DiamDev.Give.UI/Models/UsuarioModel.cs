using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DiamDev.Give.UI.Models
{
    public class UsuarioModel
    {
        public long UsuarioId { get; set; }

        [Required(ErrorMessage = "El usuario es requerido")]
        [StringLength(50)]
        public string Login { get; set; }

        [Required(ErrorMessage = "El password es requerido")]
        [StringLength(150)]
        public string Password { get; set; }
    }
}