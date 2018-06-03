using Microsoft.AspNetCore.Http;
using Utility.Serialization;

namespace Datasilk
{
    public class Service : Request
    {
        

        public Service(HttpContext context) : base(context) { }

        public string AccessDenied()
        {
            return "access denied";
        }

        public string Error(string message)
        {
            context.Response.StatusCode = 500;
            return message;
        }

        public string Success()
        {
            return "success";
        }

        public static string Inject(string selector, responseType injectType, string html, string javascript, string css)
        {
            var response = new Response()
            {
                type = injectType,
                selector = selector,
                html = html,
                javascript = javascript,
                css = css
            };
            return "{\"d\":" + Serializer.WriteObjectToString(response) + "}";
        }

        public static string Inject(Response response)
        {
            return Inject(response.selector, response.type, response.html, response.javascript, response.css);
        }
    }
}
