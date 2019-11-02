using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public class Routes
    {
        public virtual IController FromControllerRoutes(HttpContext context, Parameters parameters, string name)
        {
            return null;
        }

        public virtual IService FromServiceRoutes(HttpContext context, Parameters parameters, string name)
        {
            return null;
        }
    }
}
