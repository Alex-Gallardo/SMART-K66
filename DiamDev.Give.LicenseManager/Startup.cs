using Owin;
using System.Web.Http;
using System.Web.Http.Cors;

namespace DiamDev.Give.LicenseManager
{
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var http = new HttpConfiguration();

            http.Routes.MapHttpRoute("default", "api/{controller}/{id}", new { id = RouteParameter.Optional });

            var cors = new EnableCorsAttribute("*", "*", "*");
            http.EnableCors(cors);

            app.UseWebApi(http);
        }
    }
}
