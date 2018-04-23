using Microsoft.AspNetCore.Http;

namespace Datasilk
{
    public class Routes
    {
        protected HttpContext context;
        public Routes(HttpContext context) { this.context = context; }

        public virtual Page FromPageRoutes(string name)
        {
            return null;
        }

        public virtual Service FromServiceRoutes(string name)
        {
            return null;
        }
    }
}
