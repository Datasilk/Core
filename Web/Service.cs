using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public interface IService: IRequest
    {
        string Success();
        string Empty();

        static string Inject(string selector, responseType injectType, string html, string javascript, string css)
        {
            var response = new Response()
            {
                type = injectType,
                selector = selector,
                html = html,
                javascript = javascript,
                css = css
            };
            return "{\"d\":" + JsonSerializer.Serialize(response) + "}";
        }
        
        static string Inject(Response response)
        {
            return Inject(response.selector, response.type, response.html, response.javascript, response.css);
        }
    }

    public class Service : Request, IService
    {
        public override string AccessDenied(string message = "access denied")
        {
            Context.Response.StatusCode = 403;
            Context.Response.WriteAsync(message);
            return message;
        }

        public override string Error(string message = "")
        {
            Context.Response.StatusCode = 500;
            Context.Response.WriteAsync(message);
            return message;
        }

        public override string BadRequest(string message = "")
        {
            Context.Response.StatusCode = 400;
            return "Bad Request";
        }

        public string Success()
        {
            return "success"; 
        }

        public string Empty() { return "{}"; }

        public static string Inject(string selector, responseType injectType, string html, string javascript, string css)
        {
            return IService.Inject(selector, injectType, html, javascript, css);
        }

        public static string Inject(Response response)
        {
            return Inject(response.selector, response.type, response.html, response.javascript, response.css);
        }

        public override void AddScript(string url, string id = "", string callback = "")
        {
            if (ContainsResource(url)) { return; }
            Scripts.Append("S.util.js.load('" + url + "', '" + (id != "" ? " id=\"" + id + "\"" : "js_" + (new Random(99999)).Next().ToString()) + "'" + (callback != "" ? "," + callback : "") + ");");
        }

        public override void AddCSS(string url, string id = "")
        {
            if (ContainsResource(url)) { return; }
            Scripts.Append("S.util.css.load('" + url + "', '" + (id != "" ? " id=\"" + id + "\"" : "css_" + (new Random(99999)).Next().ToString()) + "');");
        }

        public bool ContainsResource(string url)
        {
            if (Resources.Contains(url)) { return true; }
            Resources.Add(url);
            return false;
        }
    }
}
