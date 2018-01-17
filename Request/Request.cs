namespace Datasilk
{
    public class Request
    {
        protected Core S;


        public Request(Core DatasilkCore)
        {
            S = DatasilkCore;
        }

        public bool CheckSecurity()
        {
            if (S.User.userId > 0)
            {
                return true;
            }
            return false;
        }

        public string Error()
        {
            S.Response.StatusCode = 500;
            return S.Server.LoadFileFromCache("/Pages/500.html"); ;
        }

        public string Error404()
        {
            S.Response.StatusCode = 404;
            return S.Server.LoadFileFromCache("/Pages/404.html"); ;
        }
    }
}

