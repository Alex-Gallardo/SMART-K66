using System.Configuration;
using System.Web;

namespace DiamDev.Give.UI.App_Start
{
    /// <summary>
    /// Interruptor temporal de permisos exclusivo del módulo Borradores NC.
    ///
    /// Con BorradorNC.OmitirPermisos=true cualquier usuario autenticado puede
    /// probar el módulo. Al volverlo false (o quitar la clave), se aplica el control
    /// por Rol_Permiso que usa el resto de SMART-K66.
    /// </summary>
    public sealed class BorradorNcPermisoAttribute : PermisoAttribute
    {
        public BorradorNcPermisoAttribute(string permiso) : base(permiso) { }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null || httpContext.User == null ||
                httpContext.User.Identity == null ||
                !httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            return OmitirPermisos || base.AuthorizeCore(httpContext);
        }

        public static bool OmitirPermisos =>
            string.Equals(
                ConfigurationManager.AppSettings["BorradorNC.OmitirPermisos"],
                "true",
                System.StringComparison.OrdinalIgnoreCase);
    }
}
