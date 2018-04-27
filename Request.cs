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
        protected Server Server { get; } = Server.Instance;

        public Request(HttpContext context) { this.context = context; }

        private User user;
        public virtual User User
        {
            get
            {
                if(user == null)
                {
                    //load user session
                    if (context.Session.Get("user") != null)
                    {
                        user = (User)Serializer.ReadObject(context.Session.Get("user").GetString(), typeof(User));
                    }
                    else
                    {
                        user = new User(context);
                    }
                    user.Init(context);
                }
                return user;
            }
        }

        public virtual void Unload()
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
            return Server.LoadFileFromCache("/Pages/500.html");
        }

        public string Error404()
        {
            context.Response.StatusCode = 404;
            return Server.LoadFileFromCache("/Pages/404.html");
        }
    }
}

