using System;
using System.Web.Http;

namespace DiamDev.Give.LicenseManager.Controllers
{
    public class LicenseController : ApiController
    {
        public IHttpActionResult Post()
        {
            return Ok(Guid.NewGuid());
        }
    }
}
