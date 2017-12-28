namespace Datasilk
{
    public class Page: Request
    {
        public string title = "Datasilk";
        public string description = "";
        public string headCss = "";
        public string favicon = "/images/favicon.png";
        public string svgIcons = "";
        public string scripts = "";
        public bool useTapestry = true;

        public Page(Core DatasilkCore): base (DatasilkCore)
        {
            svgIcons = DatasilkCore.Server.LoadFileFromCache("/content/themes/default/icons.svg");
        }

        public virtual string Render(string[] path, string body = "", object metadata = null)
        {
            //renders HTML layout
            var scaffold = new Scaffold(S, "/layout.html");
            scaffold.Data["title"] = title;
            scaffold.Data["description"] = description;
            scaffold.Data["head-css"] = headCss;
            scaffold.Data["favicon"] = favicon;
            scaffold.Data["svg-icons"] = svgIcons;
            scaffold.Data["body"] = body;

            //add initialization script
            scaffold.Data["scripts"] = scripts;

            return scaffold.Render();
        }

        public string AccessDenied(bool htmlOutput = true, Page login = null)
        {
            if (htmlOutput == true)
            {
                if (!CheckSecurity() && login != null)
                {
                    return login.Render(new string[] { });
                }
                var scaffold = new Scaffold(S, "/access-denied.html");
                return scaffold.Render();
            }
            return "Access Denied";
        }

        public string Redirect(string url)
        {
            return "<script language=\"javascript\">window.location.href = '" + url + "';</script>";
        }

        public void AddScript(string url)
        {
            scripts += "<script language=\"javascript\" src=\"" + url + "\"></script>";
        }

        public void AddCSS(string url)
        {
            headCss += "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + url + "\"></link>";
        }
    }
}
