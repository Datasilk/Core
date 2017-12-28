using System;
using Microsoft.AspNetCore.Http;

namespace Datasilk
{
    public class PageRequest
    {
        private Core S;
        public Scaffold scaffold;

        public PageRequest(Server server, HttpContext context, string path)
        {
            //the Pipeline.PageRequest is simply the first page request for a Datasilk website. 

            S = new Core(server, context);

            var paths = path.Split('?', 2)[0].Split('/');

            //create instance of Page class
            Type type = Type.GetType((S.Server.nameSpace + ".Pages." + (paths[0] == "" ? "Login" : S.Util.Str.Capitalize(paths[0].Replace("-", " ")).Replace(" ", ""))));
            var page = (Page)Activator.CreateInstance(type, new object[] { S });

            //render the server response
            S.Response.ContentType = "text/html";
            S.Response.WriteAsync(page.Render(paths));
        }
    }
}
