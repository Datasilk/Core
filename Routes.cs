using Microsoft.AspNetCore.Http;

namespace Datasilk
{
    public class Routes
    {
        public virtual Page FromPageRoutes(HttpContext context, string name)
        {
            return null;
        }

        public virtual Service FromServiceRoutes(HttpContext context, string name)
        {
            return null;
        }
    }
}
