using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Utility.Strings;
using Utility.Serialization;

namespace Datasilk
{
    public class Request
    {
        protected HttpContext context;
        protected Server server { get; } = Server.Instance;

        public Request(HttpContext context) { this.context = context; }

        private User _user;
        public virtual User User
        {
            get
            {
                if(_user == null)
                {
                    //load user session
                    if (context.Session.Get("user") != null)
                    {
                        _user = (User)Serializer.ReadObject(context.Session.Get("user").GetString(), typeof(User));
                    }
                    else
                    {
                        _user = new User(context);
                    }
                    _user.Init();
                }
                return _user;
            }
        }

        public virtual void Unload()
        {
            if (_user != null) { User.Save(); }
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
            return server.LoadFileFromCache("/Pages/500.html");
        }

        public string Error404()
        {
            context.Response.StatusCode = 404;
            return server.LoadFileFromCache("/Pages/404.html");
        }
    }
}

