using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Xml.Serialization;
using DiamDev.Give.BLL;
using DiamDev.Give.Entities;

namespace DiamDev.Give.Servicio.Controllers
{
    public class HorarioController : ApiController
    {
        public string Post(PersonalHorario horario)
        {
            return new PersonalHorarioBL().Guardar(horario);
        }
    }
}