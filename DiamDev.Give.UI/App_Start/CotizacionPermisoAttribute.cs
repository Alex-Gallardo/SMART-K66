using System.Configuration;
using System.Web;

namespace DiamDev.Give.UI.App_Start
{
    /// <summary>
    /// Permisos del módulo Cotizaciones con bypass exclusivo para su etapa de
    /// pruebas. Nunca omite autenticación y no afecta otros módulos.
    /// </summary>
    public sealed class CotizacionPermisoAttribute : PermisoAttribute
    {
        public CotizacionPermisoAttribute(string permiso) : base(permiso) { }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.User == null ||
                httpContext.User.Identity == null ||
                !httpContext.User.Identity.IsAuthenticated)
                return false;

            return OmitirPermisos || base.AuthorizeCore(httpContext);
        }

        public static bool OmitirPermisos
        {
            get
            {
                return string.Equals(
                    ConfigurationManager.AppSettings["Cotizaciones.OmitirPermisos"],
                    "true", System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
