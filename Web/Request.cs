using System.Text;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public interface IRequest
    {
        HttpContext Context { get; set; }
        string Path { get; set; }
        string[] PathParts { get; set; }
        Parameters Parameters { get; set; }
        StringBuilder Scripts { get; set; }
        StringBuilder Css { get; set; }

        void Init(HttpContext context, Parameters parameters, string path, string[] pathParts);
        void Unload() { }
        bool CheckSecurity();
        string Error(string message = "");
        string Error404(string message = "");
        string AccessDenied(string message = "");
        string BadRequest(string message = "");
        void AddScript(string url, string id = "", string callback = "");
        void AddCSS(string url, string id = "");
    }
    public class Request: IRequest
    {
        public HttpContext Context { get; set; }
        public string Path { get; set; }
        public string[] PathParts { get; set; }
        public Parameters Parameters { get; set; }
        public StringBuilder Scripts { get; set; } = new StringBuilder();
        public StringBuilder Css { get; set; } = new StringBuilder();
        public List<string> Resources { get; set; } = new List<string>();

        public virtual void Init(HttpContext context, Parameters parameters, string path, string[] pathParts)
        {
            Context = context;
            Parameters = parameters;
            Path = path;
            PathParts = pathParts;
        }

        public virtual void Unload(){}

        public virtual bool CheckSecurity()
        {
            return true;
        }

        public virtual string Error(string message = "")
        {
            Context.Response.StatusCode = 500;
            return message;
        }

        public virtual string Error404(string message = "")
        {
            Context.Response.StatusCode = 404;
            return message;
        }

        public virtual string AccessDenied(string message = "")
        {
            Context.Response.StatusCode = 403;
            return message;
        }

        public virtual string BadRequest(string message = "")
        {
            Context.Response.StatusCode = 400;
            return message;
        }

        public virtual void AddScript(string url, string id = "", string callback = "") { }

        public virtual void AddCSS(string url, string id = "") { }
    }
}

