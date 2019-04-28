using System;
using Microsoft.AspNetCore.Http;
using Utility.Serialization;

namespace Datasilk.Web
{
    public class Service : Request
    {
        public Service(HttpContext context, Parameters parameters) : base(context, parameters) { }

        public string AccessDenied(string message = "access denied")
        {
            context.Response.StatusCode = 403;
            context.Response.WriteAsync(message);
            return message;
        }

        public string Error(string message)
        {
            context.Response.StatusCode = 500;
            context.Response.WriteAsync(message);
            return message;
        }

        public string Success()
        {
            return "success"; 
        }

        public string Empty() { return "{}"; }

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

        public override void AddScript(string url, string id = "", string callback = "")
        {
            if (ResourceAdded(url)) { return; }
            scripts.Append("S.util.js.load('" + url + "', '" + (id != "" ? " id=\"" + id + "\"" : "js_" + (new Random(99999)).Next().ToString()) + "'" + (callback != "" ? "," + callback : "") + ");");
        }

        public override void AddCSS(string url, string id = "")
        {
            if (ResourceAdded(url)) { return; }
            scripts.Append("S.util.css.load('" + url + "', '" + (id != "" ? " id=\"" + id + "\"" : "css_" + (new Random(99999)).Next().ToString()) + "');");
        }
    }
}
