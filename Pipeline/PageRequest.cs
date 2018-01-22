using System;
using Microsoft.AspNetCore.Http;

namespace Datasilk
{
    public class PageRequest
    {
        public PageRequest(Server server, HttpContext context, string path)
        {
            //initialize Datasilk Core
            var S = new Core(server, context);

            //create instance of Page class based on request URL path
            var html = "";
            var paths = path.Split('?', 2)[0].Split('/');
            var routes = new global::Routes(S);
            var page = routes.FromPageRoutes(paths[0].ToLower());

            if (page == null) {
                //page is not part of any known routes, try getting page class manually
                Type type = Type.GetType((S.Server.nameSpace + ".Pages." + (paths[0] == "" ? "Login" : S.Util.Str.Capitalize(paths[0].Replace("-", " ")).Replace(" ", ""))));
                page = (Page)Activator.CreateInstance(type, new object[] { S });
            }

            if(page != null)
            {
                //render page
                html = page.Render(paths);
            }
            else
            {
                //show 404 error
                page = new Page(S);
                html = page.Error404();
            }

            //unload Datasilk Core
            page = null;
            S.Unload();

            //send response back to client
            S.Response.ContentType = "text/html";
            S.Response.WriteAsync(html);
        }
    }
}
