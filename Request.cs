using System.Text;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Web
{
    public class Request
    {
        public HttpContext context;
        public string path;
        public Parameters parameters;
        public StringBuilder scripts = new StringBuilder();
        public StringBuilder css = new StringBuilder();
        private List<string> _exists = new List<string>();

        public Request(HttpContext context, Parameters parameters) {
            this.context = context;
            this.parameters = parameters;
        }

        private User user;
        public User User
        {
            get
            {
                if(user == null)
                {
                    user = User.Get(context);
                }
                return user;
            }
        }

        public void Unload()
        {
            if (user != null) { User.Save(); }
        }

        public bool CheckSecurity()
        {
            if (User.userId > 0)
            {
                return true;
            }

            //check cookie authentication
            AuthenticationHttpContextExtensions.ChallengeAsync(context,
                CookieAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties()
                {
                    RedirectUri="/access-denied"
                }
            );
            return false;
        }

        public string Error()
        {
            context.Response.StatusCode = 500;
            return Server.LoadFileFromCache("/Views/500.html");
        }

        public string Error404()
        {
            context.Response.StatusCode = 404;
            return Server.LoadFileFromCache("/Views/404.html");
        }

        public virtual void AddScript(string url, string id = "", string callback = "") { }

        public virtual void AddCSS(string url, string id = "") { }

        public bool ResourceAdded(string url)
        {
            if (_exists.Contains(url)) { return true; }
            _exists.Add(url);
            return false;
        }
    }
}

