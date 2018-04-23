using System.Text;
using Microsoft.AspNetCore.Http;

namespace Datasilk
{
    public class Page: Request
    {

        public string title = "Datasilk";
        public string description = "";
        public string favicon = "/images/favicon.png";
        public bool useTapestry = true;
        public StringBuilder scripts = new StringBuilder();
        public StringBuilder headCss = new StringBuilder();

        public Page(HttpContext context) : base(context){}

        public virtual string Render(string[] path, string body = "", object metadata = null)
        {
            //renders HTML layout
            var scaffold = new Scaffold("/layout.html", server.Scaffold);
            scaffold.Data["title"] = title;
            scaffold.Data["description"] = description;
            scaffold.Data["head-css"] = headCss.ToString();
            scaffold.Data["favicon"] = favicon;
            scaffold.Data["body"] = body;

            //add initialization script
            scaffold.Data["scripts"] = scripts.ToString();

            return scaffold.Render();
        }

        protected string AccessDenied(bool htmlOutput = true, Page login = null)
        {
            if (htmlOutput == true)
            {
                if (!CheckSecurity() && login != null)
                {
                    return login.Render(new string[] { });
                }
                var scaffold = new Scaffold("/access-denied.html", server.Scaffold);
                return scaffold.Render();
            }
            return "Access Denied";
        }

        protected string Redirect(string url)
        {
            return "<script language=\"javascript\">window.location.href = '" + url + "';</script>";
        }

        protected void AddScript(string url, string id = "")
        {
            scripts.Append("<script language=\"javascript\"" + (id != "" ? " id=\"" + id + "\"" : "") + " src=\"" + url + "\"></script>");
        }

        protected void AddCSS(string url, string id = "")
        {
            headCss.Append("<link rel=\"stylesheet\" type=\"text/css\"" + (id != "" ? " id=\"" + id + "\"" : "") + " href=\"" + url + "\"></link>");
        }
    }
}
