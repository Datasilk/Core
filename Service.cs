using Microsoft.AspNetCore.Http;
using Utility.Serialization;

namespace Datasilk
{
    public class Service : Request
    {
        public enum injectType
        {
            replace = 0,
            append = 1,
            before = 2,
            after = 3
        }

        public struct Response
        {
            public injectType type;
            public string selector;
            public string html;
            public string javascript;
            public string css;
        }

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

        public static string Inject(string selector, injectType injectType, string html, string javascript, string css)
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
