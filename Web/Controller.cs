using System.Collections.Generic;
using System.Text;

namespace Datasilk.Core.Web
{
    public class Controller: Request, IController
    {
        public StringBuilder Scripts { get; set; } = new StringBuilder();
        public StringBuilder Css { get; set; } = new StringBuilder();
        private List<string> Resources { get; set; } = new List<string>();
        
        public virtual string Render(string body = "")
        {
            return body;
        }

        public string Error<T>() where T : IController
        {
            Context.Response.StatusCode = 500;
            return IController.Error<T>(this);
        }

        public string Error404<T>() where T : IController
        {
            Context.Response.StatusCode = 404;
            return IController.Error404<T>(this);
        }

        public string Error404(string message = "Error 404")
        {
            Context.Response.StatusCode = 404;
            return message;
        }

        public string AccessDenied<T>() where T : IController
        {
            Context.Response.StatusCode = 403;
            return IController.AccessDenied<T>(this);
        }

        public string Redirect(string url)
        {
            return "<script language=\"javascript\">window.location.href = '" + url + "';</script>";
        }

        public void AddScript(string url, string id = "", string callback = "")
        {
            if (ContainsResource(url)) { return; }
            Scripts.Append("<script language=\"javascript\"" + (id != "" ? " id=\"" + id + "\"" : "") + " src=\"" + url + "\"" +
                (callback != "" ? " onload=\"" + callback + "\"" : "") + "></script>");
        }

        public void AddCSS(string url, string id = "")
        {
            if (ContainsResource(url)) { return; }
            Css.Append("<link rel=\"stylesheet\" type=\"text/css\"" + (id != "" ? " id=\"" + id + "\"" : "") + " href=\"" + url + "\"></link>");
        }

        protected bool ContainsResource(string url)
        {
            if (Resources.Contains(url)) { return true; }
            Resources.Add(url);
            return false;
        }
    }
}
