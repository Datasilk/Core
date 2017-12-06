using System;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Pipeline
{
    public class PageRequest
    {
        private Core S;
        public Scaffold scaffold;

        public PageRequest(Server server, HttpContext context)
        {
            //the Pipeline.PageRequest is simply the first page request for a Datasilk website. 

            S = new Core(server, context);

            var path = context.Request.Path.ToString().Substring(1).Split('?', 2)[0].Split('/');

            //create instance of Page class
            Type type = Type.GetType(("Datasilk.Pages." + (path[0] == "" ? "Home" : S.Util.Str.Capitalize(path[0].Replace("-", " ")).Replace(" ", ""))));
            var page = (Page)Activator.CreateInstance(type, new object[] { S });

            //render the server response
            S.Response.ContentType = "text/html";
            S.Response.WriteAsync(page.Render(path));
        }
    }
}
