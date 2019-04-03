using Microsoft.AspNetCore.Http;

namespace Datasilk.Web
{
    public class Routes
    {
        public virtual Mvc.Controller FromControllerRoutes(HttpContext context, Parameters parameters, string name)
        {
            return null;
        }

        public virtual Service FromServiceRoutes(HttpContext context, Parameters parameters, string name)
        {
            return null;
        }
    }
}
