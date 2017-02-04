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
    public class PersonalController : ApiController
    {
        public IEnumerable<Personal> GetAll()
        {
            return new PersonalBL().ObtenerListado(true);
        }

        public IHttpActionResult Get(long id) 
        {
            Personal PersonalActual = new PersonalBL().ObtenerPorId(id, false);
            if (PersonalActual == null)
            {
                return NotFound();                
            }

            return Ok(PersonalActual);
        }

        public string Post(Personal item)
        {
            return new PersonalBL().Guardar(item);
        }
    }
}