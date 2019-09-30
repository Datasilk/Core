using Microsoft.AspNetCore.Http;

namespace Datasilk.Mvc
{
    public class Controller: Web.Request
    {

        public string title = "Datasilk";
        public string description = "";
        public string favicon = "/images/favicon.png";
        public bool useTapestry = true;

        public Controller(HttpContext context, Parameters parameters) : base(context, parameters){}

        public virtual string Render(string[] path, string body = "", object metadata = null)
        {
            //renders HTML layout
            var scaffold = new Scaffold("/Views/Shared/layout.html");
            scaffold["title"] = title;
            scaffold["description"] = description;
            scaffold["head-css"] = css.ToString();
            scaffold["favicon"] = favicon;
            scaffold["body"] = body;
            
            //add initialization script
            scaffold["scripts"] = scripts.ToString();

            return scaffold.Render();
        }

        protected virtual string AccessDenied(Controller login = null)
        {
            if (!CheckSecurity() && login != null)
            {
                return login.Render(new string[] { });
            }
            var scaffold = new Scaffold("/Views/access-denied.html");
            return scaffold.Render();
        }

        protected string Redirect(string url)
        {
            return "<script language=\"javascript\">window.location.href = '" + url + "';</script>";
        }

        public override void AddScript(string url, string id = "", string callback = "")
        {
            if (ResourceAdded(url)) { return; }
            scripts.Append("<script language=\"javascript\"" + (id != "" ? " id=\"" + id + "\"" : "") + " src=\"" + url + "\"" + 
                (callback != "" ? " onload=\"" + callback + "\"" : "") + "></script>");
        }

        public override void AddCSS(string url, string id = "")
        {
            if (ResourceAdded(url)) { return; }
            css.Append("<link rel=\"stylesheet\" type=\"text/css\"" + (id != "" ? " id=\"" + id + "\"" : "") + " href=\"" + url + "\"></link>");
        }
    }
}
