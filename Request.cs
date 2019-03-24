using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Web
{
    public class Request
    {
        protected HttpContext context;

        public Request(HttpContext context) { this.context = context; }

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
    }
}

